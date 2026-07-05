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
        private FakeMomentumSystem _momentum;
        private FinishResolver _resolver;
        private readonly List<GameObject> _spawned = new();
        private readonly List<EnemyFinished> _finishes = new();
        private readonly List<VerbWhiffed> _whiffs = new();
        private readonly List<FinishChallengeChanged> _challengeEvents = new();
        private System.IDisposable _finishSub;
        private System.IDisposable _whiffSub;
        private System.IDisposable _challengeSub;

        private static EngagementConfig EngagementTestConfig => new(
            ratePerSecond: 1.5f, chipDamage: 8, arcDegrees: 120f, range: 4f,
            finishReadyThreshold: 0.25f, finishReadyGlow: Color.yellow, whiffStaggerSeconds: 0.5f);

        private static GrowthConfig GrowthTestConfig => new(
            xpPerKill: 1, chargePerFinish: 2, overchargePerFinish: 8, overchargeCap: 100,
            xpCostBase: 10f, xpCostExponent: 1.35f, levelUpSlowMoScale: 0.1f,
            challengeSequenceBase: new[] { ATTACKTYPE.PUNCH, ATTACKTYPE.KICK },
            challengeSequenceHot: new[] { ATTACKTYPE.PUNCH, ATTACKTYPE.KICK, ATTACKTYPE.PUNCH },
            challengeInputWindowSeconds: 0.9f, challengeWindowTightenPerTier: 0.1f, finishAoeDamage: 6);

        [SetUp]
        public void SetUp()
        {
            _clock = new GameplayClock();
            _signals = new GameplaySignals();
            _momentum = new FakeMomentumSystem();
            _resolver = new FinishResolver(_clock, _signals, _momentum, EngagementTestConfig, GrowthTestConfig);
            _finishes.Clear();
            _whiffs.Clear();
            _challengeEvents.Clear();
            _finishSub = _signals.On<EnemyFinished>().Subscribe(e => _finishes.Add(e));
            _whiffSub = _signals.On<VerbWhiffed>().Subscribe(e => _whiffs.Add(e));
            _challengeSub = _signals.On<FinishChallengeChanged>().Subscribe(e => _challengeEvents.Add(e));
        }

        [TearDown]
        public void TearDown()
        {
            _finishSub?.Dispose();
            _whiffSub?.Dispose();
            _challengeSub?.Dispose();
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

        private (GameObject enemy, AttackData Punch, AttackData Kick) MakeReadyEnemy(int hp)
        {
            var enemy = Spawn("Enemy");
            var marker = enemy.AddComponent<FinishReadyMarker>();
            marker.SetReady(true, Color.yellow);
            var health = enemy.AddComponent<HealthSystem>();
            health.maxHp = 40;
            health.currentHp = hp;

            var inflictor = Spawn("Inflictor");
            inflictor.tag = "Player";
            var punch = new AttackData("p", 2, inflictor, ATTACKTYPE.PUNCH, knockdown: false);
            var kick = new AttackData("k", 2, inflictor, ATTACKTYPE.KICK, knockdown: false);
            return (enemy, punch, kick);
        }

        [Test]
        public void FirstMatchingHit_StartsChallenge()
        {
            var (enemy, punch, _) = MakeReadyEnemy(hp: 10);

            _resolver.HandleDamage(enemy, punch);

            Assert.AreEqual(0, _finishes.Count);
            Assert.IsTrue(_challengeEvents[_challengeEvents.Count - 1].Active);
            Assert.AreEqual(ATTACKTYPE.KICK, _challengeEvents[_challengeEvents.Count - 1].ExpectedVerb);
            Assert.AreEqual(1, _challengeEvents[_challengeEvents.Count - 1].Progress);
        }

        [Test]
        public void CompletedSequence_PublishesEnemyFinished()
        {
            var (enemy, punch, kick) = MakeReadyEnemy(hp: 10);

            _resolver.HandleDamage(enemy, punch);
            _resolver.HandleDamage(enemy, kick);

            Assert.AreEqual(1, _finishes.Count);
            Assert.IsFalse(_finishes[0].WasChaff);
            // The execution-kill runs on the next Tick at runtime — not invoked here
            // because HealthSystem.SubstractHealth touches the injected audio service.
        }

        [Test]
        public void WrongVerb_DoesNotComplete()
        {
            var (enemy, punch, _) = MakeReadyEnemy(hp: 10);

            _resolver.HandleDamage(enemy, punch);
            _resolver.HandleDamage(enemy, punch); // expected KICK

            Assert.AreEqual(0, _finishes.Count);
            Assert.AreEqual(1, _challengeEvents[_challengeEvents.Count - 1].Progress); // restarted at 1
        }

        [Test]
        public void HotTier_DemandsThreeInputs()
        {
            _momentum.Tier = MomentumTier.Hot;
            var (enemy, punch, kick) = MakeReadyEnemy(hp: 10);

            _resolver.HandleDamage(enemy, punch);
            _resolver.HandleDamage(enemy, kick);
            Assert.AreEqual(0, _finishes.Count); // 2/3

            _resolver.HandleDamage(enemy, punch);
            Assert.AreEqual(1, _finishes.Count);
        }

        [Test]
        public void DyingMidSequence_IsNotAFinish()
        {
            var (enemy, punch, _) = MakeReadyEnemy(hp: 10);
            enemy.GetComponent<HealthSystem>().currentHp = 0; // the hit killed before resolution

            _resolver.HandleDamage(enemy, punch);

            Assert.AreEqual(0, _finishes.Count);
        }

        [Test]
        public void NotReadyTarget_NoChallenge()
        {
            var (enemy, punch, _) = MakeReadyEnemy(hp: 10);
            enemy.GetComponent<FinishReadyMarker>().SetReady(false, Color.yellow);

            _resolver.HandleDamage(enemy, punch);

            Assert.AreEqual(0, _finishes.Count);
            Assert.AreEqual(0, _challengeEvents.Count);
        }

        [Test]
        public void EnemyInflictedHit_Ignored()
        {
            var (enemy, _, _) = MakeReadyEnemy(hp: 10);
            var enemyInflictor = Spawn("EnemyInflictor");
            var attack = new AttackData("e", 2, enemyInflictor, ATTACKTYPE.PUNCH, knockdown: false);

            _resolver.HandleDamage(enemy, attack);

            Assert.AreEqual(0, _challengeEvents.Count);
        }

        [Test]
        public void PlayerWhiff_PublishesVerbWhiffed()
        {
            var player = Spawn("Player");
            player.AddComponent<UnitSettings>();
            var actions = player.AddComponent<UnitActions>();

            _resolver.HandleWhiff(actions, ATTACKTYPE.KICK);

            Assert.AreEqual(1, _whiffs.Count);
            Assert.AreEqual(ATTACKTYPE.KICK, _whiffs[0].AttackType);
        }

        [Test]
        public void EnemyWhiff_Ignored()
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
