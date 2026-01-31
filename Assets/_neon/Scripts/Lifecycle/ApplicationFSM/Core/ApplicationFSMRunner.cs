using UnityHFSM;
using VContainer.Unity;

namespace BrainlessLabs.Neon.Lifecycle
{
    /// <summary>
    /// Entry point that initializes and runs the application state machine.
    /// </summary>
    public class ApplicationFSMRunner : IInitializable, ITickable
    {
        private readonly ApplicationFSMBuilder _builder;
        private StateMachine _fsm;

        public ApplicationFSMRunner(ApplicationFSMBuilder builder)
        {
            _builder = builder;
        }

        public void Initialize()
        {
            _fsm = _builder.Build();
            _fsm.Init();
            UnityEngine.Debug.Log("[Lifecycle] Application FSM initialized.");
        }

        public void Tick()
        {
            _fsm.OnLogic();
        }
    }
}
