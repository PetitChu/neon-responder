using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using BrainlessLabs.Neon.Simulation;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Level-scoped bridge: pushes Mono world state into the sim, queries chaff,
    /// queues damage/kill commands, drains sim events. Ticks at order -10 so the
    /// engagement systems (0/10/20/30) always see a fresh sim view.
    /// </summary>
    public sealed class SwarmBridge : ISwarmBridge, IGameplayTickable, IDisposable
    {
        private const int TICK_ORDER = -10;
        private const float VERB_HIT_PADDING = 0.3f; // chaff have no colliders; pad the hitbox by an agent radius

        private readonly IGameplayClock _clock;
        private readonly IGameplaySignals _signals;
        private readonly IEntitiesService _entities;
        private readonly SwarmConfig _config;

        private World _world;
        private Entity _controlEntity;
        private EntityQuery _chaffQuery;
        private EntityQuery _readyQuery;
        private EntityQuery _agentQuery;
        private bool _initialized;
        private bool _capLogged;

        public SwarmBridge(IGameplayClock clock, IGameplaySignals signals, IEntitiesService entities, SwarmConfig config)
        {
            _clock = clock;
            _signals = signals;
            _entities = entities;
            _config = config;
            _clock.Register(this, TICK_ORDER);
        }

        public void Tick(float deltaTime)
        {
            if (!_config.Enabled || !TryInitialize()) return;

            var entityManager = _world.EntityManager;
            var state = entityManager.GetComponentData<SwarmWorldState>(_controlEntity);

            var player = _entities.GetFirstByType(UNITTYPE.PLAYER).GameObject;
            if (player != null)
            {
                state.PlayerPosition = new float2(player.transform.position.x, player.transform.position.y);
                var actions = player.GetComponent<UnitActions>();
                if (actions != null) state.PlayerFacingSign = (int)actions.dir;
            }
            entityManager.SetComponentData(_controlEntity, state);

            // Drain sim events → kill XP hangs off ChaffDied. A finish ALSO emits its
            // death here (finish rewards flow separately off EnemyFinished — Charge/
            // Overcharge; kills — XP; no double-granting because the reward types differ).
            var events = entityManager.GetBuffer<SwarmEventRecord>(_controlEntity);
            for (int i = 0; i < events.Length; i++)
            {
                if (events[i].Kind == SwarmEventRecord.KIND_CHAFF_DIED)
                {
                    _signals.Publish(new ChaffDied(new Vector2(events[i].Position.x, events[i].Position.y)));
                }
            }
            events.Clear();

            if (!_capLogged && CountHot() >= _config.ChaffCap)
            {
                _capLogged = true;
                Debug.Log($"[Swarm] Chaff cap reached ({_config.ChaffCap}).");
            }
        }

        public void Dispose()
        {
            _clock.Unregister(this);
            if (!_initialized || _world == null || !_world.IsCreated) return;

            var entityManager = _world.EntityManager;
            using var allAgents = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SwarmAgent>());
            entityManager.DestroyEntity(allAgents);
            if (entityManager.Exists(_controlEntity)) entityManager.DestroyEntity(_controlEntity);
        }

        public bool TryGetNearestHot(Vector2 origin, float facingSign, float arcDegrees, float range, out TargetRef target)
        {
            target = default;
            if (!_initialized) return false;

            using var entities = _chaffQuery.ToEntityArray(Allocator.Temp);
            using var positions = _chaffQuery.ToComponentDataArray<BeltPosition>(Allocator.Temp);

            float cosHalfArc = math.cos(math.radians(math.min(arcDegrees, 360f) * 0.5f));
            var facing = new float2(math.sign(facingSign == 0f ? 1f : facingSign), 0f);
            var originF = new float2(origin.x, origin.y);
            float bestDistanceSq = range * range;
            int bestIndex = -1;

            for (int i = 0; i < entities.Length; i++)
            {
                float2 toAgent = positions[i].Value - originF;
                float distanceSq = math.lengthsq(toAgent);
                if (distanceSq > bestDistanceSq || distanceSq < 1e-6f) continue;
                if (arcDegrees < 360f && math.dot(math.normalize(toAgent), facing) < cosHalfArc) continue;

                bestDistanceSq = distanceSq;
                bestIndex = i;
            }

            if (bestIndex < 0) return false;
            var p = positions[bestIndex].Value;
            target = new TargetRef(entities[bestIndex], new Vector2(p.x, p.y));
            return true;
        }

        public bool TryGetNearestFinishReady(Vector2 origin, out TargetRef target)
        {
            target = default;
            if (!_initialized) return false;

            using var entities = _readyQuery.ToEntityArray(Allocator.Temp);
            using var positions = _readyQuery.ToComponentDataArray<BeltPosition>(Allocator.Temp);

            var originF = new float2(origin.x, origin.y);
            float bestDistanceSq = float.MaxValue;
            int bestIndex = -1;

            for (int i = 0; i < entities.Length; i++)
            {
                float distanceSq = math.lengthsq(positions[i].Value - originF);
                if (distanceSq >= bestDistanceSq) continue;
                bestDistanceSq = distanceSq;
                bestIndex = i;
            }

            if (bestIndex < 0) return false;
            var p = positions[bestIndex].Value;
            target = new TargetRef(entities[bestIndex], new Vector2(p.x, p.y));
            return true;
        }

        public int CountHot() => _initialized ? _chaffQuery.CalculateEntityCount() : 0;
        public int CountFinishReady() => _initialized ? _readyQuery.CalculateEntityCount() : 0;

        public void ApplyChip(in TargetRef target, int damage)
        {
            if (!_initialized || !target.IsChaff) return;
            _world.EntityManager.GetBuffer<SwarmDamageCommand>(_controlEntity)
                .Add(new SwarmDamageCommand { Target = target.Entity, Amount = damage, IsChip = 1 });
        }

        public bool ApplyVerbHit(Bounds hitBounds, AttackData attackData)
        {
            if (!_initialized || attackData == null) return false;

            var entityManager = _world.EntityManager;
            using var entities = _chaffQuery.ToEntityArray(Allocator.Temp);
            using var positions = _chaffQuery.ToComponentDataArray<BeltPosition>(Allocator.Temp);

            var min = new float2(hitBounds.min.x - VERB_HIT_PADDING, hitBounds.min.y - VERB_HIT_PADDING);
            var max = new float2(hitBounds.max.x + VERB_HIT_PADDING, hitBounds.max.y + VERB_HIT_PADDING);
            var damageBuffer = entityManager.GetBuffer<SwarmDamageCommand>(_controlEntity);
            var killBuffer = entityManager.GetBuffer<SwarmKillCommand>(_controlEntity);
            bool hitAny = false;

            for (int i = 0; i < entities.Length; i++)
            {
                var p = positions[i].Value;
                if (p.x < min.x || p.x > max.x || p.y < min.y || p.y > max.y) continue;

                hitAny = true;
                if (entityManager.IsComponentEnabled<FinishReadyTag>(entities[i]))
                {
                    // Single-verb chaff finish (spec §7 M1): any verb connect = finish.
                    killBuffer.Add(new SwarmKillCommand { Target = entities[i] });
                    _signals.Publish(new EnemyFinished(new Vector2(p.x, p.y), wasChaff: true));
                }
                else
                {
                    damageBuffer.Add(new SwarmDamageCommand { Target = entities[i], Amount = attackData.damage, IsChip = 0 });
                }
            }

            return hitAny;
        }

        public void ApplyAreaDamage(Vector2 center, float radius, int damage)
        {
            if (!_initialized || radius <= 0f) return;

            using var entities = _chaffQuery.ToEntityArray(Allocator.Temp);
            using var positions = _chaffQuery.ToComponentDataArray<BeltPosition>(Allocator.Temp);

            var centerF = new float2(center.x, center.y);
            float radiusSq = radius * radius;
            var damageBuffer = _world.EntityManager.GetBuffer<SwarmDamageCommand>(_controlEntity);

            for (int i = 0; i < entities.Length; i++)
            {
                if (math.lengthsq(positions[i].Value - centerF) > radiusSq) continue;
                damageBuffer.Add(new SwarmDamageCommand { Target = entities[i], Amount = damage, IsChip = 0 });
            }
        }

        private bool TryInitialize()
        {
            if (_initialized) return _world != null && _world.IsCreated;

            _world = World.DefaultGameObjectInjectionWorld;
            if (_world == null || !_world.IsCreated) return false;

            var entityManager = _world.EntityManager;
            _controlEntity = entityManager.CreateEntity(
                typeof(SwarmWorldState), typeof(SwarmDamageCommand),
                typeof(SwarmKillCommand), typeof(SwarmEventRecord));
            entityManager.SetComponentData(_controlEntity, new SwarmWorldState
            {
                PlayerFacingSign = 1f,
                ChaffCap = _config.ChaffCap,
                AmbientCap = _config.AmbientCap,
                SpawnRatePerSecond = _config.SpawnRatePerSecond,
                ChaffMaxHealth = _config.ChaffMaxHealth,
                ChaffMoveSpeed = _config.ChaffMoveSpeed,
                BeltMin = new float2(_config.BeltMin.x, _config.BeltMin.y),
                BeltMax = new float2(_config.BeltMax.x, _config.BeltMax.y),
                FinishReadyThreshold = _config.FinishReadyThreshold,
                Enabled = 1
            });

            _chaffQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<BeltPosition>(), ComponentType.ReadOnly<SwarmHealth>());
            _readyQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<BeltPosition>(), ComponentType.ReadOnly<FinishReadyTag>());

            _initialized = true;
            Debug.Log($"[Swarm] Bridge online: chaff cap {_config.ChaffCap}, ambient cap {_config.AmbientCap}.");
            return true;
        }
    }
}
