using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace BrainlessLabs.Neon.Simulation
{
    /// <summary>
    /// Floods chaff from both belt ends up to ChaffCap; seeds AmbientCap scattered
    /// vibe props once. Idle in scenes with no SwarmWorldState singleton.
    /// </summary>
    [BurstCompile]
    public partial struct SwarmSpawnSystem : ISystem
    {
        private const int LANE_COUNT = 3;

        private EntityArchetype _chaffArchetype;
        private EntityArchetype _ambientArchetype;
        private EntityQuery _chaffQuery;
        private EntityQuery _ambientQuery;
        private Random _random;
        private float _spawnAccumulator;
        private bool _spawnFromLeft;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SwarmWorldState>();

            _chaffArchetype = state.EntityManager.CreateArchetype(
                typeof(SwarmAgent), typeof(BeltPosition), typeof(SwarmVelocity),
                typeof(SwarmHealth), typeof(FinishReadyTag));
            _ambientArchetype = state.EntityManager.CreateArchetype(
                typeof(SwarmAgent), typeof(BeltPosition), typeof(SwarmVelocity));

            _chaffQuery = state.GetEntityQuery(ComponentType.ReadOnly<SwarmHealth>());
            _ambientQuery = new EntityQueryBuilder(Unity.Collections.Allocator.Temp)
                .WithAll<SwarmAgent, BeltPosition>()
                .WithNone<SwarmHealth>()
                .Build(ref state);

            _random = new Random(0x9E3779B9u);
        }

        public void OnUpdate(ref SystemState state)
        {
            var world = SystemAPI.GetSingleton<SwarmWorldState>();
            if (world.Enabled == 0) return;

            SeedAmbient(ref state, in world);
            FloodChaff(ref state, in world);
        }

        private void SeedAmbient(ref SystemState state, in SwarmWorldState world)
        {
            int missing = world.AmbientCap - _ambientQuery.CalculateEntityCount();
            for (int i = 0; i < missing; i++)
            {
                var entity = state.EntityManager.CreateEntity(_ambientArchetype);
                var position = _random.NextFloat2(world.BeltMin, world.BeltMax);
                state.EntityManager.SetComponentData(entity, new SwarmAgent { Tier = SwarmTier.Ambient });
                state.EntityManager.SetComponentData(entity, new BeltPosition { Value = position, LaneIndex = 0 });
                state.EntityManager.SetComponentData(entity, new SwarmVelocity
                {
                    Value = _random.NextFloat2Direction() * _random.NextFloat(0.2f, 0.8f)
                });
            }
        }

        private void FloodChaff(ref SystemState state, in SwarmWorldState world)
        {
            int chaffCount = _chaffQuery.CalculateEntityCount();
            _spawnAccumulator += world.SpawnRatePerSecond * SystemAPI.Time.DeltaTime;

            while (_spawnAccumulator >= 1f && chaffCount < world.ChaffCap)
            {
                _spawnAccumulator -= 1f;
                chaffCount++;

                int lane = _random.NextInt(0, LANE_COUNT);
                float laneY = math.lerp(world.BeltMin.y, world.BeltMax.y, (lane + 0.5f) / LANE_COUNT);
                float spawnX = _spawnFromLeft ? world.BeltMin.x : world.BeltMax.x;
                _spawnFromLeft = !_spawnFromLeft;

                var entity = state.EntityManager.CreateEntity(_chaffArchetype);
                state.EntityManager.SetComponentData(entity, new SwarmAgent { Tier = SwarmTier.Chaff });
                state.EntityManager.SetComponentData(entity, new BeltPosition { Value = new float2(spawnX, laneY), LaneIndex = lane });
                state.EntityManager.SetComponentData(entity, new SwarmVelocity { Value = float2.zero });
                state.EntityManager.SetComponentData(entity, new SwarmHealth { Current = world.ChaffMaxHealth, Max = world.ChaffMaxHealth });
                state.EntityManager.SetComponentEnabled<FinishReadyTag>(entity, false);
            }

            if (chaffCount >= world.ChaffCap) _spawnAccumulator = 0f;
        }
    }
}
