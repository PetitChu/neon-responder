namespace BrainlessLabs.Neon.Lifecycle
{
    /// <summary>
    /// Interface for Unity Gaming Services initializer.
    /// </summary>
    public interface IUnityServicesInitializer
    {
        bool IsInitialized { get; }
    }
}
