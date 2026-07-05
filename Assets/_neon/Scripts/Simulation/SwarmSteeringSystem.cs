using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace BrainlessLabs.Neon.Simulation
{
    /// <summary>
    /// Chaff steer as a crowd (seek player + separation + light cohesion via
    /// SwarmSteering) and hold at a stop radius; ambient bounce-wander.
    /// </summary>
    [BurstCompile]
    [UpdateAfter(typeof(SwarmSpawnSystem))]
    public partial struct SwarmSteeringSystem : ISystem
    {
        private const float STOP_RADIUS = 0.9f;
        private const float STEER_LERP = 0.08f;

        // Crowd tuning; mirror SwarmSteeringTests when changing.
        // SEP_WEIGHT 2: at weight 1 a pusher behind a stopped agent nets forward
        // force until spacing collapses to 0 (play-measured); 2 holds ~0.3u.
        private const float SEP_RADIUS = 0.6f;
        private const float SEP_WEIGHT = 2f;
        private const float COH_RADIUS = 1.5f;
        private const float COH_WEIGHT = 0.15f;

        // Golden angle: a unique drift direction per entity index (see loop comment).
        private const float TIEBREAK_ANGLE = 2.3999631f;
        private const float TIEBREAK_SPEED_FRACTION = 0.03f;

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

            // Chaff: crowd-steer toward the player. Gather all chaff positions once
            // (chaff = has SwarmHealth) for the O(n) neighbor scan per agent.
            var crowdQuery = SystemAPI.QueryBuilder()
                .WithAll<BeltPosition, SwarmHealth>().Build();
            using var crowdPos = crowdQuery.ToComponentDataArray<BeltPosition>(Allocator.Temp);
            var crowd = new NativeArray<float2>(crowdPos.Length, Allocator.Temp);
            for (int i = 0; i < crowdPos.Length; i++) crowd[i] = crowdPos[i].Value;

            var playerTarget = new float2(world.PlayerPosition.x, world.PlayerPosition.y);

            foreach (var (position, velocity, entity) in
                     SystemAPI.Query<RefRW<BeltPosition>, RefRW<SwarmVelocity>>()
                         .WithAll<SwarmHealth>()
                         .WithEntityAccess())
            {
                float2 desired = SwarmSteering.ComputeDesiredVelocity(
                    position.ValueRO.Value, playerTarget, crowd,
                    world.ChaffMoveSpeed, SEP_RADIUS, SEP_WEIGHT, COH_RADIUS, COH_WEIGHT, STOP_RADIUS);

                // Per-entity symmetry breaker: coincident agents skip each other in the
                // separation scan (self-epsilon) and would otherwise receive identical
                // steering forever - a permanent stack. A tiny unique drift (active even
                // when seek holds at the stop radius) splits them so separation can act.
                float driftAngle = entity.Index * TIEBREAK_ANGLE;
                desired += new float2(math.cos(driftAngle), math.sin(driftAngle))
                           * (TIEBREAK_SPEED_FRACTION * world.ChaffMoveSpeed);

                var newVelocity = math.lerp(velocity.ValueRO.Value, desired, STEER_LERP);
                var newPosition = math.clamp(position.ValueRO.Value + newVelocity * deltaTime,
                    world.BeltMin, world.BeltMax);

                velocity.ValueRW.Value = newVelocity;
                position.ValueRW.Value = newPosition;
            }
            crowd.Dispose();

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
