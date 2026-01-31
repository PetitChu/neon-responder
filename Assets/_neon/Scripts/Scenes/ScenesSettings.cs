using System;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Runtime configuration for the scenes service.
    /// </summary>
    [Serializable]
    public class ScenesSettings : IScenesSettings
    {
        [SerializeField]
        private SceneDefinitionAsset[] _sceneDefinitions = Array.Empty<SceneDefinitionAsset>();

        /// <inheritdoc/>
        public SceneDefinitionAsset[] SceneDefinitions
        {
            get => _sceneDefinitions;
            set => _sceneDefinitions = value;
        }

#if UNITY_EDITOR
        private const string ScenesSettingsHeader = "Scenes Settings";
        private const string ScenesSettingsDescription = "Configuration for the scenes service, including the available scene definitions.";
        private const string SceneDefinitionsLabel = "Scene Definitions";
        private const string SceneDefinitionsDescription = "The collection of scene definition assets available to the scenes service.";

        private static readonly GUIContent scenesSettingsHeaderGUIContent = new(ScenesSettingsHeader);
        private static readonly GUIContent scenesSettingsDescriptionGUIContent = new(ScenesSettingsDescription);
        private static readonly GUIContent sceneDefinitionsGUIContent = new(SceneDefinitionsLabel, SceneDefinitionsDescription);

        public void Editor_OnGUI(UnityEngine.Object target)
        {
            using (new UnityEditor.EditorGUILayout.VerticalScope())
            {
                UnityEditor.EditorGUILayout.LabelField(scenesSettingsHeaderGUIContent, UnityEditor.EditorStyles.boldLabel);
                UnityEditor.EditorGUILayout.LabelField(scenesSettingsDescriptionGUIContent, UnityEditor.EditorStyles.wordWrappedLabel);
                UnityEditor.EditorGUILayout.Space();

                using (new UnityEditor.EditorGUI.IndentLevelScope(1))
                {
                    var serializedObject = new UnityEditor.SerializedObject(target);
                    serializedObject.UpdateIfRequiredOrScript();

                    var sceneDefinitionsProperty = serializedObject.FindProperty(nameof(ScenesSettingsAsset._settings))
                        .FindPropertyRelative(nameof(_sceneDefinitions));
                    UnityEditor.EditorGUILayout.PropertyField(sceneDefinitionsProperty, sceneDefinitionsGUIContent, true);

                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
#endif
    }
}
