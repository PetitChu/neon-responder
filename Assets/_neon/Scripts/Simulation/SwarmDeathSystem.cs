using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace BrainlessLabs.Neon.Simulation
{
    /// <summary>Despawns dead chaff and records events for the bridge to drain.</summary>
    [BurstCompile]
    [UpdateAfter(typeof(FinishReadyEvalSystem))]
    public partial struct SwarmDeathSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SwarmWorldState>();
            state.RequireForUpdate<SwarmEventRecord>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var events = SystemAPI.GetSingletonBuffer<SwarmEventRecord>();
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (health, position, entity) in
                     SystemAPI.Query<RefRO<SwarmHealth>, RefRO<BeltPosition>>().WithEntityAccess())
            {
                if (health.ValueRO.Current > 0) continue;

                events.Add(new SwarmEventRecord
                {
                    Kind = SwarmEventRecord.KIND_CHAFF_DIED,
                    Position = position.ValueRO.Value
                });
                commandBuffer.DestroyEntity(entity);
            }

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }
    }
}
