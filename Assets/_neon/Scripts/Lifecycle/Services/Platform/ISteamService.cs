namespace BrainlessLabs.Neon.Lifecycle
{
    /// <summary>
    /// Interface for Steam platform service.
    /// </summary>
    public interface ISteamService
    {
        bool IsInitialized { get; }
    }
}
