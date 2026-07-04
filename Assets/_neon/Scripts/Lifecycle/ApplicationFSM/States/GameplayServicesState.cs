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
            RegisterScenesService(builder);
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

        // F1 spine-visibility fix: ScenesService captures its owning scope as the DI
        // parent of every loaded scene (LifetimeScope.EnqueueParent). It must live in
        // the DEEPEST session scope or scenes cannot resolve the spine services.
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
            CreateAndAddTargetStateWithHealthCheckedTransition(
                container,
                NextStateType.Name,
                NextStateType
            );
        }
    }
}
