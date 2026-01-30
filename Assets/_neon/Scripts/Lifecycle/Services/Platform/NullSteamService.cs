using R3;
using UnityEngine;
using VContainer.Unity;

namespace BrainlessLabs.Neon.Lifecycle
{
    /// <summary>
    /// Null implementation of Steam service for editor and non-Steam builds.
    /// Immediately reports healthy.
    /// </summary>
    public class NullSteamService : ISteamService, IHealthCheckedService, IInitializable
    {
        private readonly ReactiveProperty<ServiceHealthReport> _healthStream = new(ServiceHealthReport.Initializing());

        public bool IsInitialized => true;
        public ReadOnlyReactiveProperty<ServiceHealthReport> HealthStream => _healthStream;

        public void Initialize()
        {
            Debug.Log("[Lifecycle] NullSteamService initialized (Steam disabled).");
            _healthStream.Value = ServiceHealthReport.Healthy("Steam disabled");
        }
    }
}
