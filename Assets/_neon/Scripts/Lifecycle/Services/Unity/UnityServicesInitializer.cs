using R3;
using UnityEngine;
using Unity.Services.Core;
using VContainer.Unity;

namespace BrainlessLabs.Neon.Lifecycle
{
    /// <summary>
    /// Initializes Unity Gaming Services.
    /// </summary>
    public class UnityServicesInitializer : IUnityServicesInitializer, IHealthCheckedService, IStartable
    {
        private readonly ReactiveProperty<ServiceHealthReport> _healthStream = new(ServiceHealthReport.Initializing());

        public bool IsInitialized { get; private set; }
        public ReadOnlyReactiveProperty<ServiceHealthReport> HealthStream => _healthStream;

        public async void Start()
        {
            Debug.Log("[Lifecycle] Initializing Unity Services...");

            try
            {
                await UnityServices.InitializeAsync();
                IsInitialized = true;
                _healthStream.Value = ServiceHealthReport.Healthy("Unity Services initialized");
                Debug.Log("[Lifecycle] Unity Services initialized.");
            }
            catch (System.Exception ex)
            {
                _healthStream.Value = ServiceHealthReport.Unhealthy($"UGS init failed: {ex.Message}");
                Debug.LogException(ex);
            }
        }
    }
}
