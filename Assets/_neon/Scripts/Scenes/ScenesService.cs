using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Manages scene loading and unloading. Supports one active scene at a time using single load mode.
    /// </summary>
    public class ScenesService : IScenesService
    {
        private readonly SceneDefinitionAsset[] _sceneDefinitions;

        public SceneDefinitionAsset CurrentScene { get; private set; }

        public ScenesService()
        {
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
