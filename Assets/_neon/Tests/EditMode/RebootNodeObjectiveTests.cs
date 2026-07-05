using NUnit.Framework;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    public class RebootNodeObjectiveTests
    {
        private StatSystem _stats;

        private RebootNodeObjective Make(float duration = 10f, float radius = 2f)
        {
            _stats ??= new StatSystem();
            return new RebootNodeObjective(_stats, new Vector2(0f, 0f), radius, duration);
        }

        [SetUp]
        public void SetUp() => _stats = new StatSystem();

        [Test]
        public void SeedsFillRateScaleBase()
        {
            Make();
            Assert.AreEqual(1f, _stats.Run.GetValue(StatId.ObjectiveFillRateScale), 0.0001f);
        }

        [Test]
        public void FillsWhilePlayerInZone()
        {
            var obj = Make(duration: 10f);

            obj.Tick(1f, playerPosition: Vector2.zero);

            Assert.AreEqual(0.1f, obj.Normalized, 0.0001f); // 1s of a 10s fill
            Assert.IsFalse(obj.IsComplete);
        }

        [Test]
        public void DoesNotFillWhenPlayerOutOfZone()
        {
            var obj = Make(duration: 10f, radius: 2f);

            obj.Tick(1f, playerPosition: new Vector2(5f, 0f));

            Assert.AreEqual(0f, obj.Normalized, 0.0001f);
            Assert.IsFalse(obj.PlayerInZone);
        }

        [Test]
        public void CompletesAtFull()
        {
            var obj = Make(duration: 2f);

            obj.Tick(1f, Vector2.zero);
            bool completedThisTick = obj.Tick(1.5f, Vector2.zero); // overshoots

            Assert.IsTrue(obj.IsComplete);
            Assert.IsTrue(completedThisTick);
            Assert.AreEqual(1f, obj.Normalized, 0.0001f); // clamped
        }

        [Test]
        public void CompletesOnlyOnce()
        {
            var obj = Make(duration: 1f);

            bool first = obj.Tick(2f, Vector2.zero);
            bool second = obj.Tick(2f, Vector2.zero);

            Assert.IsTrue(first);
            Assert.IsFalse(second); // already complete — no repeat completion
        }

        [Test]
        public void FillRateScaleStat_SpeedsTheFill()
        {
            var obj = Make(duration: 10f);
            var src = ModifierSource.Create("priority-override");
            _stats.Run.AddModifier(StatId.ObjectiveFillRateScale, StatOp.PctAdd, 1f, src); // ×2 rate

            obj.Tick(1f, Vector2.zero);

            Assert.AreEqual(0.2f, obj.Normalized, 0.0001f); // 1s at 2× a 10s fill
        }

        [Test]
        public void Radius_UsesEuclideanDistance()
        {
            var obj = Make(duration: 10f, radius: 2f);

            obj.Tick(1f, new Vector2(1.4f, 1.4f)); // dist ~1.98 < 2 → inside

            Assert.IsTrue(obj.PlayerInZone);
            Assert.Greater(obj.Normalized, 0f);
        }
    }
}
