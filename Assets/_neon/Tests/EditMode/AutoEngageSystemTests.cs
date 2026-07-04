using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    public class AutoEngageSystemTests
    {
        private GameplayClock _clock;
        private StatSystem _stats;
        private FakeEntitiesService _entities;
        private FakeSwarmBridge _bridge;
        private AutoEngageSystem _autoEngage;
        private GameObject _player;

        private static EngagementConfig TestConfig => new(
            ratePerSecond: 2f, chipDamage: 8, arcDegrees: 120f, range: 4f,
            finishReadyThreshold: 0.25f, finishReadyGlow: Color.yellow, whiffStaggerSeconds: 0.5f);

        [SetUp]
        public void SetUp()
        {
            _clock = new GameplayClock();
            _stats = new StatSystem();
            _entities = new FakeEntitiesService();
            _bridge = new FakeSwarmBridge();
            _player = new GameObject("TestPlayer");
            _entities.Register(_player, UNITTYPE.PLAYER);
            _autoEngage = new AutoEngageSystem(_clock, _stats, _entities, _bridge, TestConfig);
        }

        [TearDown]
        public void TearDown()
        {
            _autoEngage.Dispose();
            Object.DestroyImmediate(_player);
        }

        private static TargetRef ChaffAt(float x) => new(new Entity { Index = 1, Version = 1 }, new Vector2(x, 0f));

        [Test]
        public void Fires_AtConfiguredRate()
        {
            _bridge.NearestHot = ChaffAt(1f);

            _clock.Advance(1f); // rate 2/s

            Assert.AreEqual(2, _bridge.ChipCalls.Count);
        }

        [Test]
        public void RateStat_DrivesCadence()
        {
            _bridge.NearestHot = ChaffAt(1f);
            _stats.Player.SetBase(StatId.AutoEngageRate, 4f);

            _clock.Advance(1f);

            Assert.AreEqual(4, _bridge.ChipCalls.Count);
        }

        [Test]
        public void RateStat_HardCappedAtSix()
        {
            _bridge.NearestHot = ChaffAt(1f);
            _stats.Player.SetBase(StatId.AutoEngageRate, 40f); // protocol-doc cap: 6/s

            _clock.Advance(1f);

            Assert.AreEqual(6, _bridge.ChipCalls.Count);
        }

        [Test]
        public void ChipDamage_ScaledByDamageMultiplier()
        {
            _bridge.NearestHot = ChaffAt(1f);
            var source = ModifierSource.Create("test");
            _stats.Player.SetBase(StatId.DamageMultiplier, 1f);
            _stats.Player.AddModifier(StatId.DamageMultiplier, StatOp.Mult, 2f, source);

            _clock.Advance(0.5f); // one shot at 2/s

            Assert.AreEqual(1, _bridge.ChipCalls.Count);
            Assert.AreEqual(16, _bridge.ChipCalls[0].Damage); // 8 × 2
        }

        [Test]
        public void NoTargets_DoesNothing()
        {
            Assert.DoesNotThrow(() => _clock.Advance(2f));
            Assert.AreEqual(0, _bridge.ChipCalls.Count);
        }

        [Test]
        public void NearerHero_WinsOverChaff_NoChipCall()
        {
            _bridge.NearestHot = ChaffAt(3f);
            var hero = new GameObject("TestEnemy");
            hero.transform.position = new Vector3(1f, 0f, 0f); // in front (facing defaults right)
            _entities.Register(hero, UNITTYPE.ENEMY);

            _clock.Advance(0.5f);

            Assert.AreEqual(0, _bridge.ChipCalls.Count); // chip went to the hero, not the bridge
            Object.DestroyImmediate(hero);
        }
    }
}
