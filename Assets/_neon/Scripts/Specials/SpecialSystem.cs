using System;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    public sealed class SpecialSystem : ISpecialSystem, IGameplayTickable, IDisposable
    {
        private const int TICK_ORDER = 45; // after RunService (40)
        private const float KNOCKDOWN_FORCE_X = 2f;
        private const float KNOCKDOWN_FORCE_Y = 2f;

        private readonly IGameplayClock _clock;
        private readonly IGameplaySignals _signals;
        private readonly IEntitiesService _entities;
        private readonly ISwarmBridge _bridge;
        private readonly IInputService _input;
        private readonly IEconomySystem _economy;
        private readonly SpecialConfig _config;

        private float _cooldownRemaining;

        // Cooldown-only: charge affordability is TryActivate's gate (TrySpend). Keeping
        // it out of Ready keeps SpecialStateChanged consistent — charge changes don't
        // re-publish state, so an affordability-gated Ready would go stale on the HUD.
        public bool CanActivate => _cooldownRemaining <= 0f;
        public float CooldownNormalized =>
            _config.SirenCooldownSeconds <= 0f ? 1f : 1f - Mathf.Clamp01(_cooldownRemaining / _config.SirenCooldownSeconds);

        public SpecialSystem(IGameplayClock clock, IGameplaySignals signals, IEntitiesService entities,
            ISwarmBridge bridge, IInputService input, IEconomySystem economy, SpecialConfig config)
        {
            _clock = clock;
            _signals = signals;
            _entities = entities;
            _bridge = bridge;
            _input = input;
            _economy = economy;
            _config = config;
            _clock.Register(this, TICK_ORDER);
        }

        public void Dispose() => _clock.Unregister(this);

        public void Tick(float deltaTime)
        {
            bool wasReady = _cooldownRemaining <= 0f;
            if (_cooldownRemaining > 0f)
            {
                _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - deltaTime);
                if (_cooldownRemaining <= 0f) PublishState(); // recovered
            }

            if (_input.SpecialKeyDown(1)) TryActivate();

            // Keep the meter live for the HUD while cooling (cheap; only when relevant).
            if (!wasReady && _cooldownRemaining > 0f) PublishState();
        }

        private void TryActivate()
        {
            if (_cooldownRemaining > 0f) return;
            if (!_economy.TrySpend(_config.SirenChargeCost)) return;

            var player = _entities.GetFirstByType(UNITTYPE.PLAYER).GameObject;
            Vector2 origin = player != null ? (Vector2)player.transform.position : Vector2.zero;

            // Chaff → mass Finish-Ready (bridge). Hero-tier → knockdown (→ Finish-Ready
            // via the staggered path FinishReadySystem already reads). No verb change.
            _bridge.MassFinishReady(origin, _config.SirenRadius);
            KnockdownHeroesInRadius(origin, _config.SirenRadius);

            _cooldownRemaining = _config.SirenCooldownSeconds;
            _signals.Publish(new Callout("SIREN PULSE", origin));
            PublishState();
        }

        private void KnockdownHeroesInRadius(Vector2 center, float radius)
        {
            float radiusSq = radius * radius;
            var enemies = _entities.GetByType(UNITTYPE.ENEMY);
            for (int i = 0; i < enemies.Count; i++)
            {
                var go = enemies[i].GameObject;
                if (go == null) continue;
                if (((Vector2)go.transform.position - center).sqrMagnitude > radiusSq) continue;

                var settings = go.GetComponent<UnitSettings>();
                if (settings == null || !settings.canBeKnockedDown) continue;
                var stateMachine = go.GetComponent<UnitStateMachine>();
                if (stateMachine == null) continue;
                if (stateMachine.GetCurrentState() is UnitKnockDown) continue;

                var pulse = new AttackData("SirenPulse", 0, go, ATTACKTYPE.NONE, knockdown: true);
                stateMachine.SetState(new UnitKnockDown(pulse, KNOCKDOWN_FORCE_X, KNOCKDOWN_FORCE_Y));
            }
        }

        private void PublishState() => _signals.Publish(new SpecialStateChanged(CanActivate, CooldownNormalized));
    }
}
