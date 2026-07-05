using System;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// The observe-and-tag seam (spec §5.1): watches the existing verbs through the
    /// static combat events. M1 hero-tier finish rule: a player verb hit that KILLS a
    /// Finish-Ready enemy (chaff finishes resolve inside SwarmBridge.ApplyVerbHit).
    /// Whiffs publish VerbWhiffed (Momentum resets on it) and stagger the player.
    /// The stagger is applied on the NEXT clock tick — onVerbWhiffed fires inside a
    /// state transition (PlayerAttack.Exit runs during SetState), and a reentrant
    /// SetState there would race the outgoing transition.
    /// </summary>
    public sealed class FinishResolver : IGameplayTickable, IDisposable
    {
        private const int TICK_ORDER = 25; // after the selector (20), before Momentum decay (30)

        private readonly IGameplayClock _clock;
        private readonly IGameplaySignals _signals;
        private readonly EngagementConfig _config;
        private UnitActions _pendingStagger;

        public FinishResolver(IGameplayClock clock, IGameplaySignals signals, EngagementConfig config)
        {
            _clock = clock;
            _signals = signals;
            _config = config;
            UnitActions.onUnitDealDamage += HandleDamage;
            UnitActions.onVerbWhiffed += HandleWhiff;
            _clock.Register(this, TICK_ORDER);
        }

        public void Dispose()
        {
            UnitActions.onUnitDealDamage -= HandleDamage;
            UnitActions.onVerbWhiffed -= HandleWhiff;
            _clock.Unregister(this);
        }

        public void Tick(float deltaTime)
        {
            if (_pendingStagger == null) return;

            var stateMachine = _pendingStagger.UnitStateMachine;
            _pendingStagger = null;
            if (stateMachine != null) stateMachine.SetState(new PlayerWhiffStagger(_config.WhiffStaggerSeconds));
        }

        /// <summary>Public so EditMode tests can drive it (static events can't be raised externally).</summary>
        public void HandleDamage(GameObject recipient, AttackData attackData)
        {
            if (recipient == null || attackData?.inflictor == null) return;
            if (!attackData.inflictor.CompareTag("Player")) return;

            var marker = recipient.GetComponent<FinishReadyMarker>();
            if (marker == null || !marker.IsReady) return;

            var health = recipient.GetComponent<HealthSystem>();
            if (health == null || !health.isDead) return; // the finishing hit is the killing hit (M1)

            _signals.Publish(new EnemyFinished(recipient.transform.position, wasChaff: false));
        }

        /// <summary>Public so EditMode tests can drive it.</summary>
        public void HandleWhiff(UnitActions unit, ATTACKTYPE attackType)
        {
            if (unit == null || !unit.isPlayer) return;

            _signals.Publish(new VerbWhiffed(attackType));
            _pendingStagger = unit;
        }
    }
}
