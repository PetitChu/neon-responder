using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Scene scope for menu scenes (scenes without a Level). Extends LifetimeScope
    /// so the scene inherits application services when loaded through IScenesService
    /// (which enqueues the app scope as parent), and injects all scene MonoBehaviours
    /// with [Inject] fields — e.g. UIButton needs IAudioService / IInputService.
    /// Mirrors Level's scene-injection callback.
    /// </summary>
    public class MenuScene : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterBuildCallback(container =>
            {
                // Inject all scene MonoBehaviours with [Inject] attributes
                foreach (var root in gameObject.scene.GetRootGameObjects())
                {
                    container.InjectGameObject(root);
                }
            });
        }
    }
}
