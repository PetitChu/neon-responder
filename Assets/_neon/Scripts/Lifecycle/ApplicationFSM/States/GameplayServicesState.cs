using System;
using System.Collections.Generic;
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
            RegisterMomentumSystem(builder);
            RegisterNullSwarmBridge(builder);
            RegisterEconomySystem(builder);
            RegisterSignalSystem(builder); // before ProtocolService — it consumes ISignalSystem
            RegisterProtocolService(builder);
            RegisterProgressionSystem(builder);
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

        private static void RegisterMomentumSystem(IContainerBuilder builder)
        {
            builder.Register<MomentumSystem>(Lifetime.Singleton)
                .WithParameter(MomentumConfig.FromSettings())
                .As<IMomentumSystem>();
            builder.RegisterBuildCallback(container => container.Resolve<IMomentumSystem>());
        }

        // Session default: scenes without a swarm (menus, training hall) still
        // inject ISwarmBridge safely. Level scopes shadow this with SwarmBridge.
        private static void RegisterNullSwarmBridge(IContainerBuilder builder)
        {
            builder.Register<NullSwarmBridge>(Lifetime.Singleton)
                .As<ISwarmBridge>();
        }

        private static void RegisterEconomySystem(IContainerBuilder builder)
        {
            builder.Register<EconomySystem>(Lifetime.Singleton)
                .WithParameter(GrowthConfig.FromSettings())
                .As<IEconomySystem>();
            builder.RegisterBuildCallback(container => container.Resolve<IEconomySystem>());
        }

        private static void RegisterProtocolService(IContainerBuilder builder)
        {
            builder.Register<ProtocolService>(Lifetime.Singleton)
                .WithParameter<IReadOnlyList<ProtocolDefinitionAsset>>(GrowthSettingsAsset.InstanceAsset.Settings.ProtocolCatalog)
                .WithParameter<int>(0) // unseeded RNG at runtime; tests seed explicitly
                .As<IProtocolService>();
        }

        // Signal is run-agnostic → session scope (spec §4.3).
        private static void RegisterSignalSystem(IContainerBuilder builder)
        {
            var runSettings = RunSettingsAsset.InstanceAsset.Settings;
            builder.Register<SignalSystem>(Lifetime.Singleton)
                .WithParameter("dawnValue", runSettings.DawnValue)
                .WithParameter("maxSpawnNastinessBonus", runSettings.MaxSpawnNastinessBonus)
                .As<ISignalSystem>();
            builder.RegisterBuildCallback(container => container.Resolve<ISignalSystem>());
        }

        private static void RegisterProgressionSystem(IContainerBuilder builder)
        {
            builder.Register<ProgressionSystem>(Lifetime.Singleton)
                .WithParameter(GrowthConfig.FromSettings())
                .As<IProgressionSystem>();
            builder.RegisterBuildCallback(container => container.Resolve<IProgressionSystem>());
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
