using System;
using System.IO;
using System.Text;
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

            var exportFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                ExportFolder.Replace('/', Path.DirectorySeparatorChar));

            var (fixtureCount, _, warnings) = LightingPlannerPackageExporter.ExportToFolder(exportFolder);
            if (fixtureCount == 0)
            {
                throw new InvalidOperationException("No DmxFixture found in the validation scene.");
            }

            var manifestJson = File.ReadAllText(Path.Combine(exportFolder, "manifest.json"));
            var rigJson      = File.ReadAllText(Path.Combine(exportFolder, "rig.json"));
            var intentJson   = File.ReadAllText(Path.Combine(exportFolder, "intent.json"));
            var timelineJson = File.ReadAllText(Path.Combine(exportFolder, "timeline.json"));

            var hasValidationFixtureType   = rigJson.Contains("Validation LED Wash RGB Strobe", StringComparison.Ordinal);
            var hasRearWashRole            = rigJson.Contains("BackLight", StringComparison.Ordinal);
            var hasFrontKeyRole            = rigJson.Contains("KeyLight", StringComparison.Ordinal);
            var hasExpectedChannels        = ContainsAll(rigJson, "dimmer", "color.red", "color.green", "color.blue", "strobe");
            var manifestPointsToRigIntentAndTimeline = ContainsAll(manifestJson, "\"rigFile\": \"rig.json\"", "\"intentFile\": \"intent.json\"", "\"timelineFile\": \"timeline.json\"");
            var intentLooksValid           = ContainsAll(intentJson, "\"schemaVersion\": \"1.0.0\"", "\"bpm\": 120.0", "\"sections\": []");
            var timelineTemplateValid      = ContainsAll(timelineJson, "\"schemaVersion\": \"1.0.0\"", "\"events\": []", "\"effects\": []");

            var report = new StringBuilder();
            report.AppendLine($"Scene: {ScenePath}");
            report.AppendLine($"ExportFolder: {exportFolder}");
            report.AppendLine($"Fixtures: {fixtureCount}");
            report.AppendLine($"Warnings: {warnings.Count}");
            report.AppendLine($"Has validation fixture type: {hasValidationFixtureType}");
            report.AppendLine($"Has Rear Wash role: {hasRearWashRole}");
            report.AppendLine($"Has Front Key role: {hasFrontKeyRole}");
            report.AppendLine($"Has dimmer/color/strobe channels: {hasExpectedChannels}");
            report.AppendLine($"Manifest points to rig/intent/timeline: {manifestPointsToRigIntentAndTimeline}");
            report.AppendLine($"Intent template looks valid: {intentLooksValid}");
            report.AppendLine($"Timeline template valid: {timelineTemplateValid}");

            File.WriteAllText(
                Path.Combine(exportFolder, "validation_report.txt"),
                report.ToString(),
                new UTF8Encoding(false));
            Debug.Log(report.ToString());
        }

        private static bool ContainsAll(string text, params string[] values)
        {
            foreach (var value in values)
            {
                if (!text.Contains(value, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
