namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Defines the configuration settings for scene management within the application.
    /// </summary>
    internal interface IScenesSettings : ISettings
    {
        /// <summary>
        /// The collection of scene definitions available to the scenes service.
        /// </summary>
        SceneDefinitionAsset[] SceneDefinitions { get; }
    }
}
