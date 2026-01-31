using UnityEngine;
using UnityEditor;

namespace BrainlessLabs.Neon.Editor
{
    /// <summary>
    /// Custom property drawer for the <see cref="HelpAttribute"/> used to display a helpful message
    /// alongside properties in the Unity Inspector.
    /// </summary>
    [CustomPropertyDrawer(typeof(HelpAttribute))]
    public class HelpAttributeDrawer : DecoratorDrawer
    {
        private GUIStyle style;

        public override float GetHeight()
        {
            return 16f;
        }

        public override void OnGUI(Rect position)
        {
            //get Attribute
            var helpAttribute = attribute as HelpAttribute;
            if (helpAttribute == null) return;

            //set label style and color
            style = GUI.skin.GetStyle("WhiteMiniLabel");
            style.normal.textColor = Color.yellow;

            //set label field
            EditorGUI.LabelField(position, helpAttribute.text, style);
        }
    }
}