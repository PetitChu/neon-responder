using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Pure geometry for the Cinemachine confiner that replaces the old
    /// CameraFollow right-edge clamp + no-backtrack behavior. No Unity side effects.
    /// </summary>
    public static class NeonCameraBounds
    {
        /// <summary>
        /// Build the confiner rect for a right-edge arena lock. The left edge never
        /// retreats (no backtracking), and the rect is always at least one camera
        /// view wide/tall so Cinemachine's Confiner2D doesn't lock the camera solid.
        /// </summary>
        public static Rect ConfinerRect(
            float rightBoundX, float camHalfWidth, float camHalfHeight,
            float centerY, float previousLeftX)
        {
            float minWidth = camHalfWidth * 2f;
            float left = previousLeftX;
            float right = Mathf.Max(rightBoundX, left + minWidth);
            float bottom = centerY - camHalfHeight;
            return new Rect(left, bottom, right - left, camHalfHeight * 2f);
        }
    }
}
