namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Represents an interface that defines an abstraction for settings assets.
    /// Provides a consistent way to access and manage application settings of a specified type.
    /// </summary>
    /// <typeparam name="TSettings">The type of the settings object. Must implement the <see cref="ISettings"/> interface.</typeparam>
    interface ISettingsAsset<out TSettings> where TSettings : ISettings
    {
        /// <summary>
        /// Represents the settings instance associated with a specific configuration or application state.
        /// Provides access to the underlying settings that implement the <see cref="ISettings"/> interface.
        /// </summary>
        TSettings Settings { get; }
    }
}
