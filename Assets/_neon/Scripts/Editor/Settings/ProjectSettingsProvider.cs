using UnityEditor;
using UnityEngine;

namespace BrainlessLabs.Neon.Editor
{
    /// <summary>
    /// Provides a custom settings provider for managing project-specific settings in the Unity Editor.
    /// </summary>
    class ProjectSettingsProvider : SettingsProvider
    {
        internal const string ProjectSettingsPath = "Neon";
        
        class SettingsLabelWidthScope : System.IDisposable
        {
            private const float DefaultLabelWidth = 250.0f;
            private readonly float _previousLabelWidth;
            
            public SettingsLabelWidthScope()
            {
                _previousLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = DefaultLabelWidth;
            }
            
            public void Dispose()
            {
                EditorGUIUtility.labelWidth = _previousLabelWidth;
            }
        }
        
        [SettingsProvider]
        internal static SettingsProvider CreateSettingsProvider()
        {
            var provider = new ProjectSettingsProvider(
                ProjectSettingsPath,
                ProjectSettings.instance);
        
            return provider;
        }
        
        internal ProjectSettingsProvider(string path, IProjectSettings settings)
            : base(path, SettingsScope.Project)
        {
            _settings = settings;
        }
        
        private IProjectSettings _settings;
        private Vector2 _scrollPosition;
        
        
        public override void OnGUI(string searchContext)
        {
            var settings = _settings as ProjectSettings;
            if (settings == null)
                return;

            using (var scrollView = new GUILayout.ScrollViewScope(_scrollPosition, false, true))
            {
                _scrollPosition = scrollView.scrollPosition;
                using (new SettingsLabelWidthScope())
                {
                    EditorGUILayout.LabelField("Neon Settings", EditorStyles.boldLabel);
                    EditorGUILayout.Space();

                    DrawLineSeparator();
                    DrawBootstrapSettings();
                    
                    DrawLineSeparator();
                    DrawAudioSettings();

                    DrawLineSeparator();
                    DrawScenesSettings();
                }
            }
        }
        
        internal void DrawBootstrapSettings()
        {
            var bootstrapSettings = _settings.BootstrapSettings;

            using var checkScope = new EditorGUI.ChangeCheckScope();
            bootstrapSettings?.Editor_OnGUI(_settings.BootstrapSettingsAsset);
                
            if (checkScope.changed)
            {
                EditorUtility.SetDirty(_settings.BootstrapSettingsAsset);
            }
        }
        
        internal void DrawAudioSettings()
        {
            var audioSettings = _settings.AudioSettings;

            using var checkScope = new EditorGUI.ChangeCheckScope();
            audioSettings?.Editor_OnGUI(_settings.AudioSettingsAsset);

            if (checkScope.changed)
            {
                EditorUtility.SetDirty(_settings.AudioSettingsAsset);
            }
        }

        internal void DrawScenesSettings()
        {
            var scenesSettings = _settings.ScenesSettings;

            using var checkScope = new EditorGUI.ChangeCheckScope();
            scenesSettings?.Editor_OnGUI(_settings.ScenesSettingsAsset);

            if (checkScope.changed)
            {
                EditorUtility.SetDirty(_settings.ScenesSettingsAsset);
            }
        }
        
        private static readonly GUIStyle lineStyle = new ()
        {
            normal = { background = EditorGUIUtility.whiteTexture },
            margin = new RectOffset(0, 0, 4, 4),
            fixedHeight = 1
        };
        
        private static void DrawLineSeparator()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(GUIContent.none, lineStyle);
        }
    }
}
