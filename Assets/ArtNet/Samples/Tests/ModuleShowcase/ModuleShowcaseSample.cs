using System;
using ArtNet.Devices.Modular;
using UnityEngine;

namespace ArtNet.Samples.Devices
{
    public class ModuleShowcaseSample : MonoBehaviour
    {
        private const string GeneratedRootName = "GeneratedModuleShowcase";

        [SerializeField] private bool rebuildOnReset = true;
        [SerializeField] private bool rebuildOnPlay = true;

        private void Reset()
        {
            if (rebuildOnReset)
            {
                RebuildSample();
            }
        }

        private void Awake()
        {
            if (Application.isPlaying && rebuildOnPlay)
            {
                RebuildSample();
            }
        }

        [ContextMenu("Rebuild Sample")]
        public void RebuildSample()
        {
            var existing = transform.Find(GeneratedRootName);
            if (existing != null)
            {
                DestroyObject(existing.gameObject);
            }

            var generatedRoot = new GameObject(GeneratedRootName);
            generatedRoot.transform.SetParent(transform, false);

            var movingHead = CreateMovingHeadFixture(generatedRoot.transform);
            var colorWheel = CreateColorWheelFixture(generatedRoot.transform);
            var directValue = CreateDirectValueFixture(generatedRoot.transform);

            var tester = GetComponent<ModuleShowcaseTester>();
            if (tester == null)
            {
                tester = gameObject.AddComponent<ModuleShowcaseTester>();
            }

            tester.Configure(movingHead, colorWheel, directValue);
        }

        private static ModularDmxFixture CreateMovingHeadFixture(Transform parent)
        {
            var fixtureRoot = new GameObject("MovingHeadFixture");
            fixtureRoot.transform.SetParent(parent, false);
            fixtureRoot.transform.localPosition = new Vector3(0f, 1.5f, 0f);

            var panAxis = new GameObject("PanAxis").transform;
            panAxis.SetParent(fixtureRoot.transform, false);

            var tiltAxis = new GameObject("TiltAxis").transform;
            tiltAxis.SetParent(panAxis, false);

            var lightGo = new GameObject("SpotLight");
            lightGo.transform.SetParent(tiltAxis, false);
            lightGo.transform.localPosition = new Vector3(0f, 0f, 0.2f);

            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Spot;
            light.range = 15f;
            light.intensity = 0f;
            light.spotAngle = 25f;

            var fixture = fixtureRoot.AddComponent<ModularDmxFixture>();

            var color = fixtureRoot.AddComponent<ColorModule>();
            var dimmer = fixtureRoot.AddComponent<DimmerModule>();
            var panTilt = fixtureRoot.AddComponent<PanTiltModule>();
            var angle = fixtureRoot.AddComponent<AngleModule>();
            var strobe = fixtureRoot.AddComponent<StrobeModule>();
            var gobo = fixtureRoot.AddComponent<GoboModule>();

            var goboSlots = SampleCookieFactory.CreateSampleGobos();
            gobo.Configure(goboSlots.Length, true, 7, 180f);

            var lightApply = fixtureRoot.AddComponent<LightApplyModule>();
            lightApply.Configure(light, panAxis, tiltAxis, new Vector2(8f, 40f), new Vector2(-270f, 270f),
                new Vector2(-120f, 120f), 4f);

            var goboApply = fixtureRoot.AddComponent<GoboApplyModule>();
            goboApply.Configure(light, SampleCookieFactory.CreateOpenCookie(), goboSlots);

            fixture.SetModules(new[]
            {
                fixture.CreateEntry(color, 0),
                fixture.CreateEntry(dimmer, 3),
                fixture.CreateEntry(panTilt, 4),
                fixture.CreateEntry(angle, 8),
                fixture.CreateEntry(strobe, 9),
                fixture.CreateEntry(gobo, 10),
                fixture.CreateEntry(lightApply, 0),
                fixture.CreateEntry(goboApply, 0)
            });

            return fixture;
        }

        private static ModularDmxFixture CreateColorWheelFixture(Transform parent)
        {
            var fixtureRoot = new GameObject("ColorWheelFixture");
            fixtureRoot.transform.SetParent(parent, false);
            fixtureRoot.transform.localPosition = new Vector3(-3f, 1.5f, 0f);

            var light = fixtureRoot.AddComponent<Light>();
            light.type = LightType.Spot;
            light.range = 10f;
            light.spotAngle = 30f;
            light.intensity = 0f;

            var fixture = fixtureRoot.AddComponent<ModularDmxFixture>();

            var colorWheel = fixtureRoot.AddComponent<ColorWheelModule>();
            var dimmer = fixtureRoot.AddComponent<DimmerModule>();

            var lightApply = fixtureRoot.AddComponent<LightApplyModule>();
            lightApply.Configure(light, null, null, new Vector2(30f, 30f), Vector2.zero, Vector2.zero, 2f);

            fixture.SetModules(new[]
            {
                fixture.CreateEntry(colorWheel, 0),
                fixture.CreateEntry(dimmer, 1),
                fixture.CreateEntry(lightApply, 0)
            });

            return fixture;
        }

        private static ModularDmxFixture CreateDirectValueFixture(Transform parent)
        {
            var fixtureRoot = new GameObject("DirectValueFixture");
            fixtureRoot.transform.SetParent(parent, false);
            fixtureRoot.transform.localPosition = new Vector3(3f, 1f, 0f);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "ValueTarget";
            visual.transform.SetParent(fixtureRoot.transform, false);
            visual.transform.localScale = new Vector3(0.8f, 0.2f, 0.8f);

            var receiver = fixtureRoot.AddComponent<SampleFloatReceiver>();
            receiver.Configure(visual.transform);

            var fixture = fixtureRoot.AddComponent<ModularDmxFixture>();

            var directValue = fixtureRoot.AddComponent<DirectValueModule>();
            directValue.Configure(DirectValueModule.ReadMode.Byte, new Vector2(0f, 255f), new Vector2(0.2f, 2f), false);
            directValue.AddListener(receiver.SetHeight);

            fixture.SetModules(new[]
            {
                fixture.CreateEntry(directValue, 0)
            });

            return fixture;
        }

        private static void DestroyObject(GameObject target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
                return;
            }

            DestroyImmediate(target);
        }
    }

    internal static class SampleCookieFactory
    {
        private const int TextureSize = 128;
        private const int RotationFrames = 16;

        public static GoboSlot[] CreateSampleGobos()
        {
            return new[]
            {
                CreateSlot("Spokes", CreateSpokesPattern),
                CreateSlot("Bars", CreateBarsPattern),
                CreateSlot("Triangle", CreateTrianglePattern)
            };
        }

        public static Texture2D CreateOpenCookie()
        {
            return CreateTexture(0f, _ => 1f);
        }

        private static GoboSlot CreateSlot(string label, Func<Vector2, float> pattern)
        {
            var rotated = new Texture[RotationFrames];
            for (var i = 0; i < RotationFrames; i++)
            {
                var angle = i * 360f / RotationFrames;
                rotated[i] = CreateTexture(angle, pattern);
            }

            return new GoboSlot
            {
                label = label,
                staticCookie = rotated[0],
                rotatedCookies = rotated
            };
        }

        private static Texture2D CreateTexture(float angleDegrees, Func<Vector2, float> pattern)
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                name = $"SampleGobo_{angleDegrees:0}",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var radians = angleDegrees * Mathf.Deg2Rad;
            var cos = Mathf.Cos(radians);
            var sin = Mathf.Sin(radians);
            var pixels = new Color[TextureSize * TextureSize];

            for (var y = 0; y < TextureSize; y++)
            {
                for (var x = 0; x < TextureSize; x++)
                {
                    var uv = new Vector2(
                        (x + 0.5f) / TextureSize * 2f - 1f,
                        (y + 0.5f) / TextureSize * 2f - 1f);
                    var rotated = new Vector2(
                        uv.x * cos - uv.y * sin,
                        uv.x * sin + uv.y * cos);
                    var intensity = Mathf.Clamp01(pattern(rotated));
                    pixels[y * TextureSize + x] = new Color(intensity, intensity, intensity, 1f);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static float CreateSpokesPattern(Vector2 uv)
        {
            var radius = uv.magnitude;
            if (radius > 0.95f)
            {
                return 0f;
            }

            var angle = Mathf.Atan2(uv.y, uv.x);
            var spoke = Mathf.Abs(Mathf.Sin(angle * 4f));
            return spoke > 0.85f && radius > 0.2f ? 1f : 0f;
        }

        private static float CreateBarsPattern(Vector2 uv)
        {
            if (uv.magnitude > 0.95f)
            {
                return 0f;
            }

            var bars = Mathf.Abs(Mathf.Sin(uv.x * 18f));
            return bars > 0.8f ? 1f : 0f;
        }

        private static float CreateTrianglePattern(Vector2 uv)
        {
            if (uv.magnitude > 0.95f)
            {
                return 0f;
            }

            var p0 = new Vector2(0f, 0.7f);
            var p1 = new Vector2(-0.65f, -0.45f);
            var p2 = new Vector2(0.65f, -0.45f);

            return IsInsideTriangle(uv, p0, p1, p2) ? 1f : 0f;
        }

        private static bool IsInsideTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            var area = Sign(p, a, b);
            var area2 = Sign(p, b, c);
            var area3 = Sign(p, c, a);

            var hasNeg = area < 0f || area2 < 0f || area3 < 0f;
            var hasPos = area > 0f || area2 > 0f || area3 > 0f;
            return !(hasNeg && hasPos);
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }
    }
}
