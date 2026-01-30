using System;
using VContainer;
using VContainer.Unity;

namespace BrainlessLabs.Neon.Lifecycle
{
    /// <summary>
    /// Initializes Unity Gaming Services.
    /// Transitions to GameServicesState when all services are healthy.
    /// </summary>
    internal class UnityServicesState : LifetimeStateMachine
    {
        public readonly Type NextStateType = typeof(GameServicesState);

        public UnityServicesState(LifetimeScope lifetimeScope) : base(lifetimeScope)
        {
        }

        protected override void RegisterTypes(IContainerBuilder builder)
        {
            base.RegisterTypes(builder);
            RegisterNextState(builder);
            RegisterUnityServices(builder);
        }

        private void RegisterNextState(IContainerBuilder builder)
        {
            builder.Register(NextStateType, Lifetime.Transient).AsSelf();
        }

        private static void RegisterUnityServices(IContainerBuilder builder)
        {
#if UNITY_EDITOR
            builder.RegisterEntryPoint<NullUnityServicesInitializer>()
                .As<IUnityServicesInitializer>()
                .As<IHealthCheckedService>();
#else
            builder.RegisterEntryPoint<UnityServicesInitializer>()
                .As<IUnityServicesInitializer>()
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
