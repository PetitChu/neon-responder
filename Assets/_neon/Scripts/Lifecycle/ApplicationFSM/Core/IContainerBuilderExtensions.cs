using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace BrainlessLabs.Neon.Lifecycle
{
    /// <summary>
    /// Extension methods for IContainerBuilder to simplify common registration patterns.
    /// </summary>
    public static class IContainerBuilderExtensions
    {
        /// <summary>
        /// Instantiates a prefab and registers a component from it.
        /// The instance is injected, marked DontDestroyOnLoad, and destroyed when the scope is disposed.
        /// </summary>
        public static void InstantiateAndRegisterPrefab<T>(this IContainerBuilder builder, GameObject prefab)
            where T : Component
        {
            bool wasActive = prefab.activeSelf;

            try
            {
                if (wasActive)
                {
                    prefab.SetActive(false);
                }

                var instance = Object.Instantiate(prefab);
                var component = instance.GetComponent<T>();

                builder.RegisterInstance(component).As<T>();
                builder.RegisterBuildCallback(container =>
                {
                    container.InjectGameObject(instance);
                    if (wasActive)
                    {
                        instance.SetActive(true);
                    }
                    Object.DontDestroyOnLoad(instance);
                });
                builder.RegisterDisposeCallback(_ =>
                {
                    if (instance != null)
                    {
                        Object.Destroy(instance);
                    }
                });
            }
            finally
            {
                if (wasActive)
                {
                    prefab.SetActive(true);
                }
            }
        }

        /// <summary>
        /// Creates a new GameObject with a single component and registers it.
        /// The instance is marked DontDestroyOnLoad and destroyed when the scope is disposed.
        /// </summary>
        public static void InstantiateAndRegisterSingleComponent<T>(this IContainerBuilder builder)
            where T : Component
        {
            var typeName = typeof(T).Name;
            var gameObject = new GameObject(typeName, typeof(T));
            var component = gameObject.GetComponent<T>();

            builder.RegisterInstance(component).As<T>();
            builder.RegisterBuildCallback(_ =>
            {
                Object.DontDestroyOnLoad(gameObject);
            });
            builder.RegisterDisposeCallback(_ =>
            {
                if (gameObject != null)
                {
                    Object.Destroy(gameObject);
                }
            });
        }
    }
}
