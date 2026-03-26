using ArtNet.Devices.Modular;
using UnityEditor;
using UnityEngine;

namespace ArtNet.Editor
{
    [CustomEditor(typeof(DmxFixture))]
    public class ModularDmxFixtureInspector : UnityEditor.Editor
    {
        private bool _showState = true;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Channel Tools", EditorStyles.boldLabel);

                if (GUILayout.Button("Register Child Modules"))
                {
                    var fixture = (DmxFixture)target;
                    Undo.RecordObject(fixture, "Register DMX Modules");
                    fixture.RegisterChildModules();
                    EditorUtility.SetDirty(fixture);
                }

                if (GUILayout.Button("Auto Assign Channels"))
                {
                    var fixture = (DmxFixture)target;
                    Undo.RecordObject(fixture, "Auto Assign DMX Channels");
                    fixture.AutoAssignModuleChannels();
                    EditorUtility.SetDirty(fixture);
                }

                if (GUILayout.Button("Register Child Outputs"))
                {
                    var fixture = (DmxFixture)target;
                    Undo.RecordObject(fixture, "Register Fixture Outputs");
                    fixture.RegisterChildOutputs();
                    EditorUtility.SetDirty(fixture);
                }
            }

            DrawStateSection((DmxFixture)target);
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
