#if STEAMWORKS_NET
using R3;
using Steamworks;
using UnityEngine;
using VContainer.Unity;

namespace BrainlessLabs.Neon.Lifecycle
{
    /// <summary>
    /// Steam service implementation using Steamworks.NET.
    /// Initializes Steam API and reports health status.
    /// </summary>
    public class SteamService : ISteamService, IHealthCheckedService, IInitializable, ITickable, System.IDisposable
    {
        private readonly ReactiveProperty<ServiceHealthReport> _healthStream = new(ServiceHealthReport.Initializing());

        public bool IsInitialized { get; private set; }
        public ReadOnlyReactiveProperty<ServiceHealthReport> HealthStream => _healthStream;

        public void Initialize()
        {
            Debug.Log("[Lifecycle] Initializing Steam...");

            try
            {
                if (SteamAPI.RestartAppIfNecessary(AppId.Invalid))
                {
                    Debug.Log("[Lifecycle] Steam requested app restart.");
                    Application.Quit();
                    return;
                }

                if (!SteamAPI.Init())
                {
                    _healthStream.Value = ServiceHealthReport.Unhealthy("SteamAPI.Init() failed. Is Steam running?");
                    Debug.LogError("[Lifecycle] SteamAPI.Init() failed.");
                    return;
                }

                IsInitialized = true;
                _healthStream.Value = ServiceHealthReport.Healthy("Steam initialized");
                Debug.Log($"[Lifecycle] Steam initialized. User: {SteamFriends.GetPersonaName()}");
            }
            catch (System.Exception ex)
            {
                _healthStream.Value = ServiceHealthReport.Unhealthy($"Steam exception: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        public void Tick()
        {
            if (IsInitialized)
            {
                SteamAPI.RunCallbacks();
            }
        }

        public void Dispose()
        {
            if (IsInitialized)
            {
                SteamAPI.Shutdown();
                IsInitialized = false;
                Debug.Log("[Lifecycle] Steam shutdown.");
            }
        }
    }
}
#endif
