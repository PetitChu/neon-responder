using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace BrainlessLabs.Neon.Lifecycle
{
    /// <summary>
    /// Root VContainer lifetime scope for the application.
    /// Configures the FSM and entry states.
    /// </summary>
    /// <remarks>
    /// Add new registrations to child scopes within states, not here.
    /// </remarks>
    public class ApplicationLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            var settings = BootstrapSettingsAsset.InstanceAsset.Settings;

            if (!settings.ExecuteBootstrapSequence)
            {
                Debug.Log("[Lifecycle] Bootstrap sequence disabled in settings.");
                return;
            }

            Debug.Log("[Lifecycle] Configuring ApplicationLifetimeScope...");

            // Core FSM infrastructure
            builder.RegisterEntryPoint<ApplicationFSMRunner>().AsSelf();
            builder.Register<ApplicationFSMBuilder>(Lifetime.Transient).AsSelf();
            builder.Register<ObjectFactory>(Lifetime.Singleton).AsSelf();

            // Entry states (resolved by FSMBuilder)
            builder.Register<InitialState>(Lifetime.Transient).AsSelf();
            builder.Register<PlatformState>(Lifetime.Transient).AsSelf();
        }
    }
}
