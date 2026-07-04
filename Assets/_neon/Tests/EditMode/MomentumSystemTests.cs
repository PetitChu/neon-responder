using NUnit.Framework;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    public class MomentumSystemTests
    {
        private GameplayClock _clock;
        private GameplaySignals _signals;
        private StatSystem _stats;
        private MomentumSystem _momentum;

        private static MomentumConfig TestConfig => new(stepsPerTier: 3, decaySeconds: 2.5f,
            tierMultipliers: new[] { 1f, 1.3f, 1.7f, 2.5f });

        [SetUp]
        public void SetUp()
        {
            _clock = new GameplayClock();
            _signals = new GameplaySignals();
            _stats = new StatSystem();
            _momentum = new MomentumSystem(_clock, _signals, _stats, TestConfig);
        }

        [TearDown]
        public void TearDown()
        {
            _momentum.Dispose();
            _signals.Dispose();
        }

        private void Finish() => _signals.Publish(new EnemyFinished(Vector2.zero, wasChaff: true));

        [Test]
        public void StartsCool_WithNeutralMultipliers()
        {
            Assert.AreEqual(MomentumTier.Cool, _momentum.Tier);
            Assert.AreEqual(1f, _stats.Player.GetValue(StatId.DamageMultiplier), 0.0001f);
            Assert.AreEqual(1f, _stats.Run.GetValue(StatId.GainMultiplier), 0.0001f);
        }

        [Test]
        public void ThreeFinishes_ReachWarm_AppliesMultiplier()
        {
            Finish(); Finish(); Finish();

            Assert.AreEqual(MomentumTier.Warm, _momentum.Tier);
            Assert.AreEqual(1.3f, _stats.Player.GetValue(StatId.DamageMultiplier), 0.0001f);
            Assert.AreEqual(1.3f, _stats.Run.GetValue(StatId.GainMultiplier), 0.0001f);
        }

        [Test]
        public void NineFinishes_ReachOverdrive_CapsThere()
        {
            for (int i = 0; i < 12; i++) Finish();

            Assert.AreEqual(MomentumTier.Overdrive, _momentum.Tier);
            Assert.AreEqual(2.5f, _stats.Player.GetValue(StatId.DamageMultiplier), 0.0001f);
        }

        [Test]
        public void TierChange_PublishesSignal()
        {
            MomentumTierChanged received = default;
            using var sub = _signals.On<MomentumTierChanged>().Subscribe(e => received = e);

            Finish(); Finish(); Finish();

            Assert.AreEqual(MomentumTier.Cool, received.Previous);
            Assert.AreEqual(MomentumTier.Warm, received.Current);
        }

        [Test]
        public void IdleDecay_DropsOneTierPerWindow()
        {
            for (int i = 0; i < 6; i++) Finish();          // Hot
            Assert.AreEqual(MomentumTier.Hot, _momentum.Tier);

            _clock.Advance(2.6f);                          // one idle window
            Assert.AreEqual(MomentumTier.Warm, _momentum.Tier);
            Assert.AreEqual(1.3f, _stats.Player.GetValue(StatId.DamageMultiplier), 0.0001f);

            _clock.Advance(2.6f);                          // another
            Assert.AreEqual(MomentumTier.Cool, _momentum.Tier);
        }

        [Test]
        public void Finish_ResetsIdleTimer()
        {
            Finish(); Finish(); Finish();                  // Warm
            _clock.Advance(2f);                            // not yet decayed
            Finish();                                      // resets idle
            _clock.Advance(2f);                            // still under window since last finish

            Assert.AreEqual(MomentumTier.Warm, _momentum.Tier);
        }

        [Test]
        public void Whiff_ResetsToCool()
        {
            for (int i = 0; i < 9; i++) Finish();          // Overdrive
            _signals.Publish(new VerbWhiffed(ATTACKTYPE.PUNCH));

            Assert.AreEqual(MomentumTier.Cool, _momentum.Tier);
            Assert.AreEqual(1f, _stats.Player.GetValue(StatId.DamageMultiplier), 0.0001f);
        }

        [Test]
        public void Dispose_RemovesModifiersAndStopsTicking()
        {
            Finish(); Finish(); Finish();                  // Warm, ×1.3
            _momentum.Dispose();

            Assert.AreEqual(1f, _stats.Player.GetValue(StatId.DamageMultiplier), 0.0001f);
            Assert.DoesNotThrow(() => _clock.Advance(3f)); // unregistered from clock
        }
    }
}
