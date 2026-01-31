using System;
using VContainer;
using VContainer.Unity;

namespace BrainlessLabs.Neon.Lifecycle
{
    /// <summary>
    /// Initializes game-specific services.
    /// Currently a placeholder for future services.
    /// Transitions to GameState when all services are healthy.
    /// </summary>
    internal class GameServicesState : LifetimeStateMachine
    {
        public readonly Type NextStateType = typeof(GameState);

        public GameServicesState(LifetimeScope lifetimeScope) : base(lifetimeScope)
        {
        }

        protected override void RegisterTypes(IContainerBuilder builder)
        {
            base.RegisterTypes(builder);
            RegisterNextState(builder);
            RegisterAudioService(builder);
        }

        private static void RegisterAudioService(IContainerBuilder builder)
        {
            builder.Register<AudioService>(Lifetime.Singleton)
                .As<IAudioService>();
        }

        private void RegisterNextState(IContainerBuilder builder)
        {
            builder.Register(NextStateType, Lifetime.Transient).AsSelf();
        }

        protected override void OnLifetimeScopeReady(IObjectResolver container)
        {
            base.OnLifetimeScopeReady(container);
            // Force eager initialization of AudioService to ensure Instance is set
            container.Resolve<IAudioService>();
            CreateAndAddTargetStateWithHealthCheckedTransition(
                container,
                NextStateType.Name,
                NextStateType
            );
        }
    }
}
