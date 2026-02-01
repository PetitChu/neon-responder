using UnityEditor;

namespace BrainlessLabs.Neon.Editor
{
    /// <summary>
    /// Provides functionality to initialize and configure the editor environment.
    /// </summary>
    static class EditorBootstrap
    {
        /// <summary>
        /// Invoked when the Unity Editor loads to initialize the editor environment based on the
        /// Bootstrap settings. This method checks the settings to determine whether the editor
        /// bootstrap process should be enabled or cleared.
        /// </summary>
        /// <remarks>
        /// This method is marked with the [InitializeOnLoadMethod] attribute, ensuring it is
        /// automatically executed when the Unity Editor finishes loading.
        /// The method retrieves the bootstrap settings from a settings asset, checks the
        /// configuration, and either sets up or clears the internal bootstrap process accordingly.
        /// </remarks>
        [InitializeOnLoadMethod]
        static void OnEditorLoad()
        {
            var bootstrapSettings = BootstrapSettingsAsset.GetOrCreateSettingsAsset().Settings;
            if (bootstrapSettings != null)
            {
                if (!bootstrapSettings.EnableEditorBootstrap)
                {
                    EditorBootstrapUtilities.ClearInternalBootstrap();
                }
                else
                {
                    EditorBootstrapUtilities.SetupInternalBootstrap();
                }
            }
        }
    }
}
