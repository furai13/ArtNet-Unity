using ArtNet.Devices.Modular;
using UnityEditor;
using UnityEngine;

namespace ArtNet.Editor
{
    [CustomEditor(typeof(DmxFixture))]
    public class ModularDmxFixtureInspector : UnityEditor.Editor
    {
        private bool _showModuleInputs = true;
        private bool _showState = true;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var fixture = (DmxFixture)target;

            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Channel Tools", EditorStyles.boldLabel);

                if (GUILayout.Button("Register Child Modules"))
                {
                    Undo.RecordObject(fixture, "Register DMX Modules");
                    fixture.RegisterChildModules();
                    EditorUtility.SetDirty(fixture);
                }

                if (GUILayout.Button("Auto Assign Channels"))
                {
                    Undo.RecordObject(fixture, "Auto Assign DMX Channels");
                    fixture.AutoAssignModuleChannels();
                    EditorUtility.SetDirty(fixture);
                }

                if (GUILayout.Button("Register Child Outputs"))
                {
                    Undo.RecordObject(fixture, "Register Fixture Outputs");
                    fixture.RegisterChildOutputs();
                    EditorUtility.SetDirty(fixture);
                }
            }

            DrawModuleInputSection(fixture);
            DrawStateSection(fixture);

            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        private void DrawModuleInputSection(DmxFixture fixture)
        {
            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _showModuleInputs = EditorGUILayout.Foldout(_showModuleInputs, "Module Input Values", true);
                if (!_showModuleInputs)
                {
                    return;
                }

                var modules = fixture.Modules;
                if (modules.Count == 0)
                {
                    EditorGUILayout.HelpBox("No modules are registered.", MessageType.Info);
                    return;
                }

                var dmxData = fixture.CurrentDmxData;
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("Last Received", FormatLastReceivedTime(fixture));
                }

                if (dmxData.IsEmpty)
                {
                    EditorGUILayout.HelpBox(
                        "No DMX data has been received or initialized yet. Channel mappings are shown without values.",
                        MessageType.Info);
                }

                for (var i = 0; i < modules.Count; i++)
                {
                    DrawModuleInput(fixture, modules[i], i, dmxData);
                }
            }
        }

        private static void DrawModuleInput(
            DmxFixture fixture,
            DmxFixture.ModuleEntry entry,
            int moduleIndex,
            System.ReadOnlySpan<byte> dmxData)
        {
            EditorGUILayout.Space(3f);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            using (new EditorGUI.DisabledScope(true))
            {
                var moduleLabel = entry.module != null
                    ? $"{moduleIndex}: {entry.module.GetType().Name}"
                    : $"{moduleIndex}: Missing Module";
                EditorGUILayout.LabelField(moduleLabel, EditorStyles.boldLabel);

                if (entry.module == null)
                {
                    return;
                }

                EditorGUILayout.ObjectField("Module", entry.module, typeof(DmxModuleBase), true);
                EditorGUILayout.IntField("Offset", entry.offset);
                EditorGUILayout.IntField("Channel Count", entry.module.ChannelCount);

                var hasValues = TryGetModuleValueRange(entry, dmxData, out var valueStart, out var valueCount);
                if (hasValues)
                {
                    EditorGUILayout.TextField("Passed Slice", FormatByteRange(dmxData, valueStart, valueCount));
                }
                else
                {
                    EditorGUILayout.TextField("Passed Slice", "-");
                    if (!dmxData.IsEmpty)
                    {
                        EditorGUILayout.HelpBox(
                            $"Offset {entry.offset} + count {entry.module.ChannelCount} is outside the current DMX buffer length {dmxData.Length}.",
                            MessageType.Warning);
                    }
                }

                var descriptors = entry.module.GetChannelDescriptors();
                if (descriptors == null || descriptors.Count == 0)
                {
                    DrawRawChannels(fixture, entry, dmxData);
                    return;
                }

                foreach (var descriptor in descriptors)
                {
                    DrawChannelValue(fixture, entry, descriptor.Name, descriptor.RelativeOffset, dmxData);
                }
            }
        }

        private static void DrawRawChannels(DmxFixture fixture, DmxFixture.ModuleEntry entry, System.ReadOnlySpan<byte> dmxData)
        {
            for (var relativeOffset = 0; relativeOffset < entry.module.ChannelCount; relativeOffset++)
            {
                DrawChannelValue(fixture, entry, $"Channel {relativeOffset + 1}", relativeOffset, dmxData);
            }
        }

        private static void DrawChannelValue(
            DmxFixture fixture,
            DmxFixture.ModuleEntry entry,
            string channelName,
            int relativeOffset,
            System.ReadOnlySpan<byte> dmxData)
        {
            var fixtureChannel = entry.offset + relativeOffset;
            var absoluteChannel = fixture.StartAddress + fixtureChannel;
            var label = $"{channelName}  (Rel {relativeOffset}, Fixture {fixtureChannel + 1}, Abs {absoluteChannel + 1})";
            var valueIndex = entry.offset + relativeOffset;
            var valueText = valueIndex >= 0 && valueIndex < dmxData.Length
                ? $"{dmxData[valueIndex]}  (0x{dmxData[valueIndex]:X2})"
                : "-";

            EditorGUILayout.TextField(label, valueText);
        }

        private static bool TryGetModuleValueRange(
            DmxFixture.ModuleEntry entry,
            System.ReadOnlySpan<byte> dmxData,
            out int start,
            out int count)
        {
            start = entry.offset;
            count = entry.module != null ? entry.module.ChannelCount : 0;
            return start >= 0 && count >= 0 && start + count <= dmxData.Length;
        }

        private static string FormatByteRange(System.ReadOnlySpan<byte> data, int start, int count)
        {
            if (count <= 0)
            {
                return string.Empty;
            }

            var values = new string[count];
            for (var i = 0; i < count; i++)
            {
                values[i] = data[start + i].ToString();
            }

            return string.Join(", ", values);
        }

        private static string FormatLastReceivedTime(DmxFixture fixture)
        {
            if (!fixture.HasReceivedDmx)
            {
                return "Never";
            }

            var secondsAgo = Mathf.Max(0f, fixture.SecondsSinceLastReceived);
            return $"{secondsAgo:0.000}s ago  (realtime {fixture.LastReceivedRealtimeSinceStartup:0.000}s)";
        }

        private void DrawStateSection(DmxFixture fixture)
        {
            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _showState = EditorGUILayout.Foldout(_showState, "Current State", true);
                if (!_showState)
                {
                    return;
                }

                using (new EditorGUI.DisabledScope(true))
                {
                    var state = fixture.State;
                    EditorGUILayout.ColorField("Color", state.Color);
                    EditorGUILayout.FloatField("Dimmer", state.Dimmer);
                    EditorGUILayout.FloatField("Pan Normalized", state.PanNormalized);
                    EditorGUILayout.FloatField("Tilt Normalized", state.TiltNormalized);
                    EditorGUILayout.FloatField("Beam Angle", state.BeamAngleNormalized);
                    EditorGUILayout.Toggle("Strobe Enabled", state.StrobeEnabled);
                    EditorGUILayout.FloatField("Strobe Rate Hz", state.StrobeRateHz);
                    EditorGUILayout.Toggle("Gobo Open", state.GoboOpen);
                    EditorGUILayout.IntField("Gobo Index", state.GoboIndex);
                    EditorGUILayout.Toggle("Gobo Rotate", state.GoboRotate);
                    EditorGUILayout.FloatField("Gobo Rotation Speed", state.GoboRotationSpeedDegPerSecond);
                    EditorGUILayout.FloatField("Gobo Rotation Angle", state.GoboRotationAngle);
                }
            }
        }
    }
}
