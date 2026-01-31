using Cysharp.Threading.Tasks;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Provides scene management operations for loading and unloading scenes.
    /// Supports one active scene at a time using single load mode.
    /// </summary>
    public interface IScenesService
    {
        /// <summary>
        /// The currently loaded scene definition, or null if no scene is loaded through this service.
        /// </summary>
        SceneDefinitionAsset CurrentScene { get; }

        /// <summary>
        /// Loads a scene by its definition asset, replacing the current scene.
        /// </summary>
        /// <param name="sceneDefinition">The scene definition to load.</param>
        UniTask LoadSceneAsync(SceneDefinitionAsset sceneDefinition);

        /// <summary>
        /// Loads a scene by name from the registered scene definitions, replacing the current scene.
        /// </summary>
        /// <param name="sceneName">The name of the scene to load.</param>
        UniTask LoadSceneAsync(string sceneName);
    }
}
