using System;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    public sealed class MomentumSystem : IMomentumSystem, IGameplayTickable, IDisposable
    {
        private const int TICK_ORDER = 30; // reserved band: Momentum decay (IGameplayClock doc)

        private readonly IGameplayClock _clock;
        private readonly IGameplaySignals _signals;
        private readonly IStatSystem _stats;
        private readonly MomentumConfig _config;
        private readonly ModifierSource _source = ModifierSource.Create("momentum");
        private readonly IDisposable _finishSubscription;
        private readonly IDisposable _whiffSubscription;
        private readonly IDisposable _protocolSubscription;

        private int _steps;
        private float _idleSeconds;

        public MomentumTier Tier { get; private set; } = MomentumTier.Cool;

        public MomentumSystem(IGameplayClock clock, IGameplaySignals signals, IStatSystem stats, MomentumConfig config)
        {
            _clock = clock;
            _signals = signals;
            _stats = stats;
            _config = config;

            // Multiplier stats read as ×1 when nothing has touched them.
            _stats.Player.SetBase(StatId.DamageMultiplier, 1f);
            _stats.Run.SetBase(StatId.GainMultiplier, 1f);

            // Momentum's own knobs live on the sheet so Protocols tune them
            // (Afterburner / Executioner's Cadence / Redline Governor).
            _stats.Player.SetBase(StatId.MomentumDecaySeconds, config.DecaySeconds);
            _stats.Player.SetBase(StatId.MomentumBonusStepsBelowHot, 0f);
            _stats.Player.SetBase(StatId.OverdriveMultiplier,
                config.TierMultipliers[(int)MomentumTier.Overdrive]);

            _finishSubscription = _signals.On<EnemyFinished>().Subscribe(_ => OnFinish());
            _whiffSubscription = _signals.On<VerbWhiffed>().Subscribe(_ => ResetToCool());
            // A protocol acquired while already at a tier must re-fold that tier's
            // multiplier (e.g. Redline Governor taken AT Overdrive).
            _protocolSubscription = _signals.On<ProtocolAcquired>().Subscribe(_ => ApplyTier(Tier, publish: false));

            _clock.Register(this, TICK_ORDER);
            ApplyTier(MomentumTier.Cool, publish: false);
        }

        public void Tick(float deltaTime)
        {
            if (_steps == 0) return;

            _idleSeconds += deltaTime;
            float decayWindow = Mathf.Max(0.5f, _stats.Player.GetValue(StatId.MomentumDecaySeconds));
            if (_idleSeconds < decayWindow) return;

            // Decay: -1 tier per idle window; steps snap to the bottom of the lower tier.
            _idleSeconds = 0f;
            int lowerTier = Mathf.Max(0, (int)Tier - 1);
            _steps = lowerTier * _config.StepsPerTier;
            var loweredTier = (MomentumTier)lowerTier;
            ApplyTier(loweredTier, publish: loweredTier != Tier); // no Cool→Cool spam
        }

        public void Dispose()
        {
            _finishSubscription?.Dispose();
            _whiffSubscription?.Dispose();
            _protocolSubscription?.Dispose();
            _clock.Unregister(this);
            _stats.Player.RemoveBySource(_source);
            _stats.Run.RemoveBySource(_source);
        }

        private void OnFinish()
        {
            int maxSteps = _config.StepsPerTier * ((int)MomentumTier.Overdrive);
            int gained = 1;
            if (Tier < MomentumTier.Hot)
            {
                gained += Mathf.Max(0, Mathf.RoundToInt(_stats.Player.GetValue(StatId.MomentumBonusStepsBelowHot)));
            }
            _steps = Mathf.Min(_steps + gained, maxSteps);
            _idleSeconds = 0f;
            var newTier = (MomentumTier)Mathf.Min(_steps / _config.StepsPerTier, (int)MomentumTier.Overdrive);
            if (newTier != Tier) ApplyTier(newTier, publish: true);
        }

        private void ResetToCool()
        {
            _steps = 0;
            _idleSeconds = 0f;
            if (Tier != MomentumTier.Cool) ApplyTier(MomentumTier.Cool, publish: true);
        }

        private void ApplyTier(MomentumTier tier, bool publish)
        {
            var previous = Tier;
            Tier = tier;

            float multiplier = tier == MomentumTier.Overdrive
                ? Mathf.Max(1f, _stats.Player.GetValue(StatId.OverdriveMultiplier))
                : _config.TierMultipliers[Mathf.Clamp((int)tier, 0, _config.TierMultipliers.Length - 1)];
            _stats.Player.RemoveBySource(_source);
            _stats.Run.RemoveBySource(_source);
            _stats.Player.AddModifier(StatId.DamageMultiplier, StatOp.Mult, multiplier, _source);
            _stats.Run.AddModifier(StatId.GainMultiplier, StatOp.Mult, multiplier, _source);

            if (publish) _signals.Publish(new MomentumTierChanged(previous, tier));
        }
    }
}
