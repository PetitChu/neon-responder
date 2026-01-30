using R3;
using UnityEngine;
using VContainer.Unity;

namespace BrainlessLabs.Neon.Lifecycle
{
    /// <summary>
    /// Null implementation of Unity Services for editor.
    /// Immediately reports healthy.
    /// </summary>
    public class NullUnityServicesInitializer : IUnityServicesInitializer, IHealthCheckedService, IInitializable
    {
        private readonly ReactiveProperty<ServiceHealthReport> _healthStream = new(ServiceHealthReport.Initializing());

        public bool IsInitialized => true;
        public ReadOnlyReactiveProperty<ServiceHealthReport> HealthStream => _healthStream;

        public void Initialize()
        {
            Debug.Log("[Lifecycle] NullUnityServicesInitializer initialized (UGS disabled in editor).");
            _healthStream.Value = ServiceHealthReport.Healthy("UGS disabled");
        }
    }
}
