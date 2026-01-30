using System;
using System.Collections.Generic;
using UnityEngine;
using UnityHFSM;
using VContainer;
using VContainer.Unity;

namespace BrainlessLabs.Neon.Lifecycle
{
    /// <summary>
    /// Base class for application states that manage a VContainer child scope.
    /// Each state creates a child DI container on enter and disposes it on exit.
    /// </summary>
    public class LifetimeStateMachine : StateMachine, ILifetimeState
    {
        protected const string BASE_STATE = "DefaultState";

        private readonly LifetimeScope _lifetimeScope;
        private LifetimeScope _childLifetimeScope;

        public virtual bool DisposeContainerOnExit => true;

        protected LifetimeStateMachine(LifetimeScope lifetimeScope)
        {
            _lifetimeScope = lifetimeScope;
            AddState(BASE_STATE, new State());
        }

        /// <summary>
        /// Override to register services in the child container.
        /// Called when entering this state.
        /// </summary>
        protected virtual void RegisterTypes(IContainerBuilder builder)
        {
            // Register the health-checked transition factory
            builder.RegisterFactory(HealthCheckedServicesTransition.Factory, Lifetime.Scoped);
        }

        /// <summary>
        /// Called after the child scope is created and ready.
        /// Override to resolve services and configure sub-states.
        /// </summary>
        protected virtual void OnLifetimeScopeReady(IObjectResolver container)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Debug.Log($"[Lifecycle] Entering state: {GetType().Name}");
            TryCreateChildScope();
        }

        public override void OnExit()
        {
            Debug.Log($"[Lifecycle] Exiting state: {GetType().Name}");
            base.OnExit();
            TryDisposeChildScope();
        }

        /// <summary>
        /// Helper to create the next state with a health-checked transition.
        /// </summary>
        protected void CreateAndAddTargetStateWithHealthCheckedTransition(
            IObjectResolver container,
            string stateName,
            Type stateType)
        {
            var transitionFactory = container.Resolve<Func<HealthCheckedServicesTransition.Parameters, HealthCheckedServicesTransition>>();
            var state = (StateBase<string>)container.Resolve(stateType);
            var healthCheckedServices = container.Resolve<IReadOnlyList<IHealthCheckedService>>();

            AddState(stateName, state);

            var parameters = new HealthCheckedServicesTransition.Parameters(
                BASE_STATE,
                stateName,
                forceInstantly: false,
                healthCheckedServices
            );

            AddTwoWayTransition(transitionFactory(parameters));
            RequestStateChange(BASE_STATE, forceInstantly: true);
        }

        private void TryCreateChildScope()
        {
            if (_childLifetimeScope != null)
            {
                return;
            }

            _childLifetimeScope = _lifetimeScope.CreateChild(RegisterTypes);

            if (_childLifetimeScope.Container == null)
            {
                Debug.LogError($"[Lifecycle] Failed to create child scope for {GetType().Name}");
                return;
            }

            OnLifetimeScopeReady(_childLifetimeScope.Container);
        }

        private void TryDisposeChildScope()
        {
            if (!DisposeContainerOnExit || _childLifetimeScope == null)
            {
                return;
            }

            _childLifetimeScope.Dispose();
            _childLifetimeScope = null;
        }
    }
}
