using System.Collections.Generic;
using System.Reflection;
using ArtNet.Devices;
using ArtNet.Devices.Modular;
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
    }
}
