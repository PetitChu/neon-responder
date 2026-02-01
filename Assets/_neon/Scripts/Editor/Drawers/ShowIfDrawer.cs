using System;
using UnityEditor;
using UnityEngine;

namespace BrainlessLabs.Neon.Editor
{
    /// <summary>
    /// Property drawer for <see cref="ShowIfAttribute"/>.
    /// Hides the field when the condition is not met, collapsing the space entirely.
    /// </summary>
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public class ShowIfDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (ShouldShow(property))
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            return 0f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (ShouldShow(property))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        private bool ShouldShow(SerializedProperty property)
        {
            var attr = (ShowIfAttribute)attribute;

            // Build the path to the condition field (sibling property).
            // Handles nested properties in serializable classes and arrays.
            string propertyPath = property.propertyPath;
            int lastDot = propertyPath.LastIndexOf('.');
            string conditionPath = lastDot >= 0
                ? propertyPath.Substring(0, lastDot + 1) + attr.ConditionField
                : attr.ConditionField;

            var conditionProperty = property.serializedObject.FindProperty(conditionPath);
            if (conditionProperty == null)
            {
                Debug.LogWarning(
                    $"[ShowIf] Could not find condition field '{attr.ConditionField}' " +
                    $"(resolved path: '{conditionPath}') for property '{property.propertyPath}'. Showing by default.");
                return true;
            }

            foreach (var compareValue in attr.CompareValues)
            {
                if (ComparePropertyValue(conditionProperty, compareValue))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ComparePropertyValue(SerializedProperty property, object compareValue)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    return property.boolValue == Convert.ToBoolean(compareValue);

                case SerializedPropertyType.Enum:
                    return property.intValue == Convert.ToInt32(compareValue);

                case SerializedPropertyType.Integer:
                    return property.intValue == Convert.ToInt32(compareValue);

                case SerializedPropertyType.Float:
                    return Mathf.Approximately(property.floatValue, Convert.ToSingle(compareValue));

                case SerializedPropertyType.String:
                    return property.stringValue == compareValue?.ToString();

                default:
                    return true;
            }
        }
    }
}
