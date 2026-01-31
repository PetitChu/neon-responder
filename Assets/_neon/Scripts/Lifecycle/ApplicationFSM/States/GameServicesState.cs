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
            RegisterInputService(builder);
            RegisterScenesService(builder);
        }

        private static void RegisterAudioService(IContainerBuilder builder)
        {
            builder.Register<AudioService>(Lifetime.Singleton)
                .As<IAudioService>();
        }

        private static void RegisterInputService(IContainerBuilder builder)
        {
            builder.Register<InputService>(Lifetime.Singleton)
                .As<IInputService>();
        }

        private static void RegisterScenesService(IContainerBuilder builder)
        {
            builder.Register<ScenesService>(Lifetime.Singleton)
                .As<IScenesService>();
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
            // Force eager initialization of InputService to ensure Instance is set
            container.Resolve<IInputService>();
            // Force eager initialization of ScenesService to ensure Instance is set
            container.Resolve<IScenesService>();
            CreateAndAddTargetStateWithHealthCheckedTransition(
                container,
                NextStateType.Name,
                NextStateType
            );
        }
    }
}
