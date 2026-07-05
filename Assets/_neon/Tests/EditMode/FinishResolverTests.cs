using System.Collections.Generic;
using NUnit.Framework;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    public class FinishResolverTests
    {
        private GameplayClock _clock;
        private GameplaySignals _signals;
        private FinishResolver _resolver;
        private readonly List<GameObject> _spawned = new();
        private readonly List<EnemyFinished> _finishes = new();
        private readonly List<VerbWhiffed> _whiffs = new();
        private System.IDisposable _finishSub;
        private System.IDisposable _whiffSub;

        private static EngagementConfig TestConfig => new(
            ratePerSecond: 1.5f, chipDamage: 8, arcDegrees: 120f, range: 4f,
            finishReadyThreshold: 0.25f, finishReadyGlow: Color.yellow, whiffStaggerSeconds: 0.5f);

        [SetUp]
        public void SetUp()
        {
            _clock = new GameplayClock();
            _signals = new GameplaySignals();
            _resolver = new FinishResolver(_clock, _signals, TestConfig);
            _finishes.Clear();
            _whiffs.Clear();
            _finishSub = _signals.On<EnemyFinished>().Subscribe(e => _finishes.Add(e));
            _whiffSub = _signals.On<VerbWhiffed>().Subscribe(e => _whiffs.Add(e));
        }

        [TearDown]
        public void TearDown()
        {
            _finishSub?.Dispose();
            _whiffSub?.Dispose();
            _resolver.Dispose();
            _signals.Dispose();
            foreach (var go in _spawned) Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        private GameObject Spawn(string name)
        {
            var go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        private (GameObject enemy, AttackData attack) MakeHit(bool ready, int remainingHp, bool playerInflictor = true)
        {
            var enemy = Spawn("Enemy");
            var marker = enemy.AddComponent<FinishReadyMarker>();
            marker.SetReady(ready, Color.yellow);
            var health = enemy.AddComponent<HealthSystem>();
            health.maxHp = 10;
            health.currentHp = remainingHp;

            var inflictor = Spawn("Inflictor");
            if (playerInflictor) inflictor.tag = "Player";
            var attack = new AttackData("test", 5, inflictor, ATTACKTYPE.PUNCH, knockdown: false);
            return (enemy, attack);
        }

        [Test]
        public void KillingHit_OnFinishReadyTarget_PublishesEnemyFinished()
        {
            var (enemy, attack) = MakeHit(ready: true, remainingHp: 0);

            _resolver.HandleDamage(enemy, attack);

            Assert.AreEqual(1, _finishes.Count);
            Assert.IsFalse(_finishes[0].WasChaff);
        }

        [Test]
        public void KillingHit_OnNotReadyTarget_IsNotAFinish()
        {
            var (enemy, attack) = MakeHit(ready: false, remainingHp: 0);

            _resolver.HandleDamage(enemy, attack);

            Assert.AreEqual(0, _finishes.Count);
        }

        [Test]
        public void NonKillingHit_OnReadyTarget_IsNotAFinish()
        {
            var (enemy, attack) = MakeHit(ready: true, remainingHp: 2);

            _resolver.HandleDamage(enemy, attack);

            Assert.AreEqual(0, _finishes.Count);
        }

        [Test]
        public void EnemyInflictedKill_IsNotAFinish()
        {
            var (enemy, attack) = MakeHit(ready: true, remainingHp: 0, playerInflictor: false);

            _resolver.HandleDamage(enemy, attack);

            Assert.AreEqual(0, _finishes.Count);
        }

        [Test]
        public void PlayerWhiff_PublishesVerbWhiffed()
        {
            var player = Spawn("Player");
            player.AddComponent<UnitSettings>(); // unitType defaults to PLAYER
            var actions = player.AddComponent<UnitActions>();

            _resolver.HandleWhiff(actions, ATTACKTYPE.KICK);

            Assert.AreEqual(1, _whiffs.Count);
            Assert.AreEqual(ATTACKTYPE.KICK, _whiffs[0].AttackType);
        }

        [Test]
        public void EnemyWhiff_IsIgnored()
        {
            var enemy = Spawn("Enemy");
            var settings = enemy.AddComponent<UnitSettings>();
            settings.unitType = UNITTYPE.ENEMY;
            var actions = enemy.AddComponent<UnitActions>();

            _resolver.HandleWhiff(actions, ATTACKTYPE.PUNCH);

            Assert.AreEqual(0, _whiffs.Count);
        }
    }
}
