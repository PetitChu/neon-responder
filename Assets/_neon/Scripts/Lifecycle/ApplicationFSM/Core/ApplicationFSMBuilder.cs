using UnityEngine;
using UnityHFSM;

namespace BrainlessLabs.Neon.Lifecycle
{
    /// <summary>
    /// Builds the root application state machine.
    /// </summary>
    public class ApplicationFSMBuilder
    {
        private const string INITIAL_STATE = "InitialState";
        private const string PLATFORM_STATE = "PlatformState";

        private readonly ObjectFactory _objectFactory;
        private readonly StateMachine _fsm;

        public ApplicationFSMBuilder(ObjectFactory objectFactory)
        {
            _objectFactory = objectFactory;
            _fsm = new StateMachine();
        }

        /// <summary>
        /// Builds and returns the configured state machine.
        /// </summary>
        public StateMachine Build()
        {
            Debug.Log("[Lifecycle] Building application FSM...");

            AddStates();
            AddTransitions();

            _fsm.SetStartState(INITIAL_STATE);

            return _fsm;
        }

        private void AddStates()
        {
            _fsm.AddState(INITIAL_STATE, _objectFactory.Resolve<InitialState>());
            _fsm.AddState(PLATFORM_STATE, _objectFactory.Resolve<PlatformState>());
        }

        private void AddTransitions()
        {
            // Immediate transition from Initial to Platform
            _fsm.AddTransition(INITIAL_STATE, PLATFORM_STATE, _ => true);
        }
    }
}
