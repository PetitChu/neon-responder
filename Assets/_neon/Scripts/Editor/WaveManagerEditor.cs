using UnityEditor;
using UnityEngine;

namespace BrainlessLabs.Neon.Editor
{
    /// <summary>
    /// Custom editor for the WaveManager class. This editor provides additional functionality
    /// and a user-friendly interface within the Unity Inspector for configuring and understanding
    /// the WaveManager component.
    /// </summary>
    [CustomEditor(typeof(WaveManager))]
    public class WaveManagerEditor : UnityEditor.Editor
    {
        private string _newLine = "\n\n"; //using double lines for better readability

        public override void OnInspectorGUI()
        {

            DrawDefaultInspector();

            GUILayout.Space(10);

            // button for more information about this component
            if (GUILayout.Button("Click here for more information about this component", GUILayout.Height(30)))
            {
                string title = "Wave Manager";
                string content = "The Wave Manager oversees level progression by managing the activation of enemy groups, directing the camera�s movement to the right, and determining when all enemies have been defeated, thus signaling the end of a level." + _newLine + "The Wave Manager data updates in real-time during play mode, allowing you to monitor the current state at all times." + _newLine;
                content += HighlightItem("Total Number Of Waves: ") + "Read only value that shows the total number of waves (during play mode)." + _newLine;
                content += HighlightItem("Current Wave: ") + "Read only value that shows the current wave (during play mode)." + _newLine;
                content += HighlightItem("Enemies Left In This Wave: ") + "Read only value that shows the amount of enemies left in this wave (during play mode)." + _newLine;
                content += HighlightItem("Total Enemies Left: ") + "Read only value that shows the total amount of enemies left in this level (during play mode)." + _newLine + "\n";
                content += HighlightItem("End Level When All Enemies Are Defeated: ") + "ends the level once all enemies are defeated. Disable this option if you prefer to end the level through other means." + _newLine;
                content += HighlightItem("Menu to open on Level Finish: ") + "The name of the menu to open when this level is completed." + _newLine;
                content += HighlightItem("Menu to open on all Levels Completed: ") + "The name of the menu to open when all levels have been completed." + _newLine;
                content += HighlightItem("Menu to open on Player Death: ") + "The name of the menu to open when the player is defeated." + _newLine + "\n";
                content += "For more information on setting up enemy waves in the level, please refer to the online documentation." + _newLine;

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