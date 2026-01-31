using UnityEditor;
using UnityEngine;

namespace BrainlessLabs.Neon.Editor
{
    /// <summary>
    /// Custom editor for the <c>AudioController</c> class. This editor script provides an enhanced
    /// interface in the Unity Inspector for managing and understanding the <c>AudioController</c> component.
    /// </summary>
    [CustomEditor(typeof(AudioController))]
    public class AudioControllerEditor : UnityEditor.Editor
    {
        private string _newLine = "\n\n"; //using double lines for better readability

        public override void OnInspectorGUI()
        {

            DrawDefaultInspector();

            GUILayout.Space(10);

            //button for more information about this component
            if (GUILayout.Button("Click here for more information about this component", GUILayout.Height(30)))
            {
                string title = "Audio Controller";
                string content =
                    "Each scene contains an Audio Controller, which has a list of AudioClips that can be played on audio events. These audio events can be triggered through animation or through code." +
                    _newLine;
                content += HighlightItem("Audio List - Item Settings") + "" + _newLine;
                content += HighlightItem("Name: ") + "The name of this audioclip, and reference for playing this sound effect." + _newLine;
                content += HighlightItem("Volume: ") + "The loudness of this sound effect, ranging from 0.0 to 1.0." + _newLine;
                content += HighlightItem("Random Volume: ") + "Use this setting if you want variation in volume, each time when this sound if played." + _newLine;
                content += HighlightItem("Random Pitch: ") + "This setting is used to vary the pitch of a sound effect each time it is played, introducing a degree of randomness." + _newLine;
                content += HighlightItem("Min Time Between Call: ") + "Controls the minimum amount of time that must elapse between consecutive plays of this sound effect." + _newLine;
                content += HighlightItem("Range: ") + "Optional setting to adjust the range/distance of this SFX. Set it to 0 to disable range, ensuring the SFX is always audible." + _newLine;
                content += HighlightItem("Loop: ") + "Whether a sound effect is played continuously in a repeating cycle or just once." + _newLine;
                content += HighlightItem("Clip: ") + "Reference to the audio file for this clip." + _newLine + "\n";
                content += "TIP: If you add multiple clips to an audio item, one will be chosen at random. By pairing this with pitch randomization, you can get more variation." + _newLine;
                CustomWindow.ShowWindow(title, content, new Vector2(700, 650));
            }
        }

        //shortcut to highlight items
        private string HighlightItem(string label, int size = 13)
        {
            return "<b><size=" + size + "><color=#FFFFFF>" + label + "</color></size></b>";
        }
    }
}