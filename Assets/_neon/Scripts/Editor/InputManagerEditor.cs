using UnityEditor;
using UnityEngine;

namespace BrainlessLabs.Neon.Editor
{
    /// <summary>
    /// A custom editor for the InputManager component, providing additional functionality
    /// within the Unity Inspector, such as interactive UI buttons and enhanced descriptions
    /// tailored for developers.
    /// </summary>
    [CustomEditor(typeof(InputManager))]
    public class InputManagerEditor : UnityEditor.Editor
    {
        private string _newLine = "\n\n"; //using double lines for better readability

        public override void OnInspectorGUI()
        {

            DrawDefaultInspector();

            GUILayout.Space(10);

            //button for more information about this component
            if (GUILayout.Button("Click here for more information about this component", GUILayout.Height(30)))
            {
                string title = "Input Manager";
                string content =
                    "The Input Manager handles both keyboard and joystick inputs. It also provides options for customizing key and button mappings." +
                    _newLine;

                content += HighlightItem("How to change controls \n");
                content += "Go to Osarion/BeatEmUpTemplate2D/Scripts/Input/PlayerControls." + _newLine;
                content +=
                    "In the Inspector window, you'll see a button named 'Edit Asset', click on it to open a window where you can set up the button mappings for the entire project.\n";

                CustomWindow.ShowWindow(title, content, new Vector2(700, 500));
            }
        }

        //shortcut to highlight items
        private string HighlightItem(string label, int size = 13)
        {
            return "<b><size=" + size + "><color=#FFFFFF>" + label + "</color></size></b>";
        }
    }
}