using System;
using VContainer;
using VContainer.Unity;

namespace BrainlessLabs.Neon.Lifecycle
{
    /// <summary>
    /// Registers the run-agnostic gameplay engine spine (spec §4.1):
    /// IGameplaySignals (event bus), IStatSystem (stat sheets),
    /// IGameplayClock (ordered gameplay tick, driven as an ITickable entry point).
    /// Transitions to GameState when all services are healthy.
    /// </summary>
    internal class GameplayServicesState : LifetimeStateMachine
    {
        public readonly Type NextStateType = typeof(GameState);

        public GameplayServicesState(LifetimeScope lifetimeScope) : base(lifetimeScope)
        {
        }

        protected override void RegisterTypes(IContainerBuilder builder)
        {
            base.RegisterTypes(builder);
            RegisterNextState(builder);
            RegisterGameplaySignals(builder);
            RegisterStatSystem(builder);
            RegisterGameplayClock(builder);
        }

        private static void RegisterGameplaySignals(IContainerBuilder builder)
        {
            builder.Register<GameplaySignals>(Lifetime.Singleton)
                .As<IGameplaySignals>();
        }

        private static void RegisterStatSystem(IContainerBuilder builder)
        {
            builder.Register<StatSystem>(Lifetime.Singleton)
                .As<IStatSystem>();
        }

        private static void RegisterGameplayClock(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<GameplayClock>()
                .As<IGameplayClock>();
        }

        private void RegisterNextState(IContainerBuilder builder)
        {
            builder.Register(NextStateType, Lifetime.Transient).AsSelf();
        }

        protected override void OnLifetimeScopeReady(IObjectResolver container)
        {
            base.OnLifetimeScopeReady(container);
            CreateAndAddTargetStateWithHealthCheckedTransition(
                container,
                NextStateType.Name,
                NextStateType
            );
        }
    }
}
