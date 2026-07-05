using System;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Marks hero-tier enemies Finish-Ready at ≤ threshold HP or while knocked
    /// down (spec §5.1: "≤25% HP (or staggered)"). Chaff-side marking lives in
    /// the sim's FinishReadyEvalSystem.
    /// </summary>
    public sealed class FinishReadySystem : IGameplayTickable, IDisposable
    {
        private const int TICK_ORDER = 10; // reserved band (IGameplayClock doc)

        private readonly IGameplayClock _clock;
        private readonly IEntitiesService _entities;
        private readonly EngagementConfig _config;

        public FinishReadySystem(IGameplayClock clock, IEntitiesService entities, EngagementConfig config)
        {
            _clock = clock;
            _entities = entities;
            _config = config;
            _clock.Register(this, TICK_ORDER);
        }

        public void Tick(float deltaTime)
        {
            var enemies = _entities.GetByType(UNITTYPE.ENEMY);
            for (int i = 0; i < enemies.Count; i++)
            {
                var go = enemies[i].GameObject;
                if (go == null) continue;

                var health = go.GetComponent<HealthSystem>();
                if (health == null) continue;

                var marker = go.GetComponent<FinishReadyMarker>();
                if (marker == null) marker = go.AddComponent<FinishReadyMarker>();

                var state = go.GetComponent<UnitStateMachine>()?.GetCurrentState();
                bool staggered = state is UnitKnockDown || state is UnitKnockDownGrounded;
                bool ready = !health.isDead
                    && (health.healthPercentage <= _config.FinishReadyHealthThreshold || staggered);
                marker.SetReady(ready, _config.FinishReadyGlow);
            }
        }

        public void Dispose()
        {
            _clock.Unregister(this);
        }
    }
}
