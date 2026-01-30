using System;
using System.Collections.Generic;
using VContainer;
using VContainer.Unity;

namespace BrainlessLabs.Neon.Lifecycle
{
    /// <summary>
    /// Initializes platform services (Steam).
    /// Transitions to UnityServicesState when all services are healthy.
    /// </summary>
    internal class PlatformState : LifetimeStateMachine
    {
        public readonly Type NextStateType = typeof(UnityServicesState);

        public PlatformState(LifetimeScope lifetimeScope) : base(lifetimeScope)
        {
        }

        protected override void RegisterTypes(IContainerBuilder builder)
        {
            base.RegisterTypes(builder);
            RegisterNextState(builder);
            RegisterPlatformServices(builder);
        }

        private void RegisterNextState(IContainerBuilder builder)
        {
            builder.Register(NextStateType, Lifetime.Transient).AsSelf();
        }

        private static void RegisterPlatformServices(IContainerBuilder builder)
        {
#if UNITY_EDITOR || !STEAMWORKS_NET
            builder.RegisterEntryPoint<NullSteamService>()
                .As<ISteamService>()
                .As<IHealthCheckedService>();
#else
            builder.RegisterEntryPoint<SteamService>()
                .As<ISteamService>()
                .As<IHealthCheckedService>();
#endif
        }

        protected override void OnLifetimeScopeReady(IObjectResolver container)
        {
            base.OnLifetimeScopeReady(container);
            CreateAndAddTargetStateWithHealthCheckedTransition(
                container,
                nameof(NextStateType),
                NextStateType
            );
        }
    }
}
