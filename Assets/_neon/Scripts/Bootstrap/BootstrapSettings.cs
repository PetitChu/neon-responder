using System;
using Eflatun.SceneReference;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Runtime bootstrap configuration.
    /// </summary>
    [Serializable]
    public class BootstrapSettings : IBootstrapSettings
    {
        [SerializeField]
        private bool _enableEditorBootstrap = default;
        
        [SerializeField]
        private SceneReference _bootstrapScene = default;

        [SerializeField]
        private SceneReference _postBootstrapScene = default;

        [SerializeField]
        private bool _executeBootstrapSequence = default;
        
        /// <inheritdoc/>
        public bool EnableEditorBootstrap
        {
            get => _enableEditorBootstrap;
            set => _enableEditorBootstrap = value;
        }

        /// <inheritdoc/>
        public SceneReference BootstrapScene
        {
            get => _bootstrapScene;
            set => _bootstrapScene = value;
        }

        /// <inheritdoc/>
        public SceneReference PostBootstrapScene
        {
            get => _postBootstrapScene;
            set => _postBootstrapScene = value;
        }
        
        /// <inheritdoc/>
        public bool ExecuteBootstrapSequence
        {
            get => _executeBootstrapSequence;
            set => _executeBootstrapSequence = value;
        }
        
        
#if UNITY_EDITOR
        private const string BootstrapSettingsHeader = "Bootstrap Settings";
        private const string BootstrapSettingsDescription = "Provides configuration settings for the bootstrap process within the application.";
        private const string EnableEditorBootstrapLabel = "Enable Editor Bootstrap";
        private const string EnableEditorBootstrapDescription = "When enabled, the specified bootstrap scene will be loaded into the EditorSceneManager.playModeStartScene as initial scene, and will then load the PostBootstrapScene after the bootstrap is complete.";
        private const string BootstrapSceneLabel = "Bootstrap Scene";
        private const string BootstrapSceneDescription = "Specifies the scene to use as the initial bootstrap scene when entering Play Mode in the Unity Editor. (Can be an empty scene)";
        private const string PostBootstrapSceneLabel = "Post-Bootstrap Scene";
        private const string PostBootstrapSceneDescription = "Specifies the scene to transition to once the initial bootstrap configuration and setup have been executed.";
        private const string ExecuteBootstrapSequenceLabel = "Execute Bootstrap Sequence";
        private const string ExecuteBootstrapSequenceDescription = "When enabled, the bootstrap sequence will be executed, initializing and configuring necessary services and components as defined in the bootstrap process.";
        
        private static readonly GUIContent bootstrapSettingsHeaderGUIContent = new (BootstrapSettingsHeader);
        private static readonly GUIContent bootstrapSettingsDescriptionGUIContent = new (BootstrapSettingsDescription);
        private static readonly GUIContent enableEditorBootstrapGUIContent = new (EnableEditorBootstrapLabel, EnableEditorBootstrapDescription);
        private static readonly GUIContent bootstrapSceneGUIContent = new (BootstrapSceneLabel, BootstrapSceneDescription);
        private static readonly GUIContent postBootstrapSceneGUIContent = new (PostBootstrapSceneLabel, PostBootstrapSceneDescription);
        private static readonly GUIContent executeBootstrapSequenceGUIContent = new (ExecuteBootstrapSequenceLabel, ExecuteBootstrapSequenceDescription);
        
        public void Editor_OnGUI(UnityEngine.Object target)
        {
            using (new UnityEditor.EditorGUILayout.VerticalScope())
            {
                UnityEditor.EditorGUILayout.LabelField(bootstrapSettingsHeaderGUIContent, UnityEditor.EditorStyles.boldLabel);
                UnityEditor.EditorGUILayout.LabelField(bootstrapSettingsDescriptionGUIContent, UnityEditor.EditorStyles.wordWrappedLabel);
                UnityEditor.EditorGUILayout.Space();

                using (new UnityEditor.EditorGUI.IndentLevelScope(1))
                {
                    EnableEditorBootstrap = UnityEditor.EditorGUILayout.Toggle(enableEditorBootstrapGUIContent, EnableEditorBootstrap);
                    if (EnableEditorBootstrap)
                    {
                        var serializedObject = new UnityEditor.SerializedObject(target);
                        serializedObject.UpdateIfRequiredOrScript();
                        
                        var bootstrapSceneSerializedProperty = serializedObject.FindProperty(nameof(BootstrapSettingsAsset._settings)).FindPropertyRelative(nameof(_bootstrapScene));
                        UnityEditor.EditorGUILayout.PropertyField(bootstrapSceneSerializedProperty, bootstrapSceneGUIContent);
                        
                        var postBootstrapSceneSerializedProperty = serializedObject.FindProperty(nameof(BootstrapSettingsAsset._settings)).FindPropertyRelative(nameof(_postBootstrapScene));
                        UnityEditor.EditorGUILayout.PropertyField(postBootstrapSceneSerializedProperty, postBootstrapSceneGUIContent);
                        
                        serializedObject.ApplyModifiedProperties();
                    }
                    
                    ExecuteBootstrapSequence = UnityEditor.EditorGUILayout.Toggle(executeBootstrapSequenceGUIContent, ExecuteBootstrapSequence);
                }
            }
        }
#endif
    }
}
