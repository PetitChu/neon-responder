using NUnit.Framework;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    public class MomentumKnobTests
    {
        private GameplayClock _clock;
        private GameplaySignals _signals;
        private StatSystem _stats;
        private MomentumSystem _momentum;
        private ModifierSource _source;

        private static MomentumConfig TestConfig => new(stepsPerTier: 3, decaySeconds: 2.5f,
            tierMultipliers: new[] { 1f, 1.3f, 1.7f, 2.5f });

        [SetUp]
        public void SetUp()
        {
            _clock = new GameplayClock();
            _signals = new GameplaySignals();
            _stats = new StatSystem();
            _momentum = new MomentumSystem(_clock, _signals, _stats, TestConfig);
            _source = ModifierSource.Create("test-protocol");
        }

        [TearDown]
        public void TearDown()
        {
            _momentum.Dispose();
            _signals.Dispose();
        }

        private void Finish() => _signals.Publish(new EnemyFinished(Vector2.zero, wasChaff: true));

        [Test]
        public void Ctor_SeedsKnobStats()
        {
            Assert.AreEqual(2.5f, _stats.Player.GetValue(StatId.MomentumDecaySeconds), 0.0001f);
            Assert.AreEqual(0f, _stats.Player.GetValue(StatId.MomentumBonusStepsBelowHot), 0.0001f);
            Assert.AreEqual(2.5f, _stats.Player.GetValue(StatId.OverdriveMultiplier), 0.0001f);
        }

        [Test]
        public void DecayStat_ExtendsTheIdleWindow()
        {
            // Afterburner: 2.5s → 4.2s
            _stats.Player.AddModifier(StatId.MomentumDecaySeconds, StatOp.Add, 1.7f, _source);
            Finish(); Finish(); Finish(); // Warm

            _clock.Advance(3f);           // would have decayed at 2.5s
            Assert.AreEqual(MomentumTier.Warm, _momentum.Tier);

            _clock.Advance(1.5f);         // 4.5s total idle > 4.2s
            Assert.AreEqual(MomentumTier.Cool, _momentum.Tier);
        }

        [Test]
        public void BonusStepsBelowHot_DoubleMomentumWhileRecovering()
        {
            // Executioner's Cadence: +1 bonus step below Hot → 2 finishes reach Warm.
            _stats.Player.AddModifier(StatId.MomentumBonusStepsBelowHot, StatOp.Add, 1f, _source);

            Finish(); Finish();           // 2 × 2 steps = 4 → Warm

            Assert.AreEqual(MomentumTier.Warm, _momentum.Tier);
        }

        [Test]
        public void BonusSteps_DoNotApplyAtHotOrAbove()
        {
            _stats.Player.AddModifier(StatId.MomentumBonusStepsBelowHot, StatOp.Add, 1f, _source);

            Finish(); Finish(); Finish(); // 6 steps → Hot
            Assert.AreEqual(MomentumTier.Hot, _momentum.Tier);

            Finish(); Finish(); Finish(); // +1 each at Hot → 9 → Overdrive
            Assert.AreEqual(MomentumTier.Overdrive, _momentum.Tier);
        }

        [Test]
        public void OverdriveMultiplierStat_GovernsTheTopTier()
        {
            // Redline Governor: ×2.5 → ×3.0
            _stats.Player.AddModifier(StatId.OverdriveMultiplier, StatOp.Add, 0.5f, _source);

            for (int i = 0; i < 9; i++) Finish();

            Assert.AreEqual(MomentumTier.Overdrive, _momentum.Tier);
            Assert.AreEqual(3.0f, _stats.Player.GetValue(StatId.DamageMultiplier), 0.0001f);
        }

        [Test]
        public void OverdriveMultiplier_AcquiredWhileAtOverdrive_RefreshesOnProtocolAcquired()
        {
            for (int i = 0; i < 9; i++) Finish(); // Overdrive at ×2.5
            _stats.Player.AddModifier(StatId.OverdriveMultiplier, StatOp.Add, 0.5f, _source);

            var protocol = ScriptableObject.CreateInstance<ProtocolDefinitionAsset>();
            _signals.Publish(new ProtocolAcquired(protocol, 1));

            Assert.AreEqual(3.0f, _stats.Player.GetValue(StatId.DamageMultiplier), 0.0001f);
            Object.DestroyImmediate(protocol);
        }
    }
}
