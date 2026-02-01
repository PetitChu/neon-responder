using Eflatun.SceneReference;

namespace BrainlessLabs.Neon
{
    public static class SceneReferenceExtensions
    {
        /// <summary>
        /// Checks if the SceneReference is valid (not null and has no unsafe reason).
        /// </summary>
        /// <param name="sceneReference">The SceneReference to check. </param>
        /// <returns>True if the SceneReference is valid; otherwise, false.</returns>
        public static bool IsValid(this SceneReference sceneReference)
        {
            return sceneReference != null && sceneReference.UnsafeReason == SceneReferenceUnsafeReason.None;
        }
    }
}
