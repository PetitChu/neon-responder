using UnityEngine;
using UnityEditor;

namespace BrainlessLabs.Neon.Editor
{
    /// <summary>
    /// A custom property drawer for displaying fields marked with the <see cref="ReadOnlyProperty"/> attribute
    /// as read-only in the Unity Inspector.
    /// This prevents modifications to the field while still allowing the value to be displayed.
    /// </summary>
    [CustomPropertyDrawer(typeof(ReadOnlyProperty))]
    public class ReadOnlyAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
        {
            string valueStr;

            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    valueStr = prop.intValue.ToString();
                    break;

                case SerializedPropertyType.Boolean:
                    valueStr = prop.boolValue.ToString();
                    break;

                case SerializedPropertyType.Float:
                    valueStr = prop.floatValue.ToString("0.00");
                    break;

                case SerializedPropertyType.String:
                    valueStr = prop.stringValue;
                    break;

                default:
                    valueStr = "(not supported)";
                    break;
            }

            EditorGUI.LabelField(position, label.text, valueStr);
        }
    }
}
