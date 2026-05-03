using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ArtNet.Devices.Modular;
using ArtNet.Devices.Semantics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArtNet.Editor
{
    internal static class LightingPlannerPackageExporter
    {
        private const string SchemaVersion = "1.0.0";

        [MenuItem("ArtNet/Lighting Planner/Export Show Package")]
        private static void ExportShowPackage()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var defaultFolderName = string.IsNullOrWhiteSpace(scene.name) ? "LightingPlannerPackage" : scene.name;
            var exportFolder = EditorUtility.SaveFolderPanel(
                "Export Lighting Planner Package",
                Application.dataPath,
                defaultFolderName);

            if (string.IsNullOrWhiteSpace(exportFolder))
            {
                return;
            }

            var (fixtureCount, fixtureTypeCount, warnings) = ExportToFolder(exportFolder);

            var message = new StringBuilder()
                .AppendLine($"Exported {fixtureCount} fixtures and {fixtureTypeCount} fixture types.")
                .AppendLine(exportFolder);

            if (warnings.Count > 0)
            {
                message.AppendLine();
                message.AppendLine("Warnings:");
                foreach (var warning in warnings)
                {
                    message.AppendLine($"- {warning}");
                }
            }

            EditorUtility.DisplayDialog("Lighting Planner Export", message.ToString(), "OK");
        }

        internal static (int fixtureCount, int fixtureTypeCount, List<string> warnings) ExportToFolder(
            string exportFolder)
        {
            var fixtures = DmxFixtureEditorUtility.FindFixturesInScene();
            if (fixtures.Length == 0)
            {
                return (0, 0, new List<string>());
            }

            var bindingByFixtureId = FixtureSemanticRegistry.FindBindings()
                .Where(binding => binding.Fixture != null)
                .GroupBy(binding => binding.Fixture.GetInstanceID())
                .ToDictionary(group => group.Key, group => group.First());

            var scene = EditorSceneManager.GetActiveScene();
            var warnings = new List<string>();
            var fixtureTypeEntries = BuildFixtureTypeEntries(fixtures, warnings);
            var rig = BuildRigExport(fixtures, fixtureTypeEntries, bindingByFixtureId);
            var manifest = BuildManifest(scene, rig);
            var intent = BuildIntentTemplate(scene, rig);

            Directory.CreateDirectory(exportFolder);
            WriteJson(Path.Combine(exportFolder, "manifest.json"), manifest);
            WriteJson(Path.Combine(exportFolder, "rig.json"), rig);
            WriteJson(Path.Combine(exportFolder, "intent.json"), intent);

            AssetDatabase.Refresh();

            return (rig.fixtures.Length, rig.fixtureTypes.Length, warnings);
        }

        private static ManifestJson BuildManifest(UnityEngine.SceneManagement.Scene scene, RigJson rig)
        {
            var packageId = string.IsNullOrWhiteSpace(scene.name) ? "artnet-show-package" : Slugify(scene.name);
            return new ManifestJson
            {
                schemaVersion = SchemaVersion,
                packageId = packageId,
                title = string.IsNullOrWhiteSpace(scene.name) ? "ArtNet Show Package" : scene.name,
                createdAt = DateTimeOffset.Now.ToString("O"),
                rigFile = "rig.json",
                intentFile = "intent.json",
                rigId = rig.rigId
            };
        }

        private static IntentJson BuildIntentTemplate(UnityEngine.SceneManagement.Scene scene, RigJson rig)
        {
            return new IntentJson
            {
                schemaVersion = SchemaVersion,
                rigId = rig.rigId,
                showId = string.IsNullOrWhiteSpace(scene.name) ? "show-001" : Slugify(scene.name),
                title = string.IsNullOrWhiteSpace(scene.name) ? "New Show" : scene.name,
                bpm = 120f,
                durationSeconds = 0f,
                globalIntent = new GlobalIntentJson
                {
                    styleKeywords = Array.Empty<string>(),
                    paletteHints = Array.Empty<string>(),
                    constraints = new ConstraintsJson
                    {
                        avoidAudienceHit = true,
                        preserveFaceVisibility = true,
                        limitStrobe = false,
                        maxBrightness = 1f,
                        maxMotionDensity = 1f
                    }
                },
                sections = Array.Empty<SectionJson>()
            };
        }

        private static RigJson BuildRigExport(
            IReadOnlyList<DmxFixture> fixtures,
            IReadOnlyDictionary<int, FixtureTypeEntry> fixtureTypeEntries,
            IReadOnlyDictionary<int, FixtureSemanticBinding> bindingByFixtureId)
        {
            var scene = EditorSceneManager.GetActiveScene();
            var rigId = string.IsNullOrWhiteSpace(scene.name) ? "artnet-rig" : Slugify(scene.name);
            var fixtureJsons = new List<FixtureJson>(fixtures.Count);
            foreach (var fixture in fixtures)
            {
                bindingByFixtureId.TryGetValue(fixture.GetInstanceID(), out var binding);
                var semanticDescriptor = binding != null ? binding.BuildDescriptor() : default;
                var hasSemantics = binding != null;
                var transform = fixture.transform;

                fixtureJsons.Add(new FixtureJson
                {
                    fixtureId = BuildFixtureId(transform),
                    fixtureTypeId = fixtureTypeEntries[fixture.GetInstanceID()].fixtureType.fixtureTypeId,
                    name = fixture.name,
                    patch = new PatchJson
                    {
                        universe = fixture.Universe,
                        startAddress = fixture.StartAddress
                    },
                    transform = new TransformJson
                    {
                        position = ToArray(transform.position),
                        rotationEuler = ToArray(transform.rotation.eulerAngles)
                    },
                    semantics = new SemanticsJson
                    {
                        primaryRole = hasSemantics ? semanticDescriptor.Roles.FirstOrDefault() ?? string.Empty : string.Empty,
                        secondaryRoles = hasSemantics ? semanticDescriptor.Roles.Skip(1).ToArray() : Array.Empty<string>(),
                        roles = hasSemantics ? semanticDescriptor.Roles : Array.Empty<string>(),
                        capabilities = hasSemantics ? semanticDescriptor.Capabilities : Array.Empty<string>(),
                        tags = hasSemantics ? semanticDescriptor.Tags : Array.Empty<string>(),
                        exclusionTags = hasSemantics ? semanticDescriptor.ExclusionTags : Array.Empty<string>(),
                        properties = hasSemantics
                            ? semanticDescriptor.Properties.Select(property => new PropertyJson
                            {
                                key = property.Key,
                                value = property.Value
                            }).ToArray()
                            : Array.Empty<PropertyJson>(),
                        priority = hasSemantics ? semanticDescriptor.Priority : 0.5f
                    }
                });
            }

            return new RigJson
            {
                schemaVersion = SchemaVersion,
                rigId = rigId,
                coordinateSystem = new CoordinateSystemJson
                {
                    space = "unity",
                    unit = "meter",
                    handedness = "left"
                },
                addressing = new AddressingJson
                {
                    universeBase = 0,
                    addressBase = 0,
                    maxChannelsPerUniverse = 512
                },
                fixtureTypes = fixtureTypeEntries.Values
                    .Select(entry => entry.fixtureType)
                    .Distinct(new FixtureTypeJsonComparer())
                    .OrderBy(type => type.fixtureTypeId, StringComparer.Ordinal)
                    .ToArray(),
                fixtures = fixtureJsons
                    .OrderBy(fixture => fixture.fixtureId, StringComparer.Ordinal)
                    .ToArray()
            };
        }

        private static Dictionary<int, FixtureTypeEntry> BuildFixtureTypeEntries(
            IReadOnlyList<DmxFixture> fixtures,
            List<string> warnings)
        {
            var prepared = fixtures
                .Select(fixture => CreatePreparedFixtureType(fixture))
                .ToArray();

            var entriesByFixtureId = new Dictionary<int, FixtureTypeEntry>(prepared.Length);
            var entriesBySignature = new Dictionary<string, FixtureTypeEntry>(StringComparer.Ordinal);
            var typeIdsInUse = new HashSet<string>(StringComparer.Ordinal);
            var typeNameToSignature = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var item in prepared)
            {
                if (entriesBySignature.TryGetValue(item.signature, out var existing))
                {
                    entriesByFixtureId[item.fixture.GetInstanceID()] = existing;
                    continue;
                }

                var typeId = item.baseTypeId;
                if (typeNameToSignature.TryGetValue(typeId, out var existingSignature) &&
                    !string.Equals(existingSignature, item.signature, StringComparison.Ordinal))
                {
                    warnings.Add(
                        $"Fixture type id '{typeId}' was used by multiple channel layouts. A hash suffix was added for '{item.fixture.name}'.");
                    typeId = $"{typeId}_{ShortHash(item.signature)}";
                }

                while (!typeIdsInUse.Add(typeId))
                {
                    typeId = $"{item.baseTypeId}_{ShortHash(item.signature)}";
                }

                typeNameToSignature[typeId] = item.signature;
                item.fixtureType.fixtureTypeId = typeId;

                var entry = new FixtureTypeEntry(item.fixtureType, item.signature);
                entriesBySignature[item.signature] = entry;
                entriesByFixtureId[item.fixture.GetInstanceID()] = entry;
            }

            return entriesByFixtureId;
        }

        private static PreparedFixtureType CreatePreparedFixtureType(DmxFixture fixture)
        {
            var patchedChannels = fixture.GetPatchedChannels()
                .OrderBy(channel => channel.AbsoluteChannel)
                .ToArray();

            var channels = patchedChannels.Select(channel => BuildChannelJson(fixture, channel)).ToArray();
            var signature = string.Join(
                "|",
                channels.Select(channel =>
                    $"{channel.offset}:{channel.functionId}:{channel.resolution}:{channel.pairKey}:{channel.label}"));

            var displayName = string.IsNullOrWhiteSpace(fixture.ProfileName) ? fixture.name : fixture.ProfileName;
            var baseTypeId = $"{Slugify(displayName)}_{Mathf.Max(fixture.ChannelFootprint, 1)}ch";

            return new PreparedFixtureType
            {
                fixture = fixture,
                signature = signature,
                baseTypeId = baseTypeId,
                fixtureType = new FixtureTypeJson
                {
                    fixtureTypeId = baseTypeId,
                    displayName = displayName,
                    channelFootprint = fixture.ChannelFootprint,
                    capabilities = InferFixtureTypeCapabilities(channels),
                    channels = channels
                }
            };
        }

        private static ChannelJson BuildChannelJson(DmxFixture fixture, DmxPatchedChannel channel)
        {
            var relativeOffset = channel.AbsoluteChannel - fixture.StartAddress;
            var moduleType = channel.Module != null ? channel.Module.GetType().Name : string.Empty;
            var descriptorName = channel.Descriptor.Name ?? string.Empty;

            return new ChannelJson
            {
                offset = relativeOffset,
                functionId = MapFunctionId(channel.Module, descriptorName),
                label = descriptorName,
                resolution = InferResolution(moduleType, descriptorName),
                pairKey = InferPairKey(descriptorName)
            };
        }

        private static string[] InferFixtureTypeCapabilities(IEnumerable<ChannelJson> channels)
        {
            var capabilities = new List<string>();
            foreach (var channel in channels)
            {
                switch (channel.functionId)
                {
                    case "dimmer":
                        AddDistinct(capabilities, "Dimmer");
                        break;
                    case "strobe":
                        AddDistinct(capabilities, "Strobe");
                        break;
                    case "pan":
                        AddDistinct(capabilities, "Pan");
                        break;
                    case "tilt":
                        AddDistinct(capabilities, "Tilt");
                        break;
                    case "beam.angle":
                        AddDistinct(capabilities, "BeamAngle");
                        break;
                    case "gobo.select":
                    case "gobo.rotate":
                        AddDistinct(capabilities, "Gobo");
                        break;
                    default:
                        if (channel.functionId.StartsWith("color.", StringComparison.Ordinal))
                        {
                            AddDistinct(capabilities, "Color");
                        }
                        break;
                }
            }

            return capabilities.ToArray();
        }

        private static string MapFunctionId(DmxModuleBase module, string descriptorName)
        {
            var normalizedName = NormalizeToken(descriptorName);
            switch (module)
            {
                case DimmerModule:
                    return "dimmer";
                case AngleModule:
                    return "beam.angle";
                case StrobeModule:
                    return "strobe";
                case ColorModule:
                    return MapColorFunction(normalizedName);
                case ColorWheelModule:
                    return "color.wheel";
                case PanTiltModule:
                    if (normalizedName.StartsWith("pan", StringComparison.Ordinal))
                    {
                        return "pan";
                    }

                    if (normalizedName.StartsWith("tilt", StringComparison.Ordinal))
                    {
                        return "tilt";
                    }

                    break;
                case GoboModule:
                    if (normalizedName.Contains("rotation", StringComparison.Ordinal) ||
                        normalizedName.Contains("rotate", StringComparison.Ordinal))
                    {
                        return "gobo.rotate";
                    }

                    return "gobo.select";
                case FunctionChannelModule:
                    return "control";
                case DirectValueModule:
                    return "value";
            }

            if (normalizedName.StartsWith("red", StringComparison.Ordinal) ||
                normalizedName.StartsWith("green", StringComparison.Ordinal) ||
                normalizedName.StartsWith("blue", StringComparison.Ordinal) ||
                normalizedName.StartsWith("white", StringComparison.Ordinal) ||
                normalizedName.StartsWith("amber", StringComparison.Ordinal) ||
                normalizedName.StartsWith("uv", StringComparison.Ordinal))
            {
                return MapColorFunction(normalizedName);
            }

            return $"unknown.{normalizedName}";
        }

        private static string MapColorFunction(string normalizedName)
        {
            if (normalizedName.StartsWith("red", StringComparison.Ordinal))
            {
                return "color.red";
            }

            if (normalizedName.StartsWith("green", StringComparison.Ordinal))
            {
                return "color.green";
            }

            if (normalizedName.StartsWith("blue", StringComparison.Ordinal))
            {
                return "color.blue";
            }

            if (normalizedName.StartsWith("white", StringComparison.Ordinal))
            {
                return "color.white";
            }

            if (normalizedName.StartsWith("amber", StringComparison.Ordinal))
            {
                return "color.amber";
            }

            if (normalizedName.StartsWith("uv", StringComparison.Ordinal))
            {
                return "color.uv";
            }

            return "color";
        }

        private static string InferResolution(string moduleType, string descriptorName)
        {
            var normalizedName = NormalizeToken(descriptorName);
            if (normalizedName.Contains("fine", StringComparison.Ordinal))
            {
                return "16bit_fine";
            }

            if (string.Equals(moduleType, nameof(PanTiltModule), StringComparison.Ordinal) ||
                string.Equals(moduleType, nameof(DirectValueModule), StringComparison.Ordinal))
            {
                if (normalizedName.StartsWith("pan", StringComparison.Ordinal) ||
                    normalizedName.StartsWith("tilt", StringComparison.Ordinal) ||
                    normalizedName.StartsWith("value", StringComparison.Ordinal))
                {
                    return "16bit_coarse";
                }
            }

            return "8bit";
        }

        private static string InferPairKey(string descriptorName)
        {
            var normalizedName = NormalizeToken(descriptorName);
            if (normalizedName.StartsWith("pan", StringComparison.Ordinal))
            {
                return "pan";
            }

            if (normalizedName.StartsWith("tilt", StringComparison.Ordinal))
            {
                return "tilt";
            }

            if (normalizedName.StartsWith("value", StringComparison.Ordinal))
            {
                return "value";
            }

            return string.Empty;
        }

        private static string BuildFixtureId(Transform transform)
        {
            var names = new Stack<string>();
            var current = transform;
            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }

            return Slugify(string.Join("_", names));
        }

        private static float[] ToArray(Vector3 vector)
        {
            return new[] { vector.x, vector.y, vector.z };
        }

        private static void WriteJson<T>(string path, T data)
        {
            File.WriteAllText(path, JsonUtility.ToJson(data, true), new UTF8Encoding(false));
        }

        private static string Slugify(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "unnamed";
            }

            var builder = new StringBuilder(value.Length);
            var lastWasSeparator = false;
            foreach (var character in value)
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToLowerInvariant(character));
                    lastWasSeparator = false;
                    continue;
                }

                if (lastWasSeparator)
                {
                    continue;
                }

                builder.Append('_');
                lastWasSeparator = true;
            }

            return builder.ToString().Trim('_');
        }

        private static string NormalizeToken(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "unknown"
                : new string(value
                    .Trim()
                    .ToLowerInvariant()
                    .Where(character => char.IsLetterOrDigit(character) || character == '.')
                    .ToArray());
        }

        private static string ShortHash(string value)
        {
            unchecked
            {
                uint hash = 17;
                foreach (var character in value)
                {
                    hash = (hash * 31) + character;
                }

                return hash.ToString("x8");
            }
        }

        private static void AddDistinct(List<string> values, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (!values.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                values.Add(value);
            }
        }

        private readonly struct FixtureTypeEntry
        {
            public FixtureTypeEntry(FixtureTypeJson fixtureType, string signature)
            {
                this.fixtureType = fixtureType;
                this.signature = signature;
            }

            public readonly FixtureTypeJson fixtureType;
            public readonly string signature;
        }

        private sealed class FixtureTypeJsonComparer : IEqualityComparer<FixtureTypeJson>
        {
            public bool Equals(FixtureTypeJson x, FixtureTypeJson y)
            {
                return string.Equals(x?.fixtureTypeId, y?.fixtureTypeId, StringComparison.Ordinal);
            }

            public int GetHashCode(FixtureTypeJson obj)
            {
                return obj?.fixtureTypeId != null ? StringComparer.Ordinal.GetHashCode(obj.fixtureTypeId) : 0;
            }
        }

        private sealed class PreparedFixtureType
        {
            public DmxFixture fixture;
            public string signature;
            public string baseTypeId;
            public FixtureTypeJson fixtureType;
        }

        [Serializable]
        private sealed class ManifestJson
        {
            public string schemaVersion;
            public string packageId;
            public string title;
            public string createdAt;
            public string rigFile;
            public string intentFile;
            public string rigId;
        }

        [Serializable]
        private sealed class RigJson
        {
            public string schemaVersion;
            public string rigId;
            public CoordinateSystemJson coordinateSystem;
            public AddressingJson addressing;
            public FixtureTypeJson[] fixtureTypes;
            public FixtureJson[] fixtures;
        }

        [Serializable]
        private sealed class CoordinateSystemJson
        {
            public string space;
            public string unit;
            public string handedness;
        }

        [Serializable]
        private sealed class AddressingJson
        {
            public int universeBase;
            public int addressBase;
            public int maxChannelsPerUniverse;
        }

        [Serializable]
        private sealed class FixtureTypeJson
        {
            public string fixtureTypeId;
            public string displayName;
            public int channelFootprint;
            public string[] capabilities;
            public ChannelJson[] channels;
        }

        [Serializable]
        private sealed class ChannelJson
        {
            public int offset;
            public string functionId;
            public string label;
            public string resolution;
            public string pairKey;
        }

        [Serializable]
        private sealed class FixtureJson
        {
            public string fixtureId;
            public string fixtureTypeId;
            public string name;
            public PatchJson patch;
            public TransformJson transform;
            public SemanticsJson semantics;
        }

        [Serializable]
        private sealed class PatchJson
        {
            public int universe;
            public int startAddress;
        }

        [Serializable]
        private sealed class TransformJson
        {
            public float[] position;
            public float[] rotationEuler;
        }

        [Serializable]
        private sealed class SemanticsJson
        {
            public string primaryRole;
            public string[] secondaryRoles;
            public string[] roles;
            public string[] capabilities;
            public string[] tags;
            public string[] exclusionTags;
            public PropertyJson[] properties;
            public float priority;
        }

        [Serializable]
        private sealed class PropertyJson
        {
            public string key;
            public string value;
        }

        [Serializable]
        private sealed class IntentJson
        {
            public string schemaVersion;
            public string rigId;
            public string showId;
            public string title;
            public float bpm;
            public float durationSeconds;
            public GlobalIntentJson globalIntent;
            public SectionJson[] sections;
        }

        [Serializable]
        private sealed class GlobalIntentJson
        {
            public string[] styleKeywords;
            public string[] paletteHints;
            public ConstraintsJson constraints;
        }

        [Serializable]
        private sealed class ConstraintsJson
        {
            public bool avoidAudienceHit;
            public bool preserveFaceVisibility;
            public bool limitStrobe;
            public float maxBrightness;
            public float maxMotionDensity;
        }

        [Serializable]
        private sealed class SectionJson
        {
            public string sectionId;
            public string label;
            public float startTime;
            public float endTime;
            public float energy;
            public float density;
            public string[] keywords;
        }
    }
}
