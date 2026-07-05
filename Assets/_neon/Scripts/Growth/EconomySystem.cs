using System;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    public sealed class EconomySystem : IEconomySystem, IDisposable
    {
        private readonly IGameplaySignals _signals;
        private readonly IStatSystem _stats;
        private readonly GrowthConfig _config;
        private readonly IDisposable _chaffDiedSubscription;
        private readonly IDisposable _finishSubscription;

        private float _xpFraction;
        private float _chargeFraction;
        private float _overchargeFraction;

        public int Xp { get; private set; }
        public int NeonCharge { get; private set; }
        public int Overcharge { get; private set; }

        public EconomySystem(IGameplaySignals signals, IStatSystem stats, GrowthConfig config)
        {
            _signals = signals;
            _stats = stats;
            _config = config;

            _chaffDiedSubscription = _signals.On<ChaffDied>().Subscribe(_ => GrantXp(_config.XpPerKill));
            _finishSubscription = _signals.On<EnemyFinished>().Subscribe(_ => OnFinish());
            HealthSystem.onUnitDeath += OnUnitDeath;
        }

        public void Dispose()
        {
            _chaffDiedSubscription?.Dispose();
            _finishSubscription?.Dispose();
            HealthSystem.onUnitDeath -= OnUnitDeath;
        }

        /// <summary>Public so EditMode tests can drive it (static event can't be raised externally).</summary>
        public void OnUnitDeath(GameObject unit)
        {
            if (unit == null || !unit.CompareTag("Enemy")) return;
            GrantXp(_config.XpPerKill);
        }

        private void OnFinish()
        {
            NeonCharge += GrantWhole(_config.ChargePerFinish, ref _chargeFraction);
            _signals.Publish(new NeonChargeChanged(NeonCharge));

            int overchargeGain = GrantWhole(_config.OverchargePerFinish, ref _overchargeFraction);
            Overcharge = Mathf.Min(Overcharge + overchargeGain, _config.OverchargeCap);
            _signals.Publish(new OverchargeChanged(Overcharge, _config.OverchargeCap));
        }

        private void GrantXp(int baseAmount)
        {
            int granted = GrantWhole(baseAmount, ref _xpFraction);
            if (granted == 0) return;

            Xp += granted;
            _signals.Publish(new XpGained(granted, Xp));
        }

        // Fractional remainders accumulate so ×1.3 on a 1-XP kill pays out fairly
        // over time instead of rounding away every gain.
        private int GrantWhole(int baseAmount, ref float fraction)
        {
            float multiplier = _stats.Run.GetValue(StatId.GainMultiplier);
            if (multiplier <= 0f) multiplier = 1f;

            fraction += baseAmount * multiplier;
            int whole = Mathf.FloorToInt(fraction);
            fraction -= whole;
            return whole;
        }
    }
}
