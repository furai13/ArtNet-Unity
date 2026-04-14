using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ArtNet.Devices.Modular;
using ArtNet.Devices.Semantics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArtNet.Editor
{
    internal static class LightingPlannerExportValidationBatch
    {
        private const string ScenePath =
            "Assets/Samples/ArtNet-Unity/0.2.0/Receive Art-Net packet/Scenes/LightingPlannerExportValidation.unity";

        private const string ExportFolder =
            "../Package/Documentation~/ValidationSamples/lighting_planner_export_validation";

        [MenuItem("ArtNet/Lighting Planner/Export Validation Scene (Fixed Folder)")]
        public static void ExportValidationScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Failed to open scene: {ScenePath}");
            }

            var exportFolder = Path.Combine(Directory.GetCurrentDirectory(), ExportFolder.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(exportFolder);

            var fixtures = DmxFixtureEditorUtility.FindFixturesInScene();
            if (fixtures.Length == 0)
            {
                throw new InvalidOperationException("No DmxFixture found in the validation scene.");
            }

            var bindingByFixtureId = FixtureSemanticRegistry.FindBindings()
                .Where(binding => binding.Fixture != null)
                .GroupBy(binding => binding.Fixture.GetInstanceID())
                .ToDictionary(group => group.Key, group => group.First());

            var exporterType = typeof(LightingPlannerPackageExporter);
            var buildFixtureTypeEntries = exporterType.GetMethod("BuildFixtureTypeEntries", BindingFlags.NonPublic | BindingFlags.Static);
            var buildRigExport = exporterType.GetMethod("BuildRigExport", BindingFlags.NonPublic | BindingFlags.Static);
            var buildManifest = exporterType.GetMethod("BuildManifest", BindingFlags.NonPublic | BindingFlags.Static);
            var buildIntentTemplate = exporterType.GetMethod("BuildIntentTemplate", BindingFlags.NonPublic | BindingFlags.Static);

            if (buildFixtureTypeEntries == null || buildRigExport == null || buildManifest == null || buildIntentTemplate == null)
            {
                throw new MissingMethodException("LightingPlannerPackageExporter private methods could not be resolved.");
            }

            var warnings = new List<string>();
            var fixtureTypeEntries = buildFixtureTypeEntries.Invoke(null, new object[] { fixtures, warnings });
            var rig = buildRigExport.Invoke(null, new object[] { fixtures, fixtureTypeEntries, bindingByFixtureId });
            var manifest = buildManifest.Invoke(null, new object[] { scene, rig });
            var intent = buildIntentTemplate.Invoke(null, new object[] { scene, rig });

            WriteJson(Path.Combine(exportFolder, "manifest.json"), manifest);
            WriteJson(Path.Combine(exportFolder, "rig.json"), rig);
            WriteJson(Path.Combine(exportFolder, "intent.json"), intent);
            AssetDatabase.Refresh();

            var manifestJson = File.ReadAllText(Path.Combine(exportFolder, "manifest.json"));
            var rigJson = File.ReadAllText(Path.Combine(exportFolder, "rig.json"));
            var intentJson = File.ReadAllText(Path.Combine(exportFolder, "intent.json"));

            var hasValidationFixtureType = rigJson.Contains("Validation LED Wash RGB Strobe", StringComparison.Ordinal);
            var hasRearWashRole = rigJson.Contains("BackLight", StringComparison.Ordinal);
            var hasFrontKeyRole = rigJson.Contains("KeyLight", StringComparison.Ordinal);
            var hasExpectedChannels = ContainsAll(rigJson, "dimmer", "color.red", "color.green", "color.blue", "strobe");
            var manifestPointsToRigAndIntent = ContainsAll(manifestJson, "\"rigFile\": \"rig.json\"", "\"intentFile\": \"intent.json\"");
            var intentLooksValid = ContainsAll(intentJson, "\"schemaVersion\": \"1.0.0\"", "\"bpm\": 120.0", "\"sections\": []");

            var report = new StringBuilder();
            report.AppendLine($"Scene: {ScenePath}");
            report.AppendLine($"ExportFolder: {exportFolder}");
            report.AppendLine($"Fixtures: {fixtures.Length}");
            report.AppendLine($"Warnings: {warnings.Count}");
            report.AppendLine($"Has validation fixture type: {hasValidationFixtureType}");
            report.AppendLine($"Has Rear Wash role: {hasRearWashRole}");
            report.AppendLine($"Has Front Key role: {hasFrontKeyRole}");
            report.AppendLine($"Has dimmer/color/strobe channels: {hasExpectedChannels}");
            report.AppendLine($"Manifest points to rig/intent: {manifestPointsToRigAndIntent}");
            report.AppendLine($"Intent template looks valid: {intentLooksValid}");

            File.WriteAllText(Path.Combine(exportFolder, "validation_report.txt"), report.ToString(), new UTF8Encoding(false));
            Debug.Log(report.ToString());
        }

        private static bool ContainsAll(string text, params string[] values)
        {
            return values.All(value => text.Contains(value, StringComparison.Ordinal));
        }

        private static void WriteJson(string path, object data)
        {
            File.WriteAllText(path, JsonUtility.ToJson(data, true), new UTF8Encoding(false));
        }
    }
}
