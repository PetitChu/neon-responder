using Unity.Collections;
using Unity.Mathematics;

namespace BrainlessLabs.Neon.Simulation
{
    /// <summary>
    /// Pure, Burst-compatible crowd steering: seek target + separation + light cohesion.
    /// No alignment (alignment reintroduces the marching-column read). Scans the crowd
    /// array O(n) per call; the caller skips the agent itself via distance epsilon.
    /// </summary>
    public static class SwarmSteering
    {
        public static float2 ComputeDesiredVelocity(
            float2 self, float2 target, in NativeArray<float2> crowd,
            float moveSpeed, float separationRadius, float separationWeight,
            float cohesionRadius, float cohesionWeight, float stopRadius)
        {
            float2 separation = float2.zero;
            float2 cohesionCenter = float2.zero;
            int cohesionCount = 0;
            float sepSq = separationRadius * separationRadius;
            float cohSq = cohesionRadius * cohesionRadius;

            for (int i = 0; i < crowd.Length; i++)
            {
                float2 offset = self - crowd[i];
                float distSq = math.lengthsq(offset);
                if (distSq < 1e-6f) continue; // the agent itself / coincident
                if (distSq < sepSq)
                    separation += math.normalizesafe(offset) * (1f - math.sqrt(distSq) / separationRadius);
                if (distSq < cohSq) { cohesionCenter += crowd[i]; cohesionCount++; }
            }

            float2 toTarget = target - self;
            float2 seek = math.length(toTarget) > stopRadius ? math.normalizesafe(toTarget) : float2.zero;

            float2 cohesion = cohesionCount > 0
                ? math.normalizesafe(cohesionCenter / cohesionCount - self)
                : float2.zero;

            float2 steer = seek + separation * separationWeight + cohesion * cohesionWeight;
            float len = math.length(steer);
            if (len < 1e-5f) return float2.zero;
            return steer / len * moveSpeed * math.min(1f, len);
        }
    }
}
