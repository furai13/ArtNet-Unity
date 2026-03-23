using ArtNet.Devices.Modular;
using UnityEditor;
using UnityEngine;

namespace ArtNet.Editor
{
    [CustomEditor(typeof(ModularDmxFixture))]
    public class ModularDmxFixtureInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Channel Tools", EditorStyles.boldLabel);

                if (GUILayout.Button("Register Child Modules"))
                {
                    var fixture = (ModularDmxFixture)target;
                    Undo.RecordObject(fixture, "Register DMX Modules");
                    fixture.RegisterChildModules();
                    EditorUtility.SetDirty(fixture);
                }

                if (GUILayout.Button("Auto Assign Channels"))
                {
                    var fixture = (ModularDmxFixture)target;
                    Undo.RecordObject(fixture, "Auto Assign DMX Channels");
                    fixture.AutoAssignModuleChannels();
                    EditorUtility.SetDirty(fixture);
                }
            }
        }
    }
}
