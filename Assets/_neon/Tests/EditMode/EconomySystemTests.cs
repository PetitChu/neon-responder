using System.Collections.Generic;
using NUnit.Framework;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    public class EconomySystemTests
    {
        private GameplaySignals _signals;
        private StatSystem _stats;
        private EconomySystem _economy;
        private readonly List<XpGained> _xpEvents = new();
        private System.IDisposable _xpSub;

        private static GrowthConfig TestConfig => new(
            xpPerKill: 1, chargePerFinish: 2, overchargePerFinish: 8, overchargeCap: 20,
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
            _xpEvents.Clear();
            _xpSub = _signals.On<XpGained>().Subscribe(e => _xpEvents.Add(e));
        }

        [TearDown]
        public void TearDown()
        {
            _xpSub?.Dispose();
            _economy.Dispose();
            _signals.Dispose();
        }

        [Test]
        public void ChaffDied_GrantsKillXp()
        {
            _signals.Publish(new ChaffDied(Vector2.zero));

            Assert.AreEqual(1, _economy.Xp);
            Assert.AreEqual(1, _xpEvents.Count);
            Assert.AreEqual(1, _xpEvents[0].TotalXp);
        }

        [Test]
        public void GainMultiplier_AccumulatesFractionally()
        {
            var momentum = ModifierSource.Create("momentum");
            _stats.Run.AddModifier(StatId.GainMultiplier, StatOp.Mult, 1.3f, momentum);

            for (int i = 0; i < 3; i++) _signals.Publish(new ChaffDied(Vector2.zero));
            Assert.AreEqual(3, _economy.Xp);  // 3 × 1.3 = 3.9 → 3 whole

            _signals.Publish(new ChaffDied(Vector2.zero));
            Assert.AreEqual(5, _economy.Xp);  // 5.2 → 5 (the 0.9 remainder paid out)
        }

        [Test]
        public void Finish_GrantsChargeAndOvercharge_NotXp()
        {
            _signals.Publish(new EnemyFinished(Vector2.zero, wasChaff: true));

            Assert.AreEqual(0, _economy.Xp);
            Assert.AreEqual(2, _economy.NeonCharge);
            Assert.AreEqual(8, _economy.Overcharge);
        }

        [Test]
        public void Overcharge_CapsAtConfig()
        {
            for (int i = 0; i < 5; i++) _signals.Publish(new EnemyFinished(Vector2.zero, wasChaff: true));

            Assert.AreEqual(20, _economy.Overcharge); // cap 20 in TestConfig
        }

        [Test]
        public void HeroDeath_GrantsKillXp()
        {
            var enemy = new GameObject("Enemy");
            enemy.tag = "Enemy";

            _economy.OnUnitDeath(enemy);

            Assert.AreEqual(1, _economy.Xp);
            Object.DestroyImmediate(enemy);
        }

        [Test]
        public void PlayerDeath_GrantsNothing()
        {
            var player = new GameObject("Player");
            player.tag = "Player";

            _economy.OnUnitDeath(player);

            Assert.AreEqual(0, _economy.Xp);
            Object.DestroyImmediate(player);
        }
    }
}
