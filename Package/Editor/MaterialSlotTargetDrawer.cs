using ArtNet.Devices.Modular.Output;
using UnityEditor;
using UnityEngine;

namespace ArtNet.Editor
{
    [CustomPropertyDrawer(typeof(MaterialSlotTarget))]
    public class MaterialSlotTargetDrawer : PropertyDrawer
    {
        private const float VerticalSpacing = 2f;

        private static class Styles
        {
            internal static readonly GUIContent ModeLabel = new("Material Slots", "Choose whether the property block applies to all material slots or only selected ones.");
            internal static readonly GUIContent IndicesLabel = new("Material Indices", "Zero-based material slot indices to update.");
            internal static readonly GUIContent HintLabel = new("This renderer will apply the property block to every material slot.");
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var modeProperty = property.FindPropertyRelative("mode");
            var indicesProperty = property.FindPropertyRelative("indices");

            EditorGUI.BeginProperty(position, label, property);

            var modeRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(modeRect, modeProperty, Styles.ModeLabel);

            var isSpecificIndices = (MaterialSlotTarget.TargetMode)modeProperty.enumValueIndex == MaterialSlotTarget.TargetMode.SpecificIndices;
            if (isSpecificIndices)
            {
                var indicesRect = new Rect(
                    position.x,
                    modeRect.yMax + VerticalSpacing,
                    position.width,
                    EditorGUI.GetPropertyHeight(indicesProperty, true));
                EditorGUI.PropertyField(indicesRect, indicesProperty, Styles.IndicesLabel, true);
            }
            else
            {
                var helpRect = new Rect(
                    position.x,
                    modeRect.yMax + VerticalSpacing,
                    position.width,
                    EditorGUIUtility.singleLineHeight * 2f);
                EditorGUI.HelpBox(helpRect, Styles.HintLabel.text, MessageType.Info);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var modeProperty = property.FindPropertyRelative("mode");
            var indicesProperty = property.FindPropertyRelative("indices");
            var height = EditorGUIUtility.singleLineHeight + VerticalSpacing;
            var isSpecificIndices = (MaterialSlotTarget.TargetMode)modeProperty.enumValueIndex == MaterialSlotTarget.TargetMode.SpecificIndices;

            if (isSpecificIndices)
            {
                height += EditorGUI.GetPropertyHeight(indicesProperty, true);
            }
            else
            {
                height += EditorGUIUtility.singleLineHeight * 2f;
            }

            return height;
        }
    }
}
