using NUnit.Framework;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    public class ProtocolEffectsSystemTests
    {
        private GameplaySignals _signals;
        private StatSystem _stats;
        private FakeEntitiesService _entities;
        private FakeSwarmBridge _bridge;
        private ProtocolEffectsSystem _effects;
        private GameObject _player;
        private HealthSystem _health;
        private UnitSettings _settings;
        private ModifierSource _source;

        private static GrowthConfig TestConfig => new(
            xpPerKill: 1, chargePerFinish: 2, overchargePerFinish: 8, overchargeCap: 100,
            xpCostBase: 10f, xpCostExponent: 1.35f, levelUpSlowMoScale: 0.1f,
            challengeSequenceBase: new[] { ATTACKTYPE.PUNCH, ATTACKTYPE.KICK },
            challengeSequenceHot: new[] { ATTACKTYPE.PUNCH, ATTACKTYPE.KICK, ATTACKTYPE.PUNCH },
            challengeInputWindowSeconds: 0.9f, challengeWindowTightenPerTier: 0.1f, finishAoeDamage: 6);

        [SetUp]
        public void SetUp()
        {
            _signals = new GameplaySignals();
            _stats = new StatSystem();
            _entities = new FakeEntitiesService();
            _bridge = new FakeSwarmBridge();
            _effects = new ProtocolEffectsSystem(_signals, _stats, _entities, _bridge, TestConfig);
            _source = ModifierSource.Create("test-protocol");

            _player = new GameObject("Player");
            _health = _player.AddComponent<HealthSystem>();
            _health.maxHp = 100;
            _health.currentHp = 100;
            _settings = _player.AddComponent<UnitSettings>();
            _settings.grabDuration = 3f;
            _entities.Register(_player, UNITTYPE.PLAYER); // fires OnEntityRegistered → base capture
        }

        [TearDown]
        public void TearDown()
        {
            _effects.Dispose();
            _signals.Dispose();
            Object.DestroyImmediate(_player);
        }

        private void AcquireSomething()
        {
            var protocol = ScriptableObject.CreateInstance<ProtocolDefinitionAsset>();
            _signals.Publish(new ProtocolAcquired(protocol, 1));
            Object.DestroyImmediate(protocol);
        }

        [Test]
        public void Ctor_SeedsDerivedStatBases()
        {
            Assert.AreEqual(1f, _stats.Player.GetValue(StatId.PlayerMaxHealthPct), 0.0001f);
            Assert.AreEqual(1f, _stats.Player.GetValue(StatId.GrabDurationScale), 0.0001f);
            Assert.AreEqual(0f, _stats.Player.GetValue(StatId.FinishAoeRadius), 0.0001f);
            Assert.AreEqual(0f, _stats.Player.GetValue(StatId.HealPerFinish), 0.0001f);
        }

        [Test]
        public void MaxHealthPct_RescalesPlayerHp_PreservingRatio()
        {
            _health.currentHp = 50; // 50%
            _stats.Player.AddModifier(StatId.PlayerMaxHealthPct, StatOp.PctAdd, -0.1f, _source);

            AcquireSomething();

            Assert.AreEqual(90, _health.maxHp);
            Assert.AreEqual(45, _health.currentHp);
        }

        [Test]
        public void MaxHealth_FloorsAt50()
        {
            _stats.Player.AddModifier(StatId.PlayerMaxHealthPct, StatOp.PctAdd, -0.9f, _source);

            AcquireSomething();

            Assert.AreEqual(50, _health.maxHp); // §8.1 HP floor
        }

        [Test]
        public void GrabDurationScale_RescalesFromCapturedBase()
        {
            _stats.Player.AddModifier(StatId.GrabDurationScale, StatOp.PctAdd, 0.5f, _source);

            AcquireSomething();
            Assert.AreEqual(4.5f, _settings.grabDuration, 0.0001f);

            AcquireSomething(); // reapplying must NOT compound (base was captured once)
            Assert.AreEqual(4.5f, _settings.grabDuration, 0.0001f);
        }

        [Test]
        public void HealPerFinish_HealsOnEnemyFinished()
        {
            _health.currentHp = 40;
            _stats.Player.AddModifier(StatId.HealPerFinish, StatOp.Add, 2f, _source);

            _signals.Publish(new EnemyFinished(Vector2.zero, wasChaff: true));

            Assert.AreEqual(42, _health.currentHp);
        }

        [Test]
        public void FinishAoe_CallsBridgeWithScaledDamage()
        {
            _stats.Player.SetBase(StatId.DamageMultiplier, 1f);
            var momentum = ModifierSource.Create("momentum");
            _stats.Player.AddModifier(StatId.DamageMultiplier, StatOp.Mult, 2f, momentum);
            _stats.Player.AddModifier(StatId.FinishAoeRadius, StatOp.Add, 0.8f, _source);

            _signals.Publish(new EnemyFinished(new Vector2(3f, 1f), wasChaff: true));

            Assert.AreEqual(1, _bridge.AreaDamageCalls.Count);
            Assert.AreEqual(new Vector2(3f, 1f), _bridge.AreaDamageCalls[0].Center);
            Assert.AreEqual(0.8f, _bridge.AreaDamageCalls[0].Radius, 0.0001f);
            Assert.AreEqual(12, _bridge.AreaDamageCalls[0].Damage); // 6 × 2
        }

        [Test]
        public void NoAoe_WhenRadiusIsZero()
        {
            _signals.Publish(new EnemyFinished(Vector2.zero, wasChaff: true));

            Assert.AreEqual(0, _bridge.AreaDamageCalls.Count);
        }
    }
}
