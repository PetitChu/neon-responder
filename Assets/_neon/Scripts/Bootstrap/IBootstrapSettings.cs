using Eflatun.SceneReference;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Defines the settings required for the bootstrap process in the application.
    /// </summary>
    internal interface IBootstrapSettings : ISettings
    {
        /// <summary>
        /// When enabled, the assigned bootstrap scene will be loaded into the EditorSceneManager.playModeStartScene as
        /// the initial scene, and will then load the PostBootstrapScene after the bootstrap is complete.
        /// </summary>
        bool EnableEditorBootstrap { get; }
        
        /// <summary>
        /// Specifies the scene to use as the initial bootstrap scene when entering Play Mode in the Unity Editor. (Can be an empty scene)
        /// </summary>
        SceneReference BootstrapScene { get; }

        /// <summary>
        /// Specifies the scene to transition to once the initial bootstrap configuration and setup have been executed.
        /// </summary>
        SceneReference PostBootstrapScene { get; }
        
        /// <summary>
        /// When enabled, the bootstrap sequence will be executed, initializing and configuring necessary services and components as defined in the bootstrap process.
        /// </summary>
        bool ExecuteBootstrapSequence { get; }
    }
}
