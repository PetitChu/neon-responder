using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace BrainlessLabs.Neon.SwarmRenderSpike
{
    /// <summary>
    /// Bounce-wander inside SpikeBounds. RequireForUpdate keeps this system idle
    /// in every scene that has no spike entities (systems live in the default
    /// world regardless of scene).
    /// </summary>
    [BurstCompile]
    public partial struct SpikeMoveSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpikeBounds>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var bounds = SystemAPI.GetSingleton<SpikeBounds>();
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (position, velocity) in
                     SystemAPI.Query<RefRW<SpikePosition>, RefRW<SpikeVelocity>>())
            {
                float2 p = position.ValueRO.Value + velocity.ValueRO.Value * deltaTime;
                float2 v = velocity.ValueRO.Value;

                if (p.x < bounds.Min.x || p.x > bounds.Max.x)
                {
                    v.x = -v.x;
                    p.x = math.clamp(p.x, bounds.Min.x, bounds.Max.x);
                }
                if (p.y < bounds.Min.y || p.y > bounds.Max.y)
                {
                    v.y = -v.y;
                    p.y = math.clamp(p.y, bounds.Min.y, bounds.Max.y);
                }

                position.ValueRW.Value = p;
                velocity.ValueRW.Value = v;
            }
        }
    }
}
