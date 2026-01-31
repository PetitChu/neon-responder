using UnityEditor;
using UnityEngine;

namespace BrainlessLabs.Neon.Editor
{
    /// <summary>
    /// Custom editor for the CameraFollow component.
    /// This editor enhances the default inspector for CameraFollow
    /// by adding additional UI elements and functionality specific to this component.
    /// </summary>
    [CustomEditor(typeof(CameraFollow))]
    public class CameraFollowEditor : UnityEditor.Editor
    {
        private string _newLine = "\n\n"; //using double lines for better readability

        public override void OnInspectorGUI()
        {

            DrawDefaultInspector();

            GUILayout.Space(10);

            // button for more information about this component
            if (GUILayout.Button("Click here for more information about this component", GUILayout.Height(30)))
            {
                string title = "Camera Follow Component";
                string content =
                    "This component is designed to dynamically follow targets, usually the player, ensuring they remain centered or within a specified area on the screen. Additionally, the system offers an option to restrict players from moving outside the camera's view, creating boundaries that prevent off-screen navigation and keeping the player within the visual field at all times. Furthermore, the camera has adjustable controls over the visible area on the screen." +
                    _newLine;

                content += HighlightItem("Player Targets: ") + _newLine;
                content += HighlightItem("Targets: ") + "The target to follow. If there are multiple targets, the camera's follow point will be the center position of all these targets combined. At the start of the level, the camera will try to find all gameObjects tagged as 'Player' and assign them as the follow target." + _newLine;
                content += HighlightItem("Restrict Targets To Cam View: ") + "If the targets should be kept inside of the camera view area." + _newLine;
                content += HighlightItem("Border Margin: ") + "The border margin can be use to increase/decrease the restricted area." + _newLine + "\n";
                content += HighlightItem("Follow Settings: ") + _newLine;
                content += HighlightItem("Y Ofset: ") + "A variable to position the camera slightly higher (or lower) than it's exact follow position." + _newLine;
                content += HighlightItem("Damp Settings: ") + "\n";
                content += "Dampening refers to the smoothing of camera movement as it tracks a target. Instead of instantly snapping to the target's position, the camera moves gradually, creating a more fluid and natural motion." + _newLine;
                content += HighlightItem("Damp X: ") + "The smoothing of the camera in horizontal movement." + _newLine;
                content += HighlightItem("Damp Y: ") + "The smoothing of the camera in vertical movement." + _newLine;
                content += HighlightItem("View Area:") + "\n";
                content += "The View Area refers to the portion of the game world that is visible on the screen at any given time. Setting the boundaries of the player's visual field by setting the Left, Right, Top and Bottom area of the screen." + _newLine;
                content += HighlightItem("Backtracking: ") + "\n";
                content += "Backtracking refers to the player's ability to move backward within a level. In a sidescrolling Beat 'em up game, the camera typically moves from left to right. When allow backtracking is disabled, the camera is prevented from moving backward, requiring the player to continue moving forward toward the end of the level." + _newLine;
                content += HighlightItem("Allow Backtracking: ") + "Enable or disable the ability for the camera to move back." + _newLine;
                content += HighlightItem("Back Track Margin: ") + "A buffer zone that provides space for the camera to move slightly backwards." + _newLine;
                content += HighlightItem("Level Bounds: ");
                content += "Specific points in a game level where players must defeat a certain number of enemies before they can advance. These bounds are set by the Wave Manager during play mode." + _newLine;

                CustomWindow.ShowWindow(title, content, new Vector2(1024, 850));
            }
        }

        //shortcut to highlight items
        private string HighlightItem(string label, int size = 13)
        {
            return "<b><size=" + size + "><color=#FFFFFF>" + label + "</color></size></b>";
        }
    }
}