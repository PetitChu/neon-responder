using UnityEditor;
using UnityEditor.SceneManagement;

namespace BrainlessLabs.Neon.Editor
{
    /// <summary>
    /// Class containing utility methods for managing internal bootstrap configurations
    /// related to the play mode start scene within the Unity Editor.
    /// </summary>
    static class EditorBootstrapUtilities
    {
        /// <summary>
        /// Clears the internal bootstrap by setting the play mode start scene to null
        /// if a start scene is currently assigned.
        /// </summary>
        internal static void ClearInternalBootstrap()
        {
            if (EditorSceneManager.playModeStartScene != null)
            {
                EditorSceneManager.playModeStartScene = null;
            }
        }
        
        /// <summary>
        /// Configures the internal bootstrap by setting the play mode start scene to the specified bootstrap scene
        /// if the current start scene is not already assigned.
        /// </summary>
        internal static void SetupInternalBootstrap()
        {
            if (EditorSceneManager.playModeStartScene == null)
            {
                var sceneRef = BootstrapSettingsAsset.GetOrCreateSettingsAsset().Settings.BootstrapScene;
                if (!sceneRef.IsValid())
                    return;
                
                string bootstrapScene = sceneRef.Name;
                EditorBuildSettingsScene[] buildSettingsScenes = EditorBuildSettings.scenes;
                if (buildSettingsScenes == null || buildSettingsScenes.Length == 0)
                {
                    return;
                }

                for (int i = 0; i < buildSettingsScenes.Length; i++)
                {
                    if (ExtractEditorSceneName(buildSettingsScenes[i].path) == bootstrapScene)
                    {
                        SceneAsset newStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(buildSettingsScenes[i].path);
                        if (newStartScene != null)
                        {
                            EditorSceneManager.playModeStartScene = newStartScene;
                        }

                        break;
                    }
                }
            }
        }
        
        /// <summary>
        /// Extracts the name of the scene from the provided scene path.
        /// </summary>
        /// <param name="scenePath">The file path of the scene.</param>
        /// <returns>The name of the scene extracted from the path.</returns>
        static string ExtractEditorSceneName(string scenePath)
        {
            string[] subs = scenePath.Split('/');
            string name = subs[^1];
            name = name.Split('.')[0];

            return name;
        }
    }
}
