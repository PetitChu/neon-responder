using NUnit.Framework;
using R3;

namespace BrainlessLabs.Neon.Tests
{
    public class SignalSystemTests
    {
        private GameplaySignals _signals;
        private StatSystem _stats;
        private SignalSystem _signal;

        // dawn = 1, +100% nastiness at dawn (×2)
        private SignalSystem Make() => new SignalSystem(_signals, _stats, dawnValue: 1f, maxSpawnNastinessBonus: 1f);

        [SetUp]
        public void SetUp()
        {
            _signals = new GameplaySignals();
            _stats = new StatSystem();
            _signal = Make();
        }

        [TearDown]
        public void TearDown()
        {
            _signal.Dispose();
            _signals.Dispose();
        }

        [Test]
        public void StartsAtZero_WithBaselineNastiness()
        {
            Assert.AreEqual(0f, _signal.Value, 0.0001f);
            Assert.IsFalse(_signal.IsDawn);
            Assert.AreEqual(1f, _stats.Run.GetValue(StatId.SpawnNastiness), 0.0001f); // base 1, no bonus
        }

        [Test]
        public void Raise_IncreasesValue_AndNastiness()
        {
            _signal.Raise(0.5f); // half to dawn → +50% nastiness

            Assert.AreEqual(0.5f, _signal.Value, 0.0001f);
            Assert.AreEqual(1.5f, _stats.Run.GetValue(StatId.SpawnNastiness), 0.0001f);
        }

        [Test]
        public void Raise_ClampsAtDawn_AndFlagsIt()
        {
            _signal.Raise(0.7f);
            _signal.Raise(0.7f); // 1.4 → clamps to 1

            Assert.AreEqual(1f, _signal.Value, 0.0001f);
            Assert.IsTrue(_signal.IsDawn);
            Assert.AreEqual(2f, _stats.Run.GetValue(StatId.SpawnNastiness), 0.0001f); // ×2 at dawn
        }

        [Test]
        public void Lower_ReducesValue_ClampsAtZero()
        {
            _signal.Raise(0.3f);
            _signal.Lower(0.5f);

            Assert.AreEqual(0f, _signal.Value, 0.0001f);
            Assert.IsFalse(_signal.IsDawn);
            Assert.AreEqual(1f, _stats.Run.GetValue(StatId.SpawnNastiness), 0.0001f);
        }

        [Test]
        public void Raise_PublishesSignalChanged()
        {
            SignalChanged received = default;
            using var sub = _signals.On<SignalChanged>().Subscribe(e => received = e);

            _signal.Raise(0.5f);

            Assert.AreEqual(0.5f, received.Value, 0.0001f);
            Assert.AreEqual(1f, received.Dawn, 0.0001f);
        }

        [Test]
        public void NastinessStacksAdditively_NotWithMomentum()
        {
            // A separate Mult source on a DIFFERENT stat must not interact.
            _stats.Run.SetBase(StatId.GainMultiplier, 1f);
            var other = ModifierSource.Create("other");
            _stats.Run.AddModifier(StatId.GainMultiplier, StatOp.Mult, 2f, other);

            _signal.Raise(1f); // dawn

            Assert.AreEqual(2f, _stats.Run.GetValue(StatId.SpawnNastiness), 0.0001f);   // Signal only
            Assert.AreEqual(2f, _stats.Run.GetValue(StatId.GainMultiplier), 0.0001f);   // untouched by Signal
        }

        [Test]
        public void Dispose_RemovesNastinessModifier()
        {
            _signal.Raise(1f);
            _signal.Dispose();

            Assert.AreEqual(1f, _stats.Run.GetValue(StatId.SpawnNastiness), 0.0001f); // back to base
        }
    }
}
