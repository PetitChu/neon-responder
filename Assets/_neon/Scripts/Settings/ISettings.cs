namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Defines the base interface for various settings configurations within the application.
    /// Serves as a marker interface for organizing and categorizing specific settings implementations.
    /// </summary>
    public interface ISettings
    {
#if UNITY_EDITOR
        /// <summary>
        /// Represents a method used to render and control the graphical user interface (GUI) for editing settings in the Unity Editor.
        /// Typically invoked within the Unity Editor context to allow developers to customize and configure settings associated with the implementation.
        /// </summary>
        void Editor_OnGUI(UnityEngine.Object target);
#endif
    }
}
