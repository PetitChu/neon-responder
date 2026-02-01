using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace BrainlessLabs.Neon.Lifecycle
{
    /// <summary>
    /// Final state - the game is running.
    /// Loads the post-bootstrap scene and manages the main game session.
    /// </summary>
    internal class GameState : LifetimeStateMachine
    {
        public GameState(LifetimeScope lifetimeScope) : base(lifetimeScope)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Debug.Log("[Lifecycle] Game state entered. Bootstrap complete!");
        }

        protected override void RegisterTypes(IContainerBuilder builder)
        {
            base.RegisterTypes(builder);

            // Load the post-bootstrap scene
            var settings = BootstrapSettingsAsset.InstanceAsset.Settings;
            if (settings.PostBootstrapScene != null)
            {
                builder.RegisterBuildCallback(_ =>
                {
                    Debug.Log($"[Lifecycle] Loading post-bootstrap scene: {settings.PostBootstrapScene.SceneName}");
                    Services.Scenes.LoadSceneAsync(settings.PostBootstrapScene).Forget(e => Debug.LogException(e));
                });
            }

            // Add game session services here as needed
        }
    }
}
