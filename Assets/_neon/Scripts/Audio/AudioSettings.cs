using System;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    [Serializable]
    public class AudioSettings : IAudioSettings
    {
        [SerializeField]
        private AudioConfigurationAsset _sfxConfiguration;

        [SerializeField]
        private AudioConfigurationAsset _musicConfiguration;

        public AudioConfigurationAsset SfxConfiguration
        {
            get => _sfxConfiguration;
            set => _sfxConfiguration = value;
        }

        public AudioConfigurationAsset MusicConfiguration
        {
            get => _musicConfiguration;
            set => _musicConfiguration = value;
        }

#if UNITY_EDITOR
        private const string AudioSettingsHeader = "Audio Settings";
        private const string AudioSettingsDescription = "Configuration for the audio service, including SFX and music audio configurations.";
        private const string SfxConfigurationLabel = "SFX Configuration";
        private const string SfxConfigurationDescription = "The audio configuration asset containing all sound effect items and their mixer group.";
        private const string MusicConfigurationLabel = "Music Configuration";
        private const string MusicConfigurationDescription = "The audio configuration asset containing all music items and their mixer group.";

        private static readonly GUIContent audioSettingsHeaderGUIContent = new(AudioSettingsHeader);
        private static readonly GUIContent audioSettingsDescriptionGUIContent = new(AudioSettingsDescription);
        private static readonly GUIContent sfxConfigurationGUIContent = new(SfxConfigurationLabel, SfxConfigurationDescription);
        private static readonly GUIContent musicConfigurationGUIContent = new(MusicConfigurationLabel, MusicConfigurationDescription);

        public void Editor_OnGUI(UnityEngine.Object target)
        {
            using (new UnityEditor.EditorGUILayout.VerticalScope())
            {
                UnityEditor.EditorGUILayout.LabelField(audioSettingsHeaderGUIContent, UnityEditor.EditorStyles.boldLabel);
                UnityEditor.EditorGUILayout.LabelField(audioSettingsDescriptionGUIContent, UnityEditor.EditorStyles.wordWrappedLabel);
                UnityEditor.EditorGUILayout.Space();

                using (new UnityEditor.EditorGUI.IndentLevelScope(1))
                {
                    var serializedObject = new UnityEditor.SerializedObject(target);
                    serializedObject.UpdateIfRequiredOrScript();

                    var sfxProperty = serializedObject.FindProperty(nameof(AudioSettingsAsset._settings))
                        .FindPropertyRelative(nameof(_sfxConfiguration));
                    UnityEditor.EditorGUILayout.PropertyField(sfxProperty, sfxConfigurationGUIContent);

                    var musicProperty = serializedObject.FindProperty(nameof(AudioSettingsAsset._settings))
                        .FindPropertyRelative(nameof(_musicConfiguration));
                    UnityEditor.EditorGUILayout.PropertyField(musicProperty, musicConfigurationGUIContent);

                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
#endif
    }
}
