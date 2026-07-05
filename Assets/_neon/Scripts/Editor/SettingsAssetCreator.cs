using UnityEditor;
using UnityEngine;

namespace BrainlessLabs.Neon.Editor
{
    /// <summary>
    /// Settings assets only auto-create via the editor-side GetOrCreateSettingsAsset
    /// (M1 execution deviation 2: Play mode just Resources.Loads and crashes if the
    /// asset is missing). One menu item creates every settings singleton the game needs.
    /// </summary>
    public static class SettingsAssetCreator
    {
        [MenuItem("Neon/Settings/Create All Settings Assets")]
        public static void CreateAll()
        {
            BootstrapSettingsAsset.GetOrCreateSettingsAsset();
            AudioSettingsAsset.GetOrCreateSettingsAsset();
            ScenesSettingsAsset.GetOrCreateSettingsAsset();
            EngagementSettingsAsset.GetOrCreateSettingsAsset();
            GrowthSettingsAsset.GetOrCreateSettingsAsset();
            AssetDatabase.SaveAssets();
            Debug.Log("[Neon] All settings assets present under Assets/Resources/Settings/.");
        }
    }
}
