using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BrainlessLabs.Neon.Simulation;

namespace BrainlessLabs.Neon.Tests
{
    public class SwarmSteeringTests
    {
        // radius/weight knobs mirror the system consts (SwarmSteeringSystem).
        const float MoveSpeed = 2f, SepR = 0.6f, SepW = 1f, CohR = 1.5f, CohW = 0.15f, StopR = 0.9f;

        static float2 Compute(float2 self, float2 target, float2[] crowd)
        {
            var arr = new NativeArray<float2>(crowd, Allocator.Temp);
            try { return SwarmSteering.ComputeDesiredVelocity(self, target, arr, MoveSpeed, SepR, SepW, CohR, CohW, StopR); }
            finally { arr.Dispose(); }
        }

        [Test]
        public void LoneAgent_far_from_target_moves_toward_it_at_move_speed()
        {
            var v = Compute(new float2(0, 0), new float2(10, 0), new[] { new float2(0, 0) });
            Assert.Greater(v.x, 0f);
            Assert.AreEqual(MoveSpeed, math.length(v), 0.01f);
        }

        [Test]
        public void LoneAgent_within_stop_radius_holds()
        {
            var v = Compute(new float2(0, 0), new float2(0.5f, 0), new[] { new float2(0, 0) });
            Assert.Less(math.length(v), 0.01f);
        }

        [Test]
        public void Close_neighbor_pushes_agent_away()
        {
            // neighbor to the right, target straight up → separation must add a leftward push
            var v = Compute(new float2(0, 0), new float2(0, 10), new[] { new float2(0, 0), new float2(0.2f, 0) });
            Assert.Less(v.x, 0f);
        }

        [Test]
        public void Cohesion_pulls_toward_a_cluster_outside_separation_range()
        {
            // neighbors at ~1.2 (outside SepR 0.6, inside CohR 1.5), target within stop radius → only cohesion acts
            var v = Compute(new float2(0, 0), new float2(0.1f, 0),
                new[] { new float2(0, 0), new float2(1.2f, 0), new float2(1.2f, 0.2f) });
            Assert.Greater(v.x, 0f);
        }
    }
}
