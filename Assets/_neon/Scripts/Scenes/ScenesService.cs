using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Manages scene loading and unloading. Supports one active scene at a time using single load mode.
    /// Enqueues the owning LifetimeScope as parent for any LifetimeScope in the loaded scene,
    /// ensuring the loaded scene's DI scope inherits all registered services.
    /// </summary>
    public class ScenesService : IScenesService
    {
        private readonly SceneDefinitionAsset[] _sceneDefinitions;
        private readonly LifetimeScope _lifetimeScope;

        public SceneDefinitionAsset CurrentScene { get; private set; }

        public ScenesService(LifetimeScope lifetimeScope)
        {
            _lifetimeScope = lifetimeScope;
            var settings = ScenesSettingsAsset.InstanceAsset.Settings;
            _sceneDefinitions = settings.SceneDefinitions;
        }

        public async UniTask LoadSceneAsync(SceneDefinitionAsset sceneDefinition)
        {
            if (sceneDefinition == null)
            {
                Debug.LogWarning("[ScenesService] Cannot load scene: scene definition is null.");
                return;
            }

            var scenePath = sceneDefinition.SceneReference.Path;
            if (string.IsNullOrEmpty(scenePath))
            {
                Debug.LogWarning($"[ScenesService] Cannot load scene '{sceneDefinition.SceneName}': scene reference path is empty.");
                return;
            }

            // Enqueue our scope as parent so LifetimeScopes in the loaded scene inherit services
            LifetimeScope.EnqueueParent(_lifetimeScope);
            await SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
            CurrentScene = sceneDefinition;
        }

        public async UniTask LoadSceneAsync(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("[ScenesService] Cannot load scene: scene name is null or empty.");
                return;
            }

            var sceneDefinition = _sceneDefinitions.FirstOrDefault(sd => sd.SceneName == sceneName);
            if (sceneDefinition == null)
            {
                Debug.LogWarning($"[ScenesService] No scene definition found with name: {sceneName}");
                return;
            }

            await LoadSceneAsync(sceneDefinition);
        }
    }
}
