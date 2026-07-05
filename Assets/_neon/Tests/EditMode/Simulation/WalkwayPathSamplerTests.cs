using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BrainlessLabs.Neon.Simulation;

namespace BrainlessLabs.Neon.Tests
{
    public class WalkwayPathSamplerTests
    {
        static (float2 pos, float2 dir) Sample(float2[] pts, float dist)
        {
            var arr = new NativeArray<float2>(pts, Allocator.Temp);
            try { WalkwayPathSampler.Sample(arr, 0, arr.Length, dist, out var p, out var d); return (p, d); }
            finally { arr.Dispose(); }
        }

        [Test]
        public void Midway_along_a_straight_segment()
        {
            var (p, d) = Sample(new[] { new float2(0, 0), new float2(10, 0) }, 3f);
            Assert.AreEqual(3f, p.x, 0.001f); Assert.AreEqual(0f, p.y, 0.001f);
            Assert.AreEqual(1f, d.x, 0.001f);
        }

        [Test]
        public void Distance_past_the_end_continues_onto_the_closing_return_leg()
        {
            // 2-point loop = out 10 + back 10 (total 20); 13 → 3 into the return leg.
            var (p, d) = Sample(new[] { new float2(0, 0), new float2(10, 0) }, 13f);
            Assert.AreEqual(7f, p.x, 0.001f);
            Assert.AreEqual(-1f, d.x, 0.001f);
        }

        [Test]
        public void Distance_past_the_full_loop_wraps_to_the_start()
        {
            var (p, d) = Sample(new[] { new float2(0, 0), new float2(10, 0) }, 23f); // 23 mod 20 → 3
            Assert.AreEqual(3f, p.x, 0.001f);
            Assert.AreEqual(1f, d.x, 0.001f);
        }

        [Test]
        public void Crosses_into_the_second_segment_of_an_L()
        {
            var (p, d) = Sample(new[] { new float2(0, 0), new float2(10, 0), new float2(10, 10) }, 15f);
            Assert.AreEqual(10f, p.x, 0.001f); Assert.AreEqual(5f, p.y, 0.001f);
            Assert.AreEqual(1f, d.y, 0.001f);
        }
    }
}
