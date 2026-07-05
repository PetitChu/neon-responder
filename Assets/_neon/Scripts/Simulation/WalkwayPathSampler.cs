using Unity.Collections;
using Unity.Mathematics;

namespace BrainlessLabs.Neon.Simulation
{
    /// <summary>
    /// Pure, Burst-compatible looping polyline sampler. Points live in a shared buffer;
    /// (start,count) selects one path. The path closes back to its first point and the
    /// distance wraps around the loop's total length, so walkers circulate forever.
    /// </summary>
    public static class WalkwayPathSampler
    {
        public static void Sample(in NativeArray<float2> points, int start, int count,
            float distance, out float2 position, out float2 direction)
        {
            if (count < 2)
            {
                position = count == 1 ? points[start] : float2.zero;
                direction = new float2(1f, 0f);
                return;
            }

            float total = 0f;
            for (int i = 0; i < count; i++)
                total += math.distance(points[start + i], points[start + (i + 1) % count]);

            float d = total > 1e-5f ? math.fmod(math.fmod(distance, total) + total, total) : 0f;

            for (int i = 0; i < count; i++)
            {
                float2 a = points[start + i];
                float2 b = points[start + (i + 1) % count];
                float seg = math.distance(a, b);
                if (d <= seg || i == count - 1)
                {
                    float t = seg > 1e-5f ? d / seg : 0f;
                    position = math.lerp(a, b, t);
                    direction = math.normalizesafe(b - a, new float2(1f, 0f));
                    return;
                }
                d -= seg;
            }
            position = points[start]; direction = new float2(1f, 0f);
        }
    }
}
