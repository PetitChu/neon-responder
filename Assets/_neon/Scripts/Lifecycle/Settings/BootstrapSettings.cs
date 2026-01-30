using System;
using Eflatun.SceneReference;

namespace BrainlessLabs.Neon.Lifecycle
{
    /// <summary>
    /// Runtime bootstrap configuration.
    /// </summary>
    [Serializable]
    public class BootstrapSettings
    {
        /// <summary>
        /// If false, the bootstrap sequence is skipped entirely.
        /// </summary>
        public bool ExecuteBootstrapSequence = true;

        /// <summary>
        /// Scene to load after bootstrap completes.
        /// </summary>
        public SceneReference PostBootstrapScene;
    }
}
