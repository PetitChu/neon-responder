using UnityEngine;

namespace BrainlessLabs.Neon.Lifecycle
{
    /// <summary>
    /// ScriptableObject container for BootstrapSettings.
    /// Create via Assets > Create > Neon > Bootstrap Settings.
    /// </summary>
    [CreateAssetMenu(fileName = "BootstrapSettings", menuName = "Neon/Bootstrap Settings")]
    public class BootstrapSettingsAsset : ScriptableObject
    {
        private static BootstrapSettingsAsset _instance;

        [SerializeField]
        private BootstrapSettings _settings = new();

        public BootstrapSettings Settings => _settings;

        /// <summary>
        /// Singleton accessor. Loads from Resources/BootstrapSettings.
        /// </summary>
        public static BootstrapSettingsAsset Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<BootstrapSettingsAsset>("BootstrapSettings");

                    if (_instance == null)
                    {
                        Debug.LogWarning("[Lifecycle] BootstrapSettingsAsset not found in Resources. Using defaults.");
                        _instance = CreateInstance<BootstrapSettingsAsset>();
                    }
                }
                return _instance;
            }
        }
    }
}
