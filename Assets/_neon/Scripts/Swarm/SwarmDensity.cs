using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>Resolves the live chaff cap from a progression curve (or a flat fallback),
    /// scaled by the Signal nastiness, clamped to the 150 proxy-pool ceiling.</summary>
    public static class SwarmDensity
    {
        public const int ProxyPoolCeiling = 150;

        public static int ResolveChaffCap(int flatCap, AnimationCurve curve, float progression, float nastiness)
        {
            float baseCap = (curve != null && curve.length > 0)
                ? curve.Evaluate(Mathf.Clamp01(progression))
                : flatCap;
            return Mathf.Min(ProxyPoolCeiling, Mathf.RoundToInt(baseCap * nastiness));
        }
    }
}
