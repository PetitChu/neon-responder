using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace BrainlessLabs.Neon.Simulation
{
    /// <summary>
    /// Chaff seek the player and crowd at a stop radius; ambient bounce-wander.
    /// M1 simplification (documented deviation): per-lane Y + jitter instead of
    /// true separation steering.
    /// </summary>
    [BurstCompile]
    [UpdateAfter(typeof(SwarmSpawnSystem))]
    public partial struct SwarmSteeringSystem : ISystem
    {
        private const float STOP_RADIUS = 0.9f;
        private const float STEER_LERP = 0.08f;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SwarmWorldState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var world = SystemAPI.GetSingleton<SwarmWorldState>();
            if (world.Enabled == 0) return;
            float deltaTime = SystemAPI.Time.DeltaTime;

            // Chaff: seek player, hold lane depth.
            foreach (var (position, velocity, entity) in
                     SystemAPI.Query<RefRW<BeltPosition>, RefRW<SwarmVelocity>>()
                         .WithAll<SwarmHealth>()
                         .WithEntityAccess())
            {
                float laneY = math.lerp(world.BeltMin.y, world.BeltMax.y,
                    (position.ValueRO.LaneIndex + 0.5f) / 3f);
                var target = new float2(world.PlayerPosition.x, laneY);
                float2 toTarget = target - position.ValueRO.Value;
                float distance = math.length(toTarget);

                // Deterministic per-entity jitter so the crowd doesn't stack on one point.
                float jitter = (entity.Index % 7 - 3) * 0.15f;
                float2 desired = distance > STOP_RADIUS + jitter
                    ? math.normalizesafe(toTarget) * world.ChaffMoveSpeed
                    : float2.zero;

                var newVelocity = math.lerp(velocity.ValueRO.Value, desired, STEER_LERP);
                var newPosition = math.clamp(position.ValueRO.Value + newVelocity * deltaTime,
                    world.BeltMin, world.BeltMax);

                velocity.ValueRW.Value = newVelocity;
                position.ValueRW.Value = newPosition;
            }

            // Ambient: bounce-wander (spike pattern).
            foreach (var (position, velocity) in
                     SystemAPI.Query<RefRW<BeltPosition>, RefRW<SwarmVelocity>>()
                         .WithNone<SwarmHealth>())
            {
                float2 p = position.ValueRO.Value + velocity.ValueRO.Value * deltaTime;
                float2 v = velocity.ValueRO.Value;

                if (p.x < world.BeltMin.x || p.x > world.BeltMax.x)
                {
                    v.x = -v.x;
                    p.x = math.clamp(p.x, world.BeltMin.x, world.BeltMax.x);
                }
                if (p.y < world.BeltMin.y || p.y > world.BeltMax.y)
                {
                    v.y = -v.y;
                    p.y = math.clamp(p.y, world.BeltMin.y, world.BeltMax.y);
                }

                position.ValueRW.Value = p;
                velocity.ValueRW.Value = v;
            }
        }
    }
}
