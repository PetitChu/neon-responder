using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace BrainlessLabs.Neon.Simulation
{
    /// <summary>Enables FinishReadyTag on chaff at or under the health threshold (spec §5.1).</summary>
    [BurstCompile]
    [UpdateAfter(typeof(SwarmDamageSystem))]
    public partial struct FinishReadyEvalSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SwarmWorldState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var world = SystemAPI.GetSingleton<SwarmWorldState>();

            foreach (var (health, finishReady) in
                     SystemAPI.Query<RefRO<SwarmHealth>, EnabledRefRW<FinishReadyTag>>()
                         .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
            {
                int threshold = (int)math.ceil(health.ValueRO.Max * world.FinishReadyThreshold);
                finishReady.ValueRW = health.ValueRO.Current > 0 && health.ValueRO.Current <= threshold;
            }
        }
    }
}
