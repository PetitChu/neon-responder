using UnityEditor;
using UnityEngine;

namespace BrainlessLabs.Neon.Editor
{
    /// <summary>
    /// Custom editor for the HealthSystem component in Unity.
    /// Provides an enhanced inspector GUI with additional functionality for improving usability
    /// and customization of the HealthSystem component.
    /// </summary>
    [CustomEditor(typeof(HealthSystem))]
    public class HealthSystemEditor : UnityEditor.Editor
    {
        private string _newLine = "\n\n"; //using double lines for better readability

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);

            // button for more information about this component
            if (GUILayout.Button("Click here for more information about this component", GUILayout.Height(30)))
            {
                string title = "Health System";
                string content =
                    "The Health System is utilized by various units, including both Player and Enemy units, as well as objects like wooden crates and drum barrels. It tracks the maximum and current health of each unit or object." +
                    _newLine;

                content += HighlightItem("Settings Overview" + _newLine);
                content += HighlightItem("Max Hp: ") + "The maximum number of health points that this unit/object has." + _newLine;
                content += HighlightItem("Current Hp: ") + "The current number of health points that this unit/object has. The object/unit is destroyed when it reaches 0." + _newLine;
                content += HighlightItem("Invulnerable: ") + "A unit/object cannot be destroyed or receive damage when it is invulneable." + _newLine;
                content += HighlightItem("Show Small Health Bar: ") + "An option to show a small healthbar near this unit in the game." + _newLine;
                content += HighlightItem("Small Health Bar Offset: ") + "The position of the small healthbar." + _newLine;
                content += HighlightItem("Play SFX On Hit: ") + "The sound effect that is played when this object/unit is hit." + _newLine;
                content += HighlightItem("Play SFX On Destroy: ") + "The sound effect that is played when this object/unit is destroyed." + _newLine;
                content += HighlightItem("Show Hit Flash: ") + "If this unit/object should flash to a white color when it is hit." + _newLine;
                content += HighlightItem("Hit Flash Duration: ") + "The duration of the hit flash." + _newLine;
                content += HighlightItem("Show Shake Effect: ") + "If this object should do a shake effect." + _newLine;
                content += HighlightItem("Shake Intensity: ") + "The size of the shake effect." + _newLine;
                content += HighlightItem("Shake Duration: ") + "The duration (in seconds) of the shake effect." + _newLine;
                content += HighlightItem("Shake speed: ") + "The speed of he shake effect." + _newLine;
                content += HighlightItem("Show Effect On Hit: ") + "Effect shown when this object/unit is hit." + _newLine;
                content += HighlightItem("Show Effect On Destroy: ") + "Effect shown when this object/unit is destroyed" + _newLine;
                
                CustomWindow.ShowWindow(title, content, new Vector2(600, 750));
            }
        }

        //shortcut to highlight items
        private string HighlightItem(string label, int size = 13)
        {
            return "<b><size=" + size + "><color=#FFFFFF>" + label + "</color></size></b>";
        }
    }
}