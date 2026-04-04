using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ArtNet.Devices;
using ArtNet.Devices.Modular;
using ArtNet.Devices.Modular.Output;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Devices.Module
{
    public class ModularDmxFixtureModuleTest
    {
        private readonly List<Object> _createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var createdObject in _createdObjects)
            {
                if (createdObject != null)
                {
                    Object.DestroyImmediate(createdObject);
                }
            }

            _createdObjects.Clear();
        }

        [Test]
        public void DmxUpdate_AppliesModuleOffsetsToFixtureState()
        {
            var root = CreateGameObject("Fixture");
            var fixture = root.AddComponent<DmxFixture>();
            var color = root.AddComponent<ColorModule>();
            var dimmer = root.AddComponent<DimmerModule>();

            fixture.SetModules(new[]
            {
                fixture.CreateEntry(color, 0),
                fixture.CreateEntry(dimmer, 3)
            });

            InitializeFixture(fixture);

            fixture.DmxUpdate(new byte[] { 255, 128, 0, 64 });

            Assert.That(fixture.State.Color.r, Is.EqualTo(1f).Within(0.001f));
            Assert.That(fixture.State.Color.g, Is.EqualTo(128f / 255f).Within(0.001f));
            Assert.That(fixture.State.Color.b, Is.EqualTo(0f).Within(0.001f));
            Assert.That(fixture.State.Dimmer, Is.EqualTo(64f / 255f).Within(0.001f));
        }

        [Test]
        public void ColorModules_CanMultiplyInsteadOfOverwrite()
        {
            var root = CreateGameObject("Fixture");
            var fixture = root.AddComponent<DmxFixture>();
            var color = root.AddComponent<ColorModule>();
            var colorWheel = root.AddComponent<ColorWheelModule>();

            color.Configure(ColorBlendMode.Overwrite);
            colorWheel.Configure(new[] { Color.red, Color.green, Color.blue }, ColorBlendMode.Multiply);

            fixture.SetModules(new[]
            {
                fixture.CreateEntry(color, 0),
                fixture.CreateEntry(colorWheel, 3)
            });

            InitializeFixture(fixture);
            fixture.DmxUpdate(new byte[] { 255, 128, 64, 0 });

            Assert.That(fixture.State.Color.r, Is.EqualTo(1f).Within(0.001f));
            Assert.That(fixture.State.Color.g, Is.EqualTo(0f).Within(0.001f));
            Assert.That(fixture.State.Color.b, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void GetPatchedChannels_ReturnsNamedChannelsFromModules()
        {
            var root = CreateGameObject("Fixture");
            var fixture = root.AddComponent<DmxFixture>();
            fixture.SetAddressPatch(2, 10);
            fixture.SetFixtureMetadata("Wash", "Front");

            var color = root.AddComponent<ColorModule>();
            var dimmer = root.AddComponent<DimmerModule>();

            fixture.SetModules(new[]
            {
                fixture.CreateEntry(color, 0),
                fixture.CreateEntry(dimmer, 3)
            });

            var channels = fixture.GetPatchedChannels();

            Assert.That(channels.Count, Is.EqualTo(4));
            Assert.That(channels[0].Descriptor.Name, Is.EqualTo("Red"));
            Assert.That(channels[0].AbsoluteChannel, Is.EqualTo(10));
            Assert.That(channels[3].Descriptor.Name, Is.EqualTo("Dimmer"));
            Assert.That(channels[3].AbsoluteChannel, Is.EqualTo(13));
            Assert.That(fixture.ProfileName, Is.EqualTo("Wash"));
            Assert.That(fixture.GroupId, Is.EqualTo("Front"));
        }

        [Test]
        public void AutoAssignModuleChannels_AssignsSequentialOffsets()
        {
            var root = CreateGameObject("Fixture");
            var fixture = root.AddComponent<DmxFixture>();
            var color = root.AddComponent<ColorModule>();
            var dimmer = root.AddComponent<DimmerModule>();
            var panTilt = root.AddComponent<PanTiltModule>();
            var output = root.AddComponent<LightOutput>();

            fixture.SetModules(new[]
            {
                fixture.CreateEntry(color, 99),
                fixture.CreateEntry(dimmer, 99),
                fixture.CreateEntry(panTilt, 99)
            });
            fixture.SetOutputs(new FixtureOutputBase[] { output });

            fixture.AutoAssignModuleChannels();
            var modules = fixture.Modules;

            Assert.That(modules[0].offset, Is.EqualTo(0));
            Assert.That(modules[1].offset, Is.EqualTo(3));
            Assert.That(modules[2].offset, Is.EqualTo(4));
        }

        [Test]
        public void DmxModuleBase_HasNoQuickActionsByDefault()
        {
            var root = CreateGameObject("Fixture");
            var passthrough = root.AddComponent<TestModuleWithoutQuickActions>();

            Assert.That(passthrough.GetQuickActionDefinitions(), Is.Empty);
            Assert.That(
                passthrough.TryBuildQuickAction(DmxQuickActionIds.Blackout, new List<DmxQuickActionOverride>()),
                Is.False);
        }

        [Test]
        public void DimmerModule_ExposesBlackoutAndFullQuickActions()
        {
            var root = CreateGameObject("Fixture");
            var dimmer = root.AddComponent<DimmerModule>();

            var quickActions = dimmer.GetQuickActionDefinitions().ToArray();

            Assert.That(quickActions.Select(action => action.Id), Is.EqualTo(new[]
            {
                DmxQuickActionIds.Blackout,
                DmxQuickActionIds.Full
            }));
            Assert.That(quickActions.Select(action => action.Label), Is.EqualTo(new[]
            {
                "Blackout",
                "Full"
            }));
        }

        [Test]
        public void DimmerModule_BuildsExpectedQuickActionOverrides()
        {
            var root = CreateGameObject("Fixture");
            var dimmer = root.AddComponent<DimmerModule>();

            var blackout = new List<DmxQuickActionOverride>();
            var full = new List<DmxQuickActionOverride>();

            Assert.That(dimmer.TryBuildQuickAction(DmxQuickActionIds.Blackout, blackout), Is.True);
            Assert.That(dimmer.TryBuildQuickAction(DmxQuickActionIds.Full, full), Is.True);
            Assert.That(dimmer.TryBuildQuickAction("unknown", new List<DmxQuickActionOverride>()), Is.False);

            Assert.That(blackout.Count, Is.EqualTo(1));
            Assert.That(blackout[0].RelativeChannel, Is.EqualTo(0));
            Assert.That(blackout[0].Value, Is.EqualTo((byte)0));

            Assert.That(full.Count, Is.EqualTo(1));
            Assert.That(full[0].RelativeChannel, Is.EqualTo(0));
            Assert.That(full[0].Value, Is.EqualTo(byte.MaxValue));
        }

        [Test]
        public void ColorModule_BuildsExpectedQuickActionOverrides()
        {
            var root = CreateGameObject("Fixture");
            var color = root.AddComponent<ColorModule>();

            var blackout = new List<DmxQuickActionOverride>();
            var white = new List<DmxQuickActionOverride>();

            Assert.That(color.TryBuildQuickAction(DmxQuickActionIds.Blackout, blackout), Is.True);
            Assert.That(color.TryBuildQuickAction(DmxQuickActionIds.White, white), Is.True);

            Assert.That(blackout.Select(x => x.RelativeChannel), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(blackout.Select(x => x.Value), Is.EqualTo(new byte[] { 0, 0, 0 }));
            Assert.That(white.Select(x => x.RelativeChannel), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(white.Select(x => x.Value), Is.EqualTo(new[] { byte.MaxValue, byte.MaxValue, byte.MaxValue }));
        }

        [Test]
        public void PanTiltModule_CenterQuickAction_UsesExpectedChannels()
        {
            var root = CreateGameObject("Fixture");
            var panTilt = root.AddComponent<PanTiltModule>();

            var overrides = new List<DmxQuickActionOverride>();

            Assert.That(panTilt.TryBuildQuickAction(DmxQuickActionIds.Center, overrides), Is.True);
            Assert.That(overrides.Select(x => x.RelativeChannel), Is.EqualTo(new[] { 0, 1, 2, 3 }));
            Assert.That(overrides.Select(x => x.Value), Is.EqualTo(new byte[] { 128, 0, 128, 0 }));
        }

        [Test]
        public void StrobeModule_BuildsExpectedQuickActionOverrides()
        {
            var root = CreateGameObject("Fixture");
            var strobe = root.AddComponent<StrobeModule>();

            var stop = new List<DmxQuickActionOverride>();
            var full = new List<DmxQuickActionOverride>();

            Assert.That(strobe.TryBuildQuickAction(DmxQuickActionIds.Stop, stop), Is.True);
            Assert.That(strobe.TryBuildQuickAction(DmxQuickActionIds.Full, full), Is.True);
            Assert.That(stop.Count, Is.EqualTo(1));
            Assert.That(stop[0].RelativeChannel, Is.EqualTo(0));
            Assert.That(stop[0].Value, Is.EqualTo((byte)15));
            Assert.That(full.Count, Is.EqualTo(1));
            Assert.That(full[0].Value, Is.EqualTo(byte.MaxValue));
        }

        [Test]
        public void GoboModule_BuildsOpenAndStopQuickActionOverrides()
        {
            var root = CreateGameObject("Fixture");
            var gobo = root.AddComponent<GoboModule>();
            gobo.Configure(4, true, 7, 180f);

            var open = new List<DmxQuickActionOverride>();
            var stop = new List<DmxQuickActionOverride>();

            Assert.That(gobo.TryBuildQuickAction(DmxQuickActionIds.Open, open), Is.True);
            Assert.That(gobo.TryBuildQuickAction(DmxQuickActionIds.Stop, stop), Is.True);
            Assert.That(open.Count, Is.EqualTo(1));
            Assert.That(open[0].RelativeChannel, Is.EqualTo(0));
            Assert.That(open[0].Value, Is.EqualTo((byte)7));
            Assert.That(stop.Count, Is.EqualTo(1));
            Assert.That(stop[0].RelativeChannel, Is.EqualTo(1));
            Assert.That(stop[0].Value, Is.EqualTo((byte)127));
        }

        [Test]
        public void FunctionChannelModule_ResetQuickAction_UsesResetRangeMidpoint()
        {
            var root = CreateGameObject("Fixture");
            var function = root.AddComponent<FunctionChannelModule>();
            function.Configure(new[]
            {
                new FunctionChannelModule.FunctionRange
                {
                    label = "Reset",
                    min = 200,
                    max = 255,
                    action = FunctionChannelModule.FunctionAction.ResetFixture
                }
            }, true);

            var overrides = new List<DmxQuickActionOverride>();

            Assert.That(function.TryBuildQuickAction(DmxQuickActionIds.Reset, overrides), Is.True);
            Assert.That(overrides.Count, Is.EqualTo(1));
            Assert.That(overrides[0].RelativeChannel, Is.EqualTo(0));
            Assert.That(overrides[0].Value, Is.EqualTo((byte)227));
        }

        [Test]
        public void DirectValueModule_MapsDmxToUnityEvent()
        {
            var root = CreateGameObject("Fixture");
            var fixture = root.AddComponent<DmxFixture>();
            var directValue = root.AddComponent<DirectValueModule>();
            var receivedValue = -1f;

            directValue.Configure(DirectValueModule.ReadMode.Byte, new Vector2(0f, 255f), new Vector2(10f, 20f), false);
            directValue.AddListener(value => receivedValue = value);

            fixture.SetModules(new[]
            {
                fixture.CreateEntry(directValue, 0)
            });

            InitializeFixture(fixture);
            fixture.DmxUpdate(new byte[] { 128 });

            Assert.That(receivedValue, Is.EqualTo(Mathf.Lerp(10f, 20f, 128f / 255f)).Within(0.01f));
        }

        [Test]
        public void LightOutput_AppliesPanAndTiltUsingLocalRotations()
        {
            var root = CreateGameObject("Fixture");
            var panAxis = CreateGameObject("PanAxis").transform;
            panAxis.SetParent(root.transform, false);

            var tiltAxis = CreateGameObject("TiltAxis").transform;
            tiltAxis.SetParent(panAxis, false);

            var lightGo = CreateGameObject("Light");
            lightGo.transform.SetParent(tiltAxis, false);
            var light = lightGo.AddComponent<Light>();

            var fixture = root.AddComponent<DmxFixture>();
            var panTilt = root.AddComponent<PanTiltModule>();
            var output = root.AddComponent<LightOutput>();
            output.Configure(light, panAxis, tiltAxis, new Vector2(10f, 60f), new Vector2(-90f, 90f), new Vector2(-45f, 45f), 1f);

            fixture.SetModules(new[]
            {
                fixture.CreateEntry(panTilt, 0)
            });
            fixture.SetOutputs(new FixtureOutputBase[] { output });

            InitializeFixture(fixture);
            fixture.DmxUpdate(new byte[] { 255, 255, 0, 0 });

            var expectedPan = Quaternion.AngleAxis(90f, Vector3.up);
            var expectedTilt = Quaternion.AngleAxis(-45f, Vector3.right);

            AssertQuaternionApproximately(panAxis.localRotation, expectedPan, 0.001f);
            AssertQuaternionApproximately(tiltAxis.localRotation, expectedTilt, 0.001f);
        }

        [Test]
        public void LightOutput_CanDriveMultipleLightsWithDimmerCurve()
        {
            var root = CreateGameObject("Fixture");
            var lightA = CreateGameObject("LightA").AddComponent<Light>();
            var lightB = CreateGameObject("LightB").AddComponent<Light>();
            lightA.transform.SetParent(root.transform, false);
            lightB.transform.SetParent(root.transform, false);

            var fixture = root.AddComponent<DmxFixture>();
            var color = root.AddComponent<ColorModule>();
            var dimmer = root.AddComponent<DimmerModule>();
            var output = root.AddComponent<LightOutput>();
            output.Configure(new[] { lightA, lightB }, null, null, new Vector2(10f, 60f), new Vector2(-90f, 90f), new Vector2(-45f, 45f), 4f);
            SetPrivateField(
                output,
                typeof(ColorDimmerOutputBase),
                "dimmerCurve",
                new AnimationCurve(
                    new Keyframe(0f, 0f),
                    new Keyframe(0.5f, 0.25f),
                    new Keyframe(1f, 1f)));

            fixture.SetModules(new[]
            {
                fixture.CreateEntry(color, 0),
                fixture.CreateEntry(dimmer, 3)
            });
            fixture.SetOutputs(new FixtureOutputBase[] { output });

            InitializeFixture(fixture);
            fixture.DmxUpdate(new byte[] { 255, 64, 0, 128 });

            var expectedDimmer = 0.25f * 4f;
            Assert.That(lightA.color.r, Is.EqualTo(1f).Within(0.001f));
            Assert.That(lightA.color.g, Is.EqualTo(64f / 255f).Within(0.001f));
            Assert.That(lightA.intensity, Is.EqualTo(expectedDimmer).Within(0.02f));
            Assert.That(lightB.intensity, Is.EqualTo(expectedDimmer).Within(0.02f));
        }

        [Test]
        public void RendererPropertyBlockOutput_AppliesColorAndDimmerToAllRenderers()
        {
            var root = CreateGameObject("Fixture");
            var rendererA = CreateGameObject("RendererA").AddComponent<MeshRenderer>();
            var rendererB = CreateGameObject("RendererB").AddComponent<MeshRenderer>();
            rendererA.transform.SetParent(root.transform, false);
            rendererB.transform.SetParent(root.transform, false);

            var fixture = root.AddComponent<DmxFixture>();
            var color = root.AddComponent<ColorModule>();
            var dimmer = root.AddComponent<DimmerModule>();
            var output = root.AddComponent<RendererPropertyBlockOutput>();
            output.Configure(new Renderer[] { rendererA, rendererB }, "_DmxColor", "_DmxDimmer");

            fixture.SetModules(new[]
            {
                fixture.CreateEntry(color, 0),
                fixture.CreateEntry(dimmer, 3)
            });
            fixture.SetOutputs(new FixtureOutputBase[] { output });

            InitializeFixture(fixture);
            fixture.DmxUpdate(new byte[] { 255, 32, 128, 64 });

            var colorPropertyId = Shader.PropertyToID("_DmxColor");
            var dimmerPropertyId = Shader.PropertyToID("_DmxDimmer");
            var block = new MaterialPropertyBlock();
            rendererA.GetPropertyBlock(block);
            Assert.That(block.GetColor(colorPropertyId).r, Is.EqualTo(1f).Within(0.001f));
            Assert.That(block.GetColor(colorPropertyId).g, Is.EqualTo(32f / 255f).Within(0.001f));
            Assert.That(block.GetColor(colorPropertyId).b, Is.EqualTo(128f / 255f).Within(0.001f));
            Assert.That(block.GetFloat(dimmerPropertyId), Is.EqualTo(64f / 255f).Within(0.001f));

            rendererB.GetPropertyBlock(block);
            Assert.That(block.GetFloat(dimmerPropertyId), Is.EqualTo(64f / 255f).Within(0.001f));
        }

        [Test]
        public void RendererPropertyBlockOutput_AppliesColorAndDimmerToEveryMaterialSlot()
        {
            var root = CreateGameObject("Fixture");
            var renderer = CreateGameObject("Renderer").AddComponent<MeshRenderer>();
            renderer.transform.SetParent(root.transform, false);
            renderer.sharedMaterials = new[]
            {
                new Material(Shader.Find("Standard")),
                new Material(Shader.Find("Standard"))
            };
            _createdObjects.Add(renderer.sharedMaterials[0]);
            _createdObjects.Add(renderer.sharedMaterials[1]);

            var fixture = root.AddComponent<DmxFixture>();
            var color = root.AddComponent<ColorModule>();
            var dimmer = root.AddComponent<DimmerModule>();
            var output = root.AddComponent<RendererPropertyBlockOutput>();
            output.Configure(new Renderer[] { renderer }, "_DmxColor", "_DmxDimmer");

            fixture.SetModules(new[]
            {
                fixture.CreateEntry(color, 0),
                fixture.CreateEntry(dimmer, 3)
            });
            fixture.SetOutputs(new FixtureOutputBase[] { output });

            InitializeFixture(fixture);
            fixture.DmxUpdate(new byte[] { 255, 128, 64, 32 });

            var colorPropertyId = Shader.PropertyToID("_DmxColor");
            var dimmerPropertyId = Shader.PropertyToID("_DmxDimmer");
            var block = new MaterialPropertyBlock();

            renderer.GetPropertyBlock(block, 0);
            Assert.That(block.GetColor(colorPropertyId).g, Is.EqualTo(128f / 255f).Within(0.001f));
            Assert.That(block.GetFloat(dimmerPropertyId), Is.EqualTo(32f / 255f).Within(0.001f));

            renderer.GetPropertyBlock(block, 1);
            Assert.That(block.GetColor(colorPropertyId).b, Is.EqualTo(64f / 255f).Within(0.001f));
            Assert.That(block.GetFloat(dimmerPropertyId), Is.EqualTo(32f / 255f).Within(0.001f));
        }

        [Test]
        public void RendererPropertyBlockOutput_AppliesColorAndDimmerOnlyToConfiguredMaterialSlots()
        {
            var root = CreateGameObject("Fixture");
            var renderer = CreateGameObject("Renderer").AddComponent<MeshRenderer>();
            renderer.transform.SetParent(root.transform, false);
            renderer.sharedMaterials = new[]
            {
                new Material(Shader.Find("Standard")),
                new Material(Shader.Find("Standard"))
            };
            _createdObjects.Add(renderer.sharedMaterials[0]);
            _createdObjects.Add(renderer.sharedMaterials[1]);

            var fixture = root.AddComponent<DmxFixture>();
            var color = root.AddComponent<ColorModule>();
            var dimmer = root.AddComponent<DimmerModule>();
            var output = root.AddComponent<RendererPropertyBlockOutput>();
            output.Configure(new Renderer[] { renderer }, "_DmxColor", "_DmxDimmer", new[] { 1 });

            fixture.SetModules(new[]
            {
                fixture.CreateEntry(color, 0),
                fixture.CreateEntry(dimmer, 3)
            });
            fixture.SetOutputs(new FixtureOutputBase[] { output });

            InitializeFixture(fixture);
            fixture.DmxUpdate(new byte[] { 255, 128, 64, 32 });

            var colorPropertyId = Shader.PropertyToID("_DmxColor");
            var dimmerPropertyId = Shader.PropertyToID("_DmxDimmer");
            var block = new MaterialPropertyBlock();

            renderer.GetPropertyBlock(block, 0);
            Assert.That(block.GetColor(colorPropertyId), Is.EqualTo(default(Color)));
            Assert.That(block.GetFloat(dimmerPropertyId), Is.EqualTo(0f).Within(0.001f));

            renderer.GetPropertyBlock(block, 1);
            Assert.That(block.GetColor(colorPropertyId).r, Is.EqualTo(1f).Within(0.001f));
            Assert.That(block.GetColor(colorPropertyId).g, Is.EqualTo(128f / 255f).Within(0.001f));
            Assert.That(block.GetColor(colorPropertyId).b, Is.EqualTo(64f / 255f).Within(0.001f));
            Assert.That(block.GetFloat(dimmerPropertyId), Is.EqualTo(32f / 255f).Within(0.001f));
        }

        [Test]
        public void FunctionChannelModule_CanRequestFixtureReset()
        {
            var root = CreateGameObject("Fixture");
            var light = root.AddComponent<Light>();
            var fixture = root.AddComponent<DmxFixture>();
            var color = root.AddComponent<ColorModule>();
            var function = root.AddComponent<FunctionChannelModule>();
            var output = root.AddComponent<LightOutput>();
            output.Configure(light, null, null, new Vector2(10f, 60f), new Vector2(-90f, 90f), new Vector2(-45f, 45f), 2f);
            function.Configure(new[]
            {
                new FunctionChannelModule.FunctionRange
                {
                    label = "Reset",
                    min = 200,
                    max = 255,
                    action = FunctionChannelModule.FunctionAction.ResetFixture
                }
            }, true);

            fixture.SetModules(new[]
            {
                fixture.CreateEntry(color, 0),
                fixture.CreateEntry(function, 3)
            });
            fixture.SetOutputs(new FixtureOutputBase[] { output });

            InitializeFixture(fixture);
            fixture.DmxUpdate(new byte[] { 255, 0, 0, 255 });

            Assert.That(fixture.State.Color, Is.EqualTo(Color.white));
            Assert.That(fixture.State.Dimmer, Is.EqualTo(1f).Within(0.001f));
            Assert.That(light.color, Is.EqualTo(Color.white));
            Assert.That(light.intensity, Is.EqualTo(2f).Within(0.001f));
        }

        [Test]
        public void GoboOutput_SelectsCookieFromConfiguredSlot()
        {
            var root = CreateGameObject("Fixture");
            var light = root.AddComponent<Light>();
            light.type = LightType.Spot;
            light.spotAngle = 30f;
            var fixture = root.AddComponent<DmxFixture>();
            var gobo = root.AddComponent<GoboModule>();
            var output = root.AddComponent<GoboOutput>();

            var openCookie = CreateTexture("Open", Color.white);
            var staticCookie = CreateTexture("Static", Color.red);
            var rotatedA = CreateTexture("RotA", Color.green);
            var rotatedB = CreateTexture("RotB", Color.blue);

            gobo.Configure(1, true, 7, 180f);
            output.Configure(light, openCookie, new[]
            {
                new GoboSlot
                {
                    label = "Test",
                    staticCookie = staticCookie,
                    rotatedCookies = new Texture[] { rotatedA, rotatedB }
                }
            });

            fixture.SetModules(new[]
            {
                fixture.CreateEntry(gobo, 0)
            });
            fixture.SetOutputs(new FixtureOutputBase[] { output });

            InitializeFixture(fixture);
            fixture.DmxUpdate(new byte[] { 255, 220 });
            output.Tick(1f);

            Assert.That(light.cookie, Is.Not.Null);
            Assert.That(light.cookie == rotatedA || light.cookie == rotatedB, Is.True);
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            _createdObjects.Add(gameObject);
            return gameObject;
        }

        private Texture2D CreateTexture(string name, Color color)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = name
            };
            texture.SetPixels(new[] { color, color, color, color });
            texture.Apply();
            _createdObjects.Add(texture);
            return texture;
        }

        private static void InitializeFixture(DmxFixture fixture)
        {
            var start = typeof(DmxDeviceBase).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(start, Is.Not.Null);
            start.Invoke(fixture, null);
        }

        private static void AssertQuaternionApproximately(Quaternion actual, Quaternion expected, float tolerance)
        {
            Assert.That(Quaternion.Dot(actual, expected), Is.EqualTo(1f).Within(tolerance));
        }

        private static void SetPrivateField(Object target, System.Type declaringType, string fieldName, object value)
        {
            var field = declaringType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found on {declaringType.Name}.");
            field.SetValue(target, value);
        }

        private sealed class TestModuleWithoutQuickActions : DmxModuleBase
        {
            private static readonly IReadOnlyList<DmxChannelDescriptor> Descriptors = new[]
            {
                new DmxChannelDescriptor("Value", 0)
            };

            public override int ChannelCount => 1;

            public override void Apply(in DmxFrame frame)
            {
            }

            public override IReadOnlyList<DmxChannelDescriptor> GetChannelDescriptors()
            {
                return Descriptors;
            }
        }
    }
}
