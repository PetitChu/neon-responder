using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// The observe-and-tag seam (spec §5.1/§5.3): watches the existing verbs through
    /// the static combat events; verbs stay untouched. M2 hero finish rule (P2): a
    /// Finish-Ready hero demands a landed verb SEQUENCE (2 inputs; 3 at Hot+, windows
    /// tightening per tier). Completing = instant finish + Momentum; dying mid-sequence
    /// = plain kill, no payout (v0.4). Chaff stay single-verb inside
    /// SwarmBridge.ApplyVerbHit. Whiffs publish VerbWhiffed + stagger the player.
    /// Stagger and the finish execution-kill are applied on the NEXT clock tick —
    /// both events fire inside state transitions / hit resolution, where a reentrant
    /// SetState / SubstractHealth would race the code that raised them.
    /// </summary>
    public sealed class FinishResolver : IGameplayTickable, IDisposable
    {
        private const int TICK_ORDER = 25; // after the selector (20), before Momentum decay (30)
        private const float MIN_INPUT_WINDOW = 0.3f;

        private readonly IGameplayClock _clock;
        private readonly IGameplaySignals _signals;
        private readonly IMomentumSystem _momentum;
        private readonly EngagementConfig _config;
        private readonly GrowthConfig _growth;
        private readonly Dictionary<GameObject, SequenceFinishChallenge> _challenges = new();
        private readonly List<GameObject> _pruneScratch = new();
        private UnitActions _pendingStagger;
        private GameObject _pendingFinishKill;

        public FinishResolver(IGameplayClock clock, IGameplaySignals signals,
            IMomentumSystem momentum, EngagementConfig config, GrowthConfig growth)
        {
            _clock = clock;
            _signals = signals;
            _momentum = momentum;
            _config = config;
            _growth = growth;
            UnitActions.onUnitDealDamage += HandleDamage;
            UnitActions.onVerbWhiffed += HandleWhiff;
            _clock.Register(this, TICK_ORDER);
        }

        public void Dispose()
        {
            UnitActions.onUnitDealDamage -= HandleDamage;
            UnitActions.onVerbWhiffed -= HandleWhiff;
            _clock.Unregister(this);
            _challenges.Clear();
        }

        public void Tick(float deltaTime)
        {
            if (_pendingStagger != null)
            {
                var stateMachine = _pendingStagger.UnitStateMachine;
                _pendingStagger = null;
                if (stateMachine != null) stateMachine.SetState(new PlayerWhiffStagger(_config.WhiffStaggerSeconds));
            }

            if (_pendingFinishKill != null)
            {
                var health = _pendingFinishKill.GetComponent<HealthSystem>();
                _pendingFinishKill = null;
                if (health != null && !health.isDead) health.SubstractHealth(health.currentHp);
            }

            PruneChallenges();
        }

        /// <summary>Public so EditMode tests can drive it (static events can't be raised externally).</summary>
        public void HandleDamage(GameObject recipient, AttackData attackData)
        {
            if (recipient == null || attackData?.inflictor == null) return;
            if (!attackData.inflictor.CompareTag("Player")) return;

            var marker = recipient.GetComponent<FinishReadyMarker>();
            if (marker == null || !marker.IsReady)
            {
                DropChallenge(recipient);
                return;
            }

            var sequence = _momentum.Tier >= MomentumTier.Hot
                ? _growth.ChallengeSequenceHot
                : _growth.ChallengeSequenceBase;
            float window = Mathf.Max(MIN_INPUT_WINDOW,
                _growth.ChallengeInputWindowSeconds - (int)_momentum.Tier * _growth.ChallengeWindowTightenPerTier);

            if (!_challenges.TryGetValue(recipient, out var challenge) || challenge.Total != sequence.Length)
            {
                challenge = new SequenceFinishChallenge(sequence, window, _clock.GameplayTime);
                _challenges[recipient] = challenge;
            }

            bool completed = challenge.TryAdvance(attackData.attackType, _clock.GameplayTime);
            var health = recipient.GetComponent<HealthSystem>();

            if (completed)
            {
                DropChallenge(recipient);
                _signals.Publish(new EnemyFinished(recipient.transform.position, wasChaff: false));
                // Execute the finish: the completed challenge kills outright (unless
                // this hit already did). Deferred to Tick — we're inside CheckForHit.
                if (health != null && !health.isDead) _pendingFinishKill = recipient;
                return;
            }

            if (health != null && health.isDead)
            {
                // Died mid-sequence: a plain kill, no Momentum (v0.4 pays on completion).
                DropChallenge(recipient);
                return;
            }

            _signals.Publish(new FinishChallengeChanged(true, recipient.transform.position,
                challenge.ExpectedVerb, challenge.Progress, challenge.Total));
        }

        /// <summary>Public so EditMode tests can drive it.</summary>
        public void HandleWhiff(UnitActions unit, ATTACKTYPE attackType)
        {
            if (unit == null || !unit.isPlayer) return;

            _signals.Publish(new VerbWhiffed(attackType));
            _pendingStagger = unit;
        }

        private void DropChallenge(GameObject target)
        {
            if (_challenges.Remove(target))
            {
                _signals.Publish(new FinishChallengeChanged(false, Vector2.zero, ATTACKTYPE.NONE, 0, 0));
            }
        }

        private void PruneChallenges()
        {
            if (_challenges.Count == 0) return;

            _pruneScratch.Clear();
            foreach (var pair in _challenges)
            {
                var target = pair.Key;
                if (target == null
                    || target.GetComponent<HealthSystem>()?.isDead == true
                    || target.GetComponent<FinishReadyMarker>()?.IsReady != true)
                {
                    _pruneScratch.Add(target);
                }
            }
            foreach (var stale in _pruneScratch) DropChallenge(stale);
        }
    }
}
