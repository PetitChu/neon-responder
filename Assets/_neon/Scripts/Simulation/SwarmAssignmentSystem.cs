using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace BrainlessLabs.Neon.Simulation
{
    /// <summary>
    /// Re-adopts orphan chaff (HeroId not in the live hero table) into the nearest hero with a
    /// free slot. Runs before SwarmSpawnSystem so orphans fill slots before fresh chaff spawn
    /// ("adopt before spawn"). Also demotes followers whose hero has died to orphan (-1).
    /// </summary>
    [BurstCompile]
    [UpdateBefore(typeof(SwarmSpawnSystem))]
    public partial struct SwarmAssignmentSystem : ISystem
    {
        public void OnCreate(ref SystemState state) => state.RequireForUpdate<SwarmWorldState>();

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var world = SystemAPI.GetSingleton<SwarmWorldState>();
            if (world.Enabled == 0) return;
            if (!SystemAPI.TryGetSingletonEntity<SwarmWorldState>(out var control)) return;

            // Stable snapshot so we never read the buffer while mutating FollowerState.
            var heroesBuf = state.EntityManager.GetBuffer<HeroSlot>(control);
            using var heroes = new NativeArray<HeroSlot>(heroesBuf.AsNativeArray(), Allocator.Temp);
            var counts = new NativeArray<int>(heroes.Length, Allocator.Temp);

            // 1) tally current valid followers; demote dead-hero followers to orphan.
            foreach (var follower in SystemAPI.Query<RefRW<FollowerState>>().WithAll<SwarmHealth>())
            {
                int idx = HeroAssignment.IndexOfHero(follower.ValueRO.HeroId, heroes);
                if (idx >= 0) counts[idx]++;
                else follower.ValueRW.HeroId = FollowerState.Orphan;
            }

            // 2) re-adopt orphans into the nearest hero with a free slot.
            foreach (var (follower, position) in
                     SystemAPI.Query<RefRW<FollowerState>, RefRO<BeltPosition>>().WithAll<SwarmHealth>())
            {
                if (follower.ValueRO.HeroId != FollowerState.Orphan) continue;
                int idx = HeroAssignment.PickNearestOpenHero(position.ValueRO.Value, heroes, counts);
                if (idx < 0) continue; // still orphan → seeks player (steering)
                follower.ValueRW.HeroId = heroes[idx].Id;
                counts[idx]++;
            }
            counts.Dispose();
        }
    }
}
