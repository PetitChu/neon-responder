using UnityEditor;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Represents a base class for managing a settings asset of type <typeparamref name="TSettingsAsset"/>
    /// and its associated settings of type <typeparamref name="TSettings"/>.
    /// </summary>
    /// <typeparam name="TSettingsAsset">
    /// The type of the derived settings asset, which inherits from BaseSettingsAsset.
    /// </typeparam>
    /// <typeparam name="TSettings">
    /// The type of the actual settings that implement the ISettings interface.
    /// </typeparam>
    public abstract class BaseSettingsAsset<TSettingsAsset, TSettings> : ScriptableObject, ISettingsAsset<TSettings>
        where TSettingsAsset : BaseSettingsAsset<TSettingsAsset, TSettings>
        where TSettings : ISettings
    {
        private static readonly string s_resourcesAssetPath = $"Settings/{typeof(TSettingsAsset).Name}";
        private static readonly string s_assetExtension = ".asset";

        private static TSettingsAsset _instance;
        private static string _lastAssetGuid; // Track the asset GUID to detect changes

#if UNITY_EDITOR
        /// <summary>
        /// Generic method to load or create an instance of the settings asset. Editor Only.
        /// </summary>
        /// <returns>A ScriptableObject of type TSettingsAsset.</returns>
        public static TSettingsAsset GetOrCreateSettingsAsset()
        {
            var path = "Assets/Resources/" + s_resourcesAssetPath + s_assetExtension;
            var settings = AssetDatabase.LoadAssetAtPath<TSettingsAsset>(path);

            if (!settings)
            {
                settings = CreateInstance<TSettingsAsset>();
                Debug.LogWarning($"Settings asset for {typeof(TSettingsAsset).Name} not found at {s_resourcesAssetPath}. Creating a new one.");

                // Ensure directory exists
                var directory = System.IO.Path.GetDirectoryName(path);
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                AssetDatabase.CreateAsset(settings, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"New settings asset saved at: {s_resourcesAssetPath}");
            }

            // Update the cached instance and track the GUID
            _instance = settings;
            _lastAssetGuid = AssetDatabase.AssetPathToGUID(path);

            return settings;
        }
#endif

        /// <summary>
        /// Provides a singleton-like access point for the settings asset instance of type <typeparamref name="TSettingsAsset"/>.
        /// Ensures that the instance is loaded from the Resources folder with the predefined path, or returns null if not found.
        /// Automatically reloads the asset if it has changed.
        /// </summary>
        /// <typeparamref name="TSettingsAsset"/> The type of the settings asset.
        /// <returns>
        /// The singleton instance of the settings asset, or null if it cannot be located in the Resources folder.
        /// </returns>
        public static TSettingsAsset InstanceAsset
        {
            get
            {
                // Always check if we need to reload the asset
                if (ShouldReloadAsset())
                    ReloadAsset();

                return _instance;
            }
        }

        /// <summary>
        /// Represents the settings data of type <typeparamref name="TSettings"/> associated with the settings asset.
        /// Provides encapsulated access to the settings instance, allowing internal modification and external retrieval of the current settings configuration.
        /// </summary>
        /// <typeparamref name="TSettings"/> The type of the settings data.
        /// <returns>
        /// The active settings instance that is associated with the current asset.
        /// </returns>
        public TSettings Settings
        {
            get => _settings;
            internal set => _settings = value;
        }

        [SerializeField, HideInInspector]
        public TSettings _settings = default;

        /// <summary>
        /// Determines if the asset should be reloaded by checking if the current instance is null,
        /// or if the underlying asset file has been modified.
        /// </summary>
        /// <returns>True if the asset should be reloaded, false otherwise.</returns>
        private static bool ShouldReloadAsset()
        {
            if (!_instance)
                return true;

#if UNITY_EDITOR
            // Check if the asset file has been modified or deleted
            if (!Application.isPlaying)
            {
                var currentInstance = Resources.Load<TSettingsAsset>(s_resourcesAssetPath);
                
                // Check if asset was deleted
                if (!currentInstance)
                    return true;

                // Check if we're pointing to a different asset instance
                var currentPath = AssetDatabase.GetAssetPath(currentInstance);
                var currentGuid = AssetDatabase.AssetPathToGUID(currentPath);

                // Asset has been replaced or changed
                if (_lastAssetGuid != currentGuid)
                    return true;

                // Check if the asset has been modified
                if (EditorUtility.GetDirtyCount(_instance) > 0 || !AssetDatabase.Contains(_instance))
                    return true;
            }
#endif
            return false;
        }

        /// <summary>
        /// Reloads the asset from the Resources folder and updates the cached instance.
        /// </summary>
        private static void ReloadAsset()
        {
            _instance = Resources.Load<TSettingsAsset>(s_resourcesAssetPath);

#if UNITY_EDITOR
            // Update the tracked GUID in editor
            if (_instance)
            {
                var path = AssetDatabase.GetAssetPath(_instance);
                _lastAssetGuid = AssetDatabase.AssetPathToGUID(path);
            }
            else
            {
                _lastAssetGuid = null;
            }
#endif

            if (!_instance)
                Debug.LogWarning($"Settings asset for {typeof(TSettingsAsset).Name} could not be loaded from path: {s_resourcesAssetPath}");
        }

        /// <summary>
        /// Forces a reload of the settings asset on the next access.
        /// Useful when you know the asset has been modified externally.
        /// </summary>
        public static void ForceReload()
        {
            _instance = null;
            _lastAssetGuid = null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Reset the cached instance when the scriptable object is destroyed
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                _lastAssetGuid = null;
            }
        }
#endif
    }
}
