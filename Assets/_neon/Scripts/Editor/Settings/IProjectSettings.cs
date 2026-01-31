namespace BrainlessLabs.Neon.Editor
{
    /// <summary>
    /// Represents the interface for defining project settings within the application.
    /// It provides access to configuration settings utilized in project initialization
    /// and bootstrap processes.
    /// </summary>
    internal interface IProjectSettings
    {
        /// <summary>
        /// Represents the configuration settings related to the application's bootstrap process.
        /// Provides access to initialization parameters such as enabling editor bootstrap and specific scene definitions.
        /// </summary>
        IBootstrapSettings BootstrapSettings { get; }

        /// <summary>
        /// Defines a ScriptableObject for managing and storing bootstrap-related configuration settings
        /// specific to the application's initialization process within the CoreXR framework.
        /// </summary>
        BootstrapSettingsAsset BootstrapSettingsAsset { get; }


        /// <summary>
        /// Represents the configuration settings related to audio within the application.
        /// Provides access to audio-specific parameters and functionality for managing
        /// the application's audio behaviors and preferences.
        /// </summary>
        IAudioSettings AudioSettings { get; }

        /// <summary>
        /// Represents the configuration asset responsible for storing and providing access to audio-related settings within the application.
        /// Facilitates centralized management of audio settings utilized during runtime or editor interaction.
        /// </summary>
        AudioSettingsAsset AudioSettingsAsset { get; }

        /// <summary>
        /// Represents the configuration settings related to scene management within the application.
        /// Provides access to scene definitions and scene service configuration.
        /// </summary>
        IScenesSettings ScenesSettings { get; }

        /// <summary>
        /// Represents the configuration asset responsible for storing and providing access to scene-related settings within the application.
        /// Facilitates centralized management of scene settings utilized during runtime or editor interaction.
        /// </summary>
        ScenesSettingsAsset ScenesSettingsAsset { get; }
    }
}
