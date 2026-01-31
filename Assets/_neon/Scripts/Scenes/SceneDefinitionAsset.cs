using Eflatun.SceneReference;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// A ScriptableObject that defines a scene with its reference, display name, and type.
    /// </summary>
    [CreateAssetMenu(fileName = "SceneDefinition", menuName = "Neon/Scenes/Scene Definition")]
    public class SceneDefinitionAsset : ScriptableObject
    {
        [SerializeField]
        private SceneReference _sceneReference;

        [SerializeField]
        private string _sceneName;

        [SerializeField]
        private SceneType _sceneType;

        /// <summary>
        /// The scene reference used to load this scene.
        /// </summary>
        public SceneReference SceneReference => _sceneReference;

        /// <summary>
        /// The display name of the scene.
        /// </summary>
        public string SceneName => _sceneName;

        /// <summary>
        /// The type of scene (Menu or Level).
        /// </summary>
        public SceneType SceneType => _sceneType;
    }
}
