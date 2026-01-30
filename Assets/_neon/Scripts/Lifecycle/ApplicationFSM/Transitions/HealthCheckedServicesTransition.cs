using System;
using System.Collections.Generic;
using System.Linq;
using R3;
using VContainer;

namespace BrainlessLabs.Neon.Lifecycle
{
    /// <summary>
    /// Transition that waits for all registered IHealthCheckedService instances to report healthy.
    /// Uses R3 to combine health streams with debouncing for stability.
    /// </summary>
    public class HealthCheckedServicesTransition : ApplicationTransition, IDisposable
    {
        private readonly IDisposable _subscription = null;
        private bool _allHealthy;

        public override bool ShouldTransition() => _allHealthy;

        internal HealthCheckedServicesTransition(Parameters parameters)
            : base(parameters.From, parameters.To, parameters.ForceInstantly)
        {
            var services = parameters.HealthCheckedServices;

            if (services == null || services.Count == 0)
            {
                // No services to check, immediately healthy
                _allHealthy = true;
                return;
            }

            var healthStreams = services
                .Select(service => service.HealthStream.Select(report => report.Status == ServiceStatus.Healthy))
                .ToArray();

            _subscription = Observable.CombineLatest(healthStreams)
                .Select(statuses => statuses.All(healthy => healthy))
                .DistinctUntilChanged()
                .Debounce(TimeSpan.FromSeconds(0.5f))
                .Subscribe(allHealthy =>
                {
                    _allHealthy = allHealthy;
                    if (allHealthy)
                    {
                        UnityEngine.Debug.Log($"[Lifecycle] All services healthy, ready to transition: {From} → {To}");
                    }
                });
        }

        public void Dispose()
        {
            _subscription?.Dispose();
        }

        #region Factory

        public readonly struct Parameters
        {
            public string From { get; }
            public string To { get; }
            public bool ForceInstantly { get; }
            public IReadOnlyList<IHealthCheckedService> HealthCheckedServices { get; }

            public Parameters(
                string from,
                string to,
                bool forceInstantly,
                IReadOnlyList<IHealthCheckedService> healthCheckedServices)
            {
                From = from;
                To = to;
                ForceInstantly = forceInstantly;
                HealthCheckedServices = healthCheckedServices;
            }
        }

        public static Func<Parameters, HealthCheckedServicesTransition> Factory(IObjectResolver _) =>
            parameters => new HealthCheckedServicesTransition(parameters);

        #endregion
    }
}
