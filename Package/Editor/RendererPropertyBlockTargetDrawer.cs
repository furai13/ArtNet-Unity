using ArtNet.Devices.Modular.Output;
using UnityEditor;
using UnityEngine;

namespace ArtNet.Editor
{
    [CustomPropertyDrawer(typeof(RendererPropertyBlockTarget))]
    public class RendererPropertyBlockTargetDrawer : PropertyDrawer
    {
        private const float VerticalSpacing = 2f;

        private static class Styles
        {
            internal static readonly GUIContent RendererLabel = new("Renderer", "Renderer that receives the property block update.");
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var rendererProperty = property.FindPropertyRelative("renderer");
            var materialSlotsProperty = property.FindPropertyRelative("materialSlots");

            EditorGUI.BeginProperty(position, label, property);

            var headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, label, true);
            if (property.isExpanded)
            {
                var indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = indent + 1;

                var rendererRect = new Rect(
                    position.x,
                    headerRect.yMax + VerticalSpacing,
                    position.width,
                    EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(rendererRect, rendererProperty, Styles.RendererLabel);

                var slotsRect = new Rect(
                    position.x,
                    rendererRect.yMax + VerticalSpacing,
                    position.width,
                    EditorGUI.GetPropertyHeight(materialSlotsProperty, true));
                EditorGUI.PropertyField(slotsRect, materialSlotsProperty, GUIContent.none, true);

                EditorGUI.indentLevel = indent;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.isExpanded == false)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            var materialSlotsProperty = property.FindPropertyRelative("materialSlots");
            return EditorGUIUtility.singleLineHeight
                + VerticalSpacing
                + EditorGUIUtility.singleLineHeight
                + VerticalSpacing
                + EditorGUI.GetPropertyHeight(materialSlotsProperty, true);
        }
    }
}
