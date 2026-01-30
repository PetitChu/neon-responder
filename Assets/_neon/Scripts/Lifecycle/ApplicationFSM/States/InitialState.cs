using VContainer;
using VContainer.Unity;

namespace BrainlessLabs.Neon.Lifecycle
{
    /// <summary>
    /// First state after application launch.
    /// Used for reading configuration overrides or special boot procedures.
    /// Transitions immediately to PlatformState.
    /// </summary>
    internal class InitialState : LifetimeStateMachine
    {
        public InitialState(LifetimeScope lifetimeScope) : base(lifetimeScope)
        {
        }

        protected override void RegisterTypes(IContainerBuilder builder)
        {
            base.RegisterTypes(builder);
            // Add any early initialization services here
        }
    }
}
