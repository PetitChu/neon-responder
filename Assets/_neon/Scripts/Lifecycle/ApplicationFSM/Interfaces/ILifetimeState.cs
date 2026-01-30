namespace BrainlessLabs.Neon.Lifecycle
{
    /// <summary>
    /// Interface for states that manage a VContainer lifetime scope.
    /// </summary>
    public interface ILifetimeState
    {
        /// <summary>
        /// If true, the child container is disposed when exiting this state.
        /// </summary>
        bool DisposeContainerOnExit { get; }

        void OnEnter();
        void OnExit();
    }
}
