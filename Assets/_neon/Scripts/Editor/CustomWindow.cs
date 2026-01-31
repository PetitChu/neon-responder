using UnityEngine;
using UnityEditor;

namespace BrainlessLabs.Neon.Editor
{
    /// <summary>
    /// Represents a custom editor window in Unity that can display content with a specified title, size, and position.
    /// </summary>
    /// <seealso cref="EditorWindow" />
    public class CustomWindow : EditorWindow
    {
        private string _windowTitle;
        private string _content;
        private string _url = "https://www.osarion.com/BeatEmUpTemplate2D/documentation.html";
        private int _padding = 25;

        public static void ShowWindow(string title, string content, Vector2 size)
        {
            CustomWindow window = GetWindow<CustomWindow>(title);

            window._windowTitle = title;
            window._content = content;
            window.Repaint();

            //set window size
            window.minSize = size;
            window.maxSize = new Vector2(1024, 1024);

            //put window in screen center position
            Vector2 screenCenter = new Vector2(Screen.currentResolution.width / 2, Screen.currentResolution.height / 2);
            Vector2 windowSize = size;
            Vector2 windowPosition = screenCenter - (windowSize / 2);
            window.position = new Rect(windowPosition.x, windowPosition.y, windowSize.x, windowSize.y);

            //set the window's position and size
            window.position = new Rect(windowPosition.x, windowPosition.y, windowSize.x, windowSize.y);
        }

        private void OnGUI()
        {

            //title
            ShowTitle(_windowTitle);

            //show content if it exists
            if (!string.IsNullOrEmpty(_content))
            {
                EditorGUILayout.TextArea(_content, LabelStyle(), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            }

            //show footeLink to documentation
            EditorGUILayout.Space(10);
            ShowTitle("Documentation");
            GUILayout.Label("For detailed documentation, FAQ, tutorials, and videos, please visit the website:", LabelStyle());

            //button to website
            if (GUILayout.Button(new GUIContent("Online Documentation", "Open link"), ButtonStyle()))
            {
                Application.OpenURL(_url);
            }
        }

        //style
        private GUIStyle ButtonStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);
            style.normal.textColor = new Color(1, 1, 1, .6f);
            style.hover.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;
            style.richText = true;
            style.margin = new RectOffset(_padding, _padding, 0, 10);
            style.fixedHeight = 40;
            return style;
        }

        //style for labels
        private GUIStyle LabelStyle(bool bold = false)
        {
            GUIStyle style = bold ? new GUIStyle(EditorStyles.boldLabel) : new GUIStyle(EditorStyles.label);
            style.wordWrap = true;
            style.richText = true;
            style.padding = new RectOffset(_padding, _padding, 0, 0);
            style.alignment = TextAnchor.UpperLeft;
            return style;
        }

        //title void
        private void ShowTitle(string label)
        {
            string richText = $"<b><size=14><color=#FFFFFF>{label}</color></size></b>";
            GUILayout.Label(richText, TitleStyle());
        }

        //style for titles
        private GUIStyle TitleStyle()
        {
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
            style.wordWrap = true;
            style.richText = true;
            style.padding = new RectOffset(_padding, _padding, _padding, 0);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = 14;
            style.richText = true;
            return style;
        }
    }
}