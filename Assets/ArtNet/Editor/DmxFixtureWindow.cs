using System;
using System.Collections.Generic;
using System.Linq;
using ArtNet.Devices.Modular;
using UnityEditor;
using UnityEngine;

namespace ArtNet.Editor
{
    public class DmxFixtureWindow : EditorWindow
    {
        private enum TestWaveform
        {
            Constant,
            PingPong,
            Sine,
            Step
        }

        private sealed class FixtureGroupView
        {
            public string Key;
            public string Label;
            public List<DmxFixture> Fixtures;
            public ushort Universe;
            public ushort StartAddress;
        }

        private sealed class TestClip
        {
            public DmxFixture Fixture;
            public int AbsoluteChannel;
            public int RelativeChannel;
            public string DisplayName;
            public TestWaveform Waveform;
            public float Speed = 1f;
            public float Min = 0f;
            public float Max = 255f;
            public bool Playing;
        }

        private readonly Dictionary<string, TestClip> _testClips = new();
        private readonly Dictionary<int, byte[]> _fixtureBuffers = new();
        private readonly HashSet<int> _fixturesToClear = new();
        private readonly List<FixtureGroupView> _groups = new();

        private Vector2 _scroll;
        private int _selectedGroupIndex;
        private int _selectedFixtureIndex;
        private string _selectedChannelKey;
        private bool _showDebug;

        [MenuItem("ArtNet/Dmx Fixture Window")]
        public static void Open()
        {
            GetWindow<DmxFixtureWindow>("DMX Fixtures");
        }

        private void OnEnable()
        {
            RefreshFixtures(true);
            EditorApplication.hierarchyChanged += RefreshFixtureList;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= RefreshFixtureList;
            EditorApplication.update -= OnEditorUpdate;
            StopAllTests();
        }

        private void OnGUI()
        {
            PruneInvalidReferences();

            if (GUILayout.Button("Refresh Fixtures"))
            {
                RefreshFixtures(true);
            }

            if (GUILayout.Button("Stop All Tests"))
            {
                StopAllTests();
            }

            if (_groups.Count == 0)
            {
                EditorGUILayout.HelpBox("No DmxFixture found in the open scene.", MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawGroupSection();
            EditorGUILayout.Space();
            DrawChannelTestSection();
            EditorGUILayout.Space();
            DrawDebugSection();
            EditorGUILayout.EndScrollView();
        }

        private void PruneInvalidReferences()
        {
            _groups.RemoveAll(group =>
            {
                group.Fixtures.RemoveAll(fixture => fixture == null);
                return group.Fixtures.Count == 0;
            });

            var invalidClipKeys = _testClips
                .Where(pair => pair.Value.Fixture == null)
                .Select(pair => pair.Key)
                .ToArray();

            foreach (var key in invalidClipKeys)
            {
                _testClips.Remove(key);
            }

            var validFixtureIds = _groups
                .SelectMany(group => group.Fixtures)
                .Where(fixture => fixture != null)
                .Select(fixture => fixture.GetInstanceID())
                .ToHashSet();

            var invalidBufferKeys = _fixtureBuffers.Keys
                .Where(id => !validFixtureIds.Contains(id))
                .ToArray();

            foreach (var key in invalidBufferKeys)
            {
                _fixtureBuffers.Remove(key);
                _fixturesToClear.Remove(key);
            }

            _selectedGroupIndex = Mathf.Clamp(_selectedGroupIndex, 0, Mathf.Max(0, _groups.Count - 1));
            var selectedFixtures = _groups.Count > 0 ? _groups[_selectedGroupIndex].Fixtures : null;
            var maxFixtureIndex = selectedFixtures == null ? 0 : Mathf.Max(0, selectedFixtures.Count - 1);
            _selectedFixtureIndex = Mathf.Clamp(_selectedFixtureIndex, 0, maxFixtureIndex);
        }

        private void RefreshFixtureList()
        {
            RefreshFixtures(false);
        }

        private void RefreshFixtures(bool reinitializeFixtures)
        {
            StopAllTests();
            var previousGroupIndex = _selectedGroupIndex;
            var previousFixtureIndex = _selectedFixtureIndex;
            var previousChannelKey = _selectedChannelKey;
            _groups.Clear();
            var fixtures = DmxFixtureEditorUtility.FindFixturesInScene();

            if (reinitializeFixtures)
            {
                foreach (var fixture in fixtures)
                {
                    if (fixture == null)
                    {
                        continue;
                    }

                    fixture.ReinitializeFixture();
                    EditorUtility.SetDirty(fixture);
                }
            }

            foreach (var grouping in fixtures.GroupBy(DmxFixtureEditorUtility.BuildGroupKey))
            {
                var fixtureList = grouping.ToList();
                var first = fixtureList[0];
                _groups.Add(new FixtureGroupView
                {
                    Key = grouping.Key,
                    Label = DmxFixtureEditorUtility.BuildGroupLabel(first),
                    Fixtures = fixtureList,
                    Universe = first.Universe,
                    StartAddress = first.StartAddress
                });
            }

            _groups.Sort((a, b) => string.CompareOrdinal(a.Label, b.Label));
            _selectedGroupIndex = Mathf.Clamp(previousGroupIndex, 0, Mathf.Max(0, _groups.Count - 1));
            var selectedFixtures = _groups.Count > 0 ? _groups[_selectedGroupIndex].Fixtures : null;
            var maxFixtureIndex = selectedFixtures == null ? 0 : Mathf.Max(0, selectedFixtures.Count - 1);
            _selectedFixtureIndex = Mathf.Clamp(previousFixtureIndex, 0, maxFixtureIndex);
            _selectedChannelKey = previousChannelKey;
            Repaint();
        }

        private void DrawGroupSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Scene Fixtures", EditorStyles.boldLabel);
                var groupNames = _groups.Select(group => $"{group.Label} ({group.Fixtures.Count})").ToArray();
                var previousGroupIndex = _selectedGroupIndex;
                _selectedGroupIndex = GUILayout.Toolbar(_selectedGroupIndex, groupNames);
                if (_selectedGroupIndex != previousGroupIndex)
                {
                    _selectedFixtureIndex = 0;
                    _selectedChannelKey = null;
                }

                var selectedGroup = _groups[_selectedGroupIndex];
                EditorGUILayout.LabelField("Selected Group", selectedGroup.Label);
                EditorGUILayout.LabelField("Fixture Count", selectedGroup.Fixtures.Count.ToString());
                EditorGUILayout.LabelField("Total Footprint", $"{selectedGroup.Fixtures.Sum(fixture => fixture.ChannelFootprint)} ch");

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Address Patch", EditorStyles.boldLabel);
                selectedGroup.Universe = (ushort)EditorGUILayout.IntField("Patch Universe", selectedGroup.Universe);
                selectedGroup.StartAddress = (ushort)EditorGUILayout.IntField("Patch Start Address", selectedGroup.StartAddress);

                if (GUILayout.Button("Apply Sequential Patch"))
                {
                    DmxFixtureEditorUtility.PatchSequentially(selectedGroup.Fixtures, selectedGroup.Universe, selectedGroup.StartAddress);
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Fixtures in Group", EditorStyles.boldLabel);
                DrawFixtureTable(selectedGroup);
            }
        }

        private void DrawFixtureTable(FixtureGroupView selectedGroup)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Select", GUILayout.Width(45));
                GUILayout.Label("Name", GUILayout.Width(180));
                GUILayout.Label("Profile", GUILayout.Width(140));
                GUILayout.Label("Universe", GUILayout.Width(60));
                GUILayout.Label("Start", GUILayout.Width(60));
                GUILayout.Label("Footprint", GUILayout.Width(70));
            }

            for (var i = 0; i < selectedGroup.Fixtures.Count; i++)
            {
                var fixture = selectedGroup.Fixtures[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    var isSelected = i == _selectedFixtureIndex;
                    if (GUILayout.Toggle(isSelected, string.Empty, GUILayout.Width(45)) && !isSelected)
                    {
                        _selectedFixtureIndex = i;
                        _selectedChannelKey = null;
                    }

                    GUILayout.Label(fixture.name, GUILayout.Width(180));
                    GUILayout.Label(fixture.ProfileName, GUILayout.Width(140));
                    GUILayout.Label(fixture.Universe.ToString(), GUILayout.Width(60));
                    GUILayout.Label((fixture.StartAddress + 1).ToString(), GUILayout.Width(60));
                    GUILayout.Label(fixture.ChannelFootprint.ToString(), GUILayout.Width(70));
                }
            }
        }

        private void DrawChannelTestSection()
        {
            var selectedGroup = _groups[_selectedGroupIndex];
            if (selectedGroup.Fixtures.Count == 0)
            {
                return;
            }

            _selectedFixtureIndex = Mathf.Clamp(_selectedFixtureIndex, 0, selectedGroup.Fixtures.Count - 1);
            var selectedFixture = selectedGroup.Fixtures[Mathf.Clamp(_selectedFixtureIndex, 0, selectedGroup.Fixtures.Count - 1)];
            var patchedChannels = selectedFixture.GetPatchedChannels().ToList();
            if (patchedChannels.Count > 0 && string.IsNullOrEmpty(_selectedChannelKey))
            {
                _selectedChannelKey = BuildClipKey(selectedFixture, patchedChannels[0].AbsoluteChannel);
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Channel Test", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Fixture", selectedFixture.name);
                EditorGUILayout.LabelField("Profile", selectedFixture.ProfileName);
                EditorGUILayout.LabelField("Signature", DmxFixtureEditorUtility.BuildSignature(selectedFixture));
                EditorGUILayout.LabelField("Active Tests", _testClips.Values.Count(clip => clip.Playing).ToString());

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Channels", EditorStyles.boldLabel);
                DrawChannelList(selectedFixture, patchedChannels);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Selected Channel", EditorStyles.boldLabel);
                DrawSelectedChannelEditor(selectedFixture, patchedChannels);
            }
        }

        private void DrawChannelList(DmxFixture fixture, List<DmxPatchedChannel> patchedChannels)
        {
            foreach (var patchedChannel in patchedChannels)
            {
                var clip = GetOrCreateClip(fixture, patchedChannel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    var key = BuildClipKey(fixture, patchedChannel.AbsoluteChannel);
                    var selected = key == _selectedChannelKey;
                    var label = $"{patchedChannel.Descriptor.Name}  ({patchedChannel.Module.GetType().Name}, Abs {patchedChannel.AbsoluteChannel + 1})";
                    if (GUILayout.Toggle(selected, label, "Button"))
                    {
                        _selectedChannelKey = key;
                    }

                    GUILayout.Label(clip.Playing ? "Playing" : string.Empty, GUILayout.Width(55));
                }
            }
        }

        private void DrawSelectedChannelEditor(DmxFixture fixture, List<DmxPatchedChannel> patchedChannels)
        {
            if (patchedChannels.Count == 0)
            {
                EditorGUILayout.HelpBox("This fixture has no patched channels.", MessageType.Info);
                return;
            }

            var selectedPatchedChannel = patchedChannels.FirstOrDefault(channel =>
                BuildClipKey(fixture, channel.AbsoluteChannel) == _selectedChannelKey);

            if (selectedPatchedChannel.Module == null)
            {
                selectedPatchedChannel = patchedChannels[0];
                _selectedChannelKey = BuildClipKey(fixture, selectedPatchedChannel.AbsoluteChannel);
            }

            var clip = GetOrCreateClip(fixture, selectedPatchedChannel);

            EditorGUILayout.LabelField("Channel", selectedPatchedChannel.Descriptor.Name);
            EditorGUILayout.LabelField("Module", selectedPatchedChannel.Module.GetType().Name);
            EditorGUILayout.LabelField("Absolute Channel", (selectedPatchedChannel.AbsoluteChannel + 1).ToString());
            clip.Waveform = (TestWaveform)EditorGUILayout.EnumPopup("Waveform", clip.Waveform);
            clip.Min = EditorGUILayout.Slider("DMX Min", clip.Min, 0f, 255f);
            clip.Max = EditorGUILayout.Slider("DMX Max", clip.Max, 0f, 255f);
            clip.Speed = EditorGUILayout.Slider("Animation Speed", clip.Speed, 0.1f, 10f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (!clip.Playing)
                {
                    if (GUILayout.Button("Play"))
                    {
                        clip.Playing = true;
                    }
                }
                else if (GUILayout.Button("Stop"))
                {
                    clip.Playing = false;
                    _fixturesToClear.Add(fixture.GetInstanceID());
                }

                if (GUILayout.Button("Stop All"))
                {
                    StopAllTests();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Full On"))
                {
                    clip.Waveform = TestWaveform.Constant;
                    clip.Min = 255f;
                    clip.Max = 255f;
                    clip.Playing = true;
                }

                if (GUILayout.Button("PingPong 0-255"))
                {
                    clip.Waveform = TestWaveform.PingPong;
                    clip.Min = 0f;
                    clip.Max = 255f;
                    clip.Playing = true;
                }

                if (GUILayout.Button("Sine 0-255"))
                {
                    clip.Waveform = TestWaveform.Sine;
                    clip.Min = 0f;
                    clip.Max = 255f;
                    clip.Playing = true;
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Center"))
                {
                    clip.Waveform = TestWaveform.Constant;
                    clip.Min = 127f;
                    clip.Max = 127f;
                    clip.Playing = true;
                }

                if (GUILayout.Button("Off"))
                {
                    clip.Waveform = TestWaveform.Constant;
                    clip.Min = 0f;
                    clip.Max = 0f;
                    clip.Playing = true;
                }
            }
        }

        private TestClip GetOrCreateClip(DmxFixture fixture, DmxPatchedChannel patchedChannel)
        {
            var key = BuildClipKey(fixture, patchedChannel.AbsoluteChannel);
            if (_testClips.TryGetValue(key, out var clip))
            {
                return clip;
            }

            clip = new TestClip
            {
                Fixture = fixture,
                AbsoluteChannel = patchedChannel.AbsoluteChannel,
                RelativeChannel = patchedChannel.AbsoluteChannel - fixture.StartAddress,
                DisplayName = $"{patchedChannel.Module.GetType().Name}/{patchedChannel.Descriptor.Name}"
            };
            _testClips[key] = clip;
            return clip;
        }

        private static string BuildClipKey(DmxFixture fixture, int absoluteChannel)
        {
            return $"{fixture.GetInstanceID()}:{absoluteChannel}";
        }

        private void DrawDebugSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _showDebug = EditorGUILayout.Foldout(_showDebug, "Debug", true);
                if (!_showDebug)
                {
                    return;
                }

                foreach (var clip in _testClips.Values.OrderBy(clip => clip.DisplayName))
                {
                    if (clip.Fixture == null)
                    {
                        continue;
                    }

                    var fixtureId = clip.Fixture.GetInstanceID();
                    var hasBuffer = _fixtureBuffers.TryGetValue(fixtureId, out var buffer);
                    var currentByte = hasBuffer && clip.RelativeChannel >= 0 && clip.RelativeChannel < buffer.Length
                        ? buffer[clip.RelativeChannel]
                        : (byte)0;

                    EditorGUILayout.LabelField(
                        $"{clip.Fixture.name} | {clip.DisplayName} | Play:{clip.Playing} | Abs:{clip.AbsoluteChannel + 1} | Rel:{clip.RelativeChannel} | Buffer:{(hasBuffer ? buffer.Length : 0)} | Value:{currentByte}");
                }
            }
        }

        private void OnEditorUpdate()
        {
            var time = EditorApplication.timeSinceStartup;
            var activeFixtures = new Dictionary<int, DmxFixture>();

            foreach (var clip in _testClips.Values)
            {
                if (!clip.Playing || clip.Fixture == null)
                {
                    continue;
                }

                var fixtureId = clip.Fixture.GetInstanceID();
                activeFixtures[fixtureId] = clip.Fixture;

                if (!_fixtureBuffers.TryGetValue(fixtureId, out var buffer) || buffer.Length != clip.Fixture.ChannelFootprint)
                {
                    buffer = new byte[Mathf.Max(clip.Fixture.ChannelFootprint, 1)];
                    _fixtureBuffers[fixtureId] = buffer;
                }
            }

            foreach (var pair in activeFixtures)
            {
                Array.Clear(_fixtureBuffers[pair.Key], 0, _fixtureBuffers[pair.Key].Length);
            }

            foreach (var clip in _testClips.Values)
            {
                if (!clip.Playing || clip.Fixture == null)
                {
                    continue;
                }

                var buffer = _fixtureBuffers[clip.Fixture.GetInstanceID()];
                var relativeChannel = clip.AbsoluteChannel - clip.Fixture.StartAddress;
                clip.RelativeChannel = relativeChannel;
                if (relativeChannel < 0 || relativeChannel >= buffer.Length)
                {
                    continue;
                }

                buffer[relativeChannel] = (byte)Mathf.Clamp(
                    Mathf.RoundToInt(EvaluateClip(clip, time)),
                    0,
                    255);
            }

            foreach (var pair in activeFixtures)
            {
                pair.Value.DmxUpdate(_fixtureBuffers[pair.Key]);
                EditorUtility.SetDirty(pair.Value);
            }

            foreach (var fixtureId in _fixturesToClear.ToArray())
            {
                if (!_fixtureBuffers.TryGetValue(fixtureId, out var buffer))
                {
                    continue;
                }

                Array.Clear(buffer, 0, buffer.Length);
                var fixture = _testClips.Values.Select(clip => clip.Fixture).FirstOrDefault(f => f != null && f.GetInstanceID() == fixtureId);
                if (fixture != null)
                {
                    fixture.DmxUpdate(buffer);
                    EditorUtility.SetDirty(fixture);
                }
                _fixturesToClear.Remove(fixtureId);
            }

            if (activeFixtures.Count > 0)
            {
                SceneView.RepaintAll();
                Repaint();
            }
        }

        private static float EvaluateClip(TestClip clip, double time)
        {
            var t = (float)(time * Math.Max(clip.Speed, 0.01f));
            return clip.Waveform switch
            {
                TestWaveform.Constant => clip.Max,
                TestWaveform.PingPong => Mathf.Lerp(clip.Min, clip.Max, Mathf.PingPong(t, 1f)),
                TestWaveform.Sine => Mathf.Lerp(clip.Min, clip.Max, Mathf.Sin(t) * 0.5f + 0.5f),
                TestWaveform.Step => Mathf.Sin(t) >= 0f ? clip.Max : clip.Min,
                _ => clip.Min
            };
        }

        private void StopAllTests()
        {
            foreach (var clip in _testClips.Values)
            {
                clip.Playing = false;
            }

            foreach (var pair in _fixtureBuffers)
            {
                Array.Clear(pair.Value, 0, pair.Value.Length);
            }
        }
    }
}
