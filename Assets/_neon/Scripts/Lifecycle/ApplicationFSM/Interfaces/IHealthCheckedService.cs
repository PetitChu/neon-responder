using R3;

namespace BrainlessLabs.Neon.Lifecycle
{
    /// <summary>
    /// Interface for services that report their health status.
    /// Used by HealthCheckedServicesTransition to determine when to transition.
    /// </summary>
    public interface IHealthCheckedService
    {
        /// <summary>
        /// Observable stream of health reports. Must emit at least once.
        /// Transition occurs when all services report Healthy.
        /// </summary>
        ReadOnlyReactiveProperty<ServiceHealthReport> HealthStream { get; }
    }
}
