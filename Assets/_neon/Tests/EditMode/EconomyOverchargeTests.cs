using NUnit.Framework;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    public class EconomyOverchargeTests
    {
        private GameplaySignals _signals;
        private StatSystem _stats;
        private EconomySystem _economy;

        // overchargePerFinish 8, cap 24 → full after 3 finishes.
        private static GrowthConfig TestConfig => new(
            xpPerKill: 1, chargePerFinish: 2, overchargePerFinish: 8, overchargeCap: 24,
            xpCostBase: 10f, xpCostExponent: 1.35f, levelUpSlowMoScale: 0.1f,
            challengeSequenceBase: new[] { ATTACKTYPE.PUNCH, ATTACKTYPE.KICK },
            challengeSequenceHot: new[] { ATTACKTYPE.PUNCH, ATTACKTYPE.KICK, ATTACKTYPE.PUNCH },
            challengeInputWindowSeconds: 0.9f, challengeWindowTightenPerTier: 0.1f, finishAoeDamage: 6);

        [SetUp]
        public void SetUp()
        {
            _signals = new GameplaySignals();
            _stats = new StatSystem();
            _stats.Run.SetBase(StatId.GainMultiplier, 1f);
            _economy = new EconomySystem(_signals, _stats, TestConfig);
        }

        [TearDown]
        public void TearDown()
        {
            _economy.Dispose();
            _signals.Dispose();
        }

        private void Finishes(int n)
        {
            for (int i = 0; i < n; i++) _signals.Publish(new EnemyFinished(Vector2.zero, wasChaff: true));
        }

        [Test]
        public void NotFull_ConsumeFails_NoChange()
        {
            Finishes(2); // 16 < 24

            Assert.IsFalse(_economy.TryConsumeOvercharge());
            Assert.AreEqual(16, _economy.Overcharge);
        }

        [Test]
        public void Full_ConsumeSucceeds_Zeroes()
        {
            Finishes(3); // 24 (capped)
            Assert.AreEqual(24, _economy.Overcharge);

            Assert.IsTrue(_economy.TryConsumeOvercharge());
            Assert.AreEqual(0, _economy.Overcharge);
        }

        [Test]
        public void Consume_PublishesOverchargeChanged()
        {
            Finishes(3);
            int last = -1;
            using var sub = _signals.On<OverchargeChanged>().Subscribe(e => last = e.Value);

            _economy.TryConsumeOvercharge();

            Assert.AreEqual(0, last);
        }

        [Test]
        public void RefillsAfterConsume()
        {
            Finishes(3);
            _economy.TryConsumeOvercharge();
            Finishes(3);

            Assert.AreEqual(24, _economy.Overcharge);
            Assert.IsTrue(_economy.TryConsumeOvercharge());
        }
    }
}
