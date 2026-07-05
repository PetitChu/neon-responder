using System;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Turns the growth-layer derived stats into world effects (Level scope):
    /// PlayerMaxHealthPct → player maxHp (floor 50, doc §8.1) ·
    /// GrabDurationScale → player UnitSettings.grabDuration ·
    /// HealPerFinish → AddHealth on EnemyFinished (Vampiric Cadence) ·
    /// FinishAoeRadius → splash at the finish position (Concussive Finish).
    /// Bases for the derived values are captured ONCE per player spawn so
    /// re-application never compounds.
    /// </summary>
    public sealed class ProtocolEffectsSystem : IDisposable
    {
        private const int MAX_HP_FLOOR = 50; // protocol doc §8.1 hard cap

        private readonly IGameplaySignals _signals;
        private readonly IStatSystem _stats;
        private readonly IEntitiesService _entities;
        private readonly ISwarmBridge _bridge;
        private readonly GrowthConfig _config;
        private readonly IDisposable _acquiredSubscription;
        private readonly IDisposable _finishSubscription;

        private GameObject _player;
        private int _baseMaxHp;
        private float _baseGrabDuration;

        public ProtocolEffectsSystem(IGameplaySignals signals, IStatSystem stats,
            IEntitiesService entities, ISwarmBridge bridge, GrowthConfig config)
        {
            _signals = signals;
            _stats = stats;
            _entities = entities;
            _bridge = bridge;
            _config = config;

            _stats.Player.SetBase(StatId.PlayerMaxHealthPct, 1f);
            _stats.Player.SetBase(StatId.GrabDurationScale, 1f);
            _stats.Player.SetBase(StatId.FinishAoeRadius, 0f);
            _stats.Player.SetBase(StatId.HealPerFinish, 0f);

            _acquiredSubscription = _signals.On<ProtocolAcquired>().Subscribe(_ => ApplyDerivedToPlayer());
            _finishSubscription = _signals.On<EnemyFinished>().Subscribe(OnFinish);
            _entities.OnEntityRegistered += OnEntityRegistered;
        }

        public void Dispose()
        {
            _acquiredSubscription?.Dispose();
            _finishSubscription?.Dispose();
            _entities.OnEntityRegistered -= OnEntityRegistered;
        }

        private void OnEntityRegistered(TrackedEntity entity)
        {
            if (entity.UnitType != UNITTYPE.PLAYER || entity.GameObject == null) return;

            _player = entity.GameObject;
            var health = _player.GetComponent<HealthSystem>();
            _baseMaxHp = health != null ? health.maxHp : 0;
            var settings = _player.GetComponent<UnitSettings>();
            _baseGrabDuration = settings != null ? settings.grabDuration : 0f;

            ApplyDerivedToPlayer();
        }

        private void ApplyDerivedToPlayer()
        {
            if (_player == null) return;

            var health = _player.GetComponent<HealthSystem>();
            if (health != null && _baseMaxHp > 0)
            {
                float ratio = health.healthPercentage;
                int newMax = Mathf.Max(MAX_HP_FLOOR,
                    Mathf.RoundToInt(_baseMaxHp * _stats.Player.GetValue(StatId.PlayerMaxHealthPct)));
                if (newMax != health.maxHp)
                {
                    health.maxHp = newMax;
                    health.currentHp = Mathf.Clamp(Mathf.RoundToInt(newMax * ratio), 1, newMax);
                    health.AddHealth(0); // republish onHealthChange so bars refresh
                }
            }

            var settings = _player.GetComponent<UnitSettings>();
            if (settings != null && _baseGrabDuration > 0f)
            {
                settings.grabDuration = _baseGrabDuration * _stats.Player.GetValue(StatId.GrabDurationScale);
            }
        }

        private void OnFinish(EnemyFinished finished)
        {
            int heal = Mathf.RoundToInt(_stats.Player.GetValue(StatId.HealPerFinish));
            if (heal > 0 && _player != null)
            {
                _player.GetComponent<HealthSystem>()?.AddHealth(heal);
            }

            float radius = _stats.Player.GetValue(StatId.FinishAoeRadius);
            if (radius <= 0f) return;

            float damageMultiplier = _stats.Player.GetValue(StatId.DamageMultiplier);
            if (damageMultiplier <= 0f) damageMultiplier = 1f;
            int damage = Mathf.Max(1, Mathf.RoundToInt(_config.FinishAoeDamage * damageMultiplier));

            _bridge.ApplyAreaDamage(finished.Position, radius, damage);

            // Hero-tier splash (runtime path — SubstractHealth touches injected audio,
            // so EditMode coverage stops at the bridge call above).
            var enemies = _entities.GetByType(UNITTYPE.ENEMY);
            float radiusSq = radius * radius;
            for (int i = 0; i < enemies.Count; i++)
            {
                var go = enemies[i].GameObject;
                if (go == null) continue;
                var health = go.GetComponent<HealthSystem>();
                if (health == null || health.isDead) continue;
                if (((Vector2)go.transform.position - finished.Position).sqrMagnitude > radiusSq) continue;
                health.SubstractHealth(damage);
            }
        }
    }
}
