using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>Whiff desaturate weight: full at the moment of the whiff, linear decay to 0.</summary>
    public static class WhiffFx
    {
        public static float WeightAt(float elapsed, float duration)
        {
            if (duration <= 0f) return 0f;
            return Mathf.Clamp01(1f - elapsed / duration);
        }
    }
}
