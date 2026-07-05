using NUnit.Framework;
using R3;

namespace BrainlessLabs.Neon.Tests
{
    public class EconomySpendTests
    {
        private GameplaySignals _signals;
        private StatSystem _stats;
        private EconomySystem _economy;

        private static GrowthConfig TestConfig => new(
            xpPerKill: 1, chargePerFinish: 10, overchargePerFinish: 8, overchargeCap: 100,
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

        private void Earn(int finishes)
        {
            for (int i = 0; i < finishes; i++) _signals.Publish(new EnemyFinished(UnityEngine.Vector2.zero, wasChaff: true));
        }

        [Test]
        public void TrySpend_WithEnough_DeductsAndReturnsTrue()
        {
            Earn(3); // 30 charge

            bool ok = _economy.TrySpend(25);

            Assert.IsTrue(ok);
            Assert.AreEqual(5, _economy.NeonCharge);
        }

        [Test]
        public void TrySpend_Insufficient_NoChangeReturnsFalse()
        {
            Earn(2); // 20 charge

            bool ok = _economy.TrySpend(25);

            Assert.IsFalse(ok);
            Assert.AreEqual(20, _economy.NeonCharge);
        }

        [Test]
        public void TrySpend_Exact_Succeeds()
        {
            Earn(1); // 10

            Assert.IsTrue(_economy.TrySpend(10));
            Assert.AreEqual(0, _economy.NeonCharge);
        }

        [Test]
        public void TrySpend_PublishesNeonChargeChanged()
        {
            Earn(3);
            int lastTotal = -1;
            using var sub = _signals.On<NeonChargeChanged>().Subscribe(e => lastTotal = e.Total);

            _economy.TrySpend(25);

            Assert.AreEqual(5, lastTotal);
        }

        [Test]
        public void TrySpend_NonPositive_IsNoOpTrue()
        {
            Earn(1);

            Assert.IsTrue(_economy.TrySpend(0));
            Assert.AreEqual(10, _economy.NeonCharge);
        }
    }
}
