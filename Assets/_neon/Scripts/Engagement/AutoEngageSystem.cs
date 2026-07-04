using System;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// The automated basic attack (spec §5.1) — a NEW attack; manual verbs untouched.
    /// Every rhythm interval, chips the nearest hot enemy in the player's facing arc:
    /// chaff via the bridge, hero-tier via HealthSystem.SubstractHealth (no hit-reaction
    /// state change — chip is pressure toward Finish-Ready; the verbs are the finish).
    /// All knobs read from the stat sheet each tick, so upgrades apply live.
    /// </summary>
    public sealed class AutoEngageSystem : IGameplayTickable, IDisposable
    {
        private const int TICK_ORDER = 0;      // reserved band (IGameplayClock doc)
        private const float RATE_HARD_CAP = 6f;    // protocol-doc guardrail: auto-rate ≤ 6/s
        private const float ARC_HARD_CAP = 180f;   // protocol-doc guardrail: arc ≤ 180°

        private readonly IGameplayClock _clock;
        private readonly IStatSystem _stats;
        private readonly IEntitiesService _entities;
        private readonly ISwarmBridge _bridge;
        private float _accumulator;

        public AutoEngageSystem(IGameplayClock clock, IStatSystem stats, IEntitiesService entities,
            ISwarmBridge bridge, EngagementConfig config)
        {
            _clock = clock;
            _stats = stats;
            _entities = entities;
            _bridge = bridge;

            // Seed the tunable bases (idempotent — Protocols modify via modifiers, not bases).
            _stats.Player.SetBase(StatId.AutoEngageRate, config.AutoEngageRatePerSecond);
            _stats.Player.SetBase(StatId.AutoEngageDamage, config.AutoEngageChipDamage);
            _stats.Player.SetBase(StatId.AutoEngageArcDegrees, config.AutoEngageArcDegrees);
            _stats.Player.SetBase(StatId.AutoEngageRange, config.AutoEngageRange);

            _clock.Register(this, TICK_ORDER);
        }

        public void Tick(float deltaTime)
        {
            // Accumulate chips-owed (deltaTime × rate) rather than comparing against
            // 1/rate — dividing makes the interval land just above exact fractions
            // (1f/6f > 1/6), silently dropping the last chip of a window.
            float rate = Mathf.Clamp(_stats.Player.GetValue(StatId.AutoEngageRate), 0.01f, RATE_HARD_CAP);
            _accumulator += deltaTime * rate;

            while (_accumulator >= 1f)
            {
                _accumulator -= 1f;
                FireChip();
            }
        }

        public void Dispose()
        {
            _clock.Unregister(this);
        }

        private void FireChip()
        {
            var player = _entities.GetFirstByType(UNITTYPE.PLAYER).GameObject;
            if (player == null) return;

            Vector2 origin = player.transform.position;
            var actions = player.GetComponent<UnitActions>();
            float facingSign = actions != null ? (int)actions.dir : 1f;
            float arc = Mathf.Min(_stats.Player.GetValue(StatId.AutoEngageArcDegrees), ARC_HARD_CAP);
            float range = _stats.Player.GetValue(StatId.AutoEngageRange);

            float damageMultiplier = _stats.Player.GetValue(StatId.DamageMultiplier);
            if (damageMultiplier <= 0f) damageMultiplier = 1f;
            int damage = Mathf.Max(1, Mathf.RoundToInt(_stats.Player.GetValue(StatId.AutoEngageDamage) * damageMultiplier));

            // Nearest hot target across BOTH worlds — bridge answers for chaff,
            // EntitiesQueries for hero-tier; nearest wins. The spine never knows which world.
            bool hasChaff = _bridge.TryGetNearestHot(origin, facingSign, arc, range, out var chaffTarget);
            var heroTarget = _entities.GetNearestEnemyInArc(origin, facingSign, arc, range);

            float chaffSqrDistance = hasChaff ? (chaffTarget.Position - origin).sqrMagnitude : float.MaxValue;
            float heroSqrDistance = heroTarget != null
                ? ((Vector2)heroTarget.transform.position - origin).sqrMagnitude
                : float.MaxValue;

            if (!hasChaff && heroTarget == null) return;

            if (chaffSqrDistance <= heroSqrDistance)
            {
                _bridge.ApplyChip(in chaffTarget, damage);
            }
            else
            {
                heroTarget.GetComponent<HealthSystem>()?.SubstractHealth(damage);
            }
        }
    }
}
