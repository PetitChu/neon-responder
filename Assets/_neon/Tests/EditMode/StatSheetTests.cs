using NUnit.Framework;

namespace BrainlessLabs.Neon.Tests
{
    public class StatSheetTests
    {
        [Test]
        public void GetValue_UnsetStat_ReturnsZero()
        {
            var sheet = new StatSheet();

            Assert.AreEqual(0f, sheet.GetValue(StatId.AutoEngageDamage));
        }

        [Test]
        public void GetValue_BaseOnly_ReturnsBase()
        {
            var sheet = new StatSheet();
            sheet.SetBase(StatId.AutoEngageDamage, 8f);

            Assert.AreEqual(8f, sheet.GetValue(StatId.AutoEngageDamage));
        }

        [Test]
        public void Fold_Order_Is_BasePlusAdd_TimesPctAdd_TimesMult()
        {
            var sheet = new StatSheet();
            var src = ModifierSource.Create("test");
            sheet.SetBase(StatId.AutoEngageDamage, 10f);
            sheet.AddModifier(StatId.AutoEngageDamage, StatOp.Add, 5f, src);      // (10 + 5)
            sheet.AddModifier(StatId.AutoEngageDamage, StatOp.PctAdd, 0.2f, src); // × 1.2 = 18
            sheet.AddModifier(StatId.AutoEngageDamage, StatOp.Mult, 2f, src);     // × 2   = 36

            Assert.AreEqual(36f, sheet.GetValue(StatId.AutoEngageDamage), 0.0001f);
        }

        [Test]
        public void PctAdd_StacksAdditively()
        {
            var sheet = new StatSheet();
            var src = ModifierSource.Create("test");
            sheet.SetBase(StatId.GainMultiplier, 10f);
            sheet.AddModifier(StatId.GainMultiplier, StatOp.PctAdd, 0.1f, src);
            sheet.AddModifier(StatId.GainMultiplier, StatOp.PctAdd, 0.1f, src);

            // 10 × (1 + 0.1 + 0.1) = 12, NOT 10 × 1.1 × 1.1 = 12.1
            Assert.AreEqual(12f, sheet.GetValue(StatId.GainMultiplier), 0.0001f);
        }

        [Test]
        public void Mult_StacksMultiplicatively()
        {
            var sheet = new StatSheet();
            var src = ModifierSource.Create("test");
            sheet.SetBase(StatId.DamageMultiplier, 10f);
            sheet.AddModifier(StatId.DamageMultiplier, StatOp.Mult, 2f, src);
            sheet.AddModifier(StatId.DamageMultiplier, StatOp.Mult, 2f, src);

            Assert.AreEqual(40f, sheet.GetValue(StatId.DamageMultiplier), 0.0001f);
        }

        [Test]
        public void RemoveBySource_RemovesOnlyThatSource_AcrossAllStats()
        {
            var sheet = new StatSheet();
            var momentum = ModifierSource.Create("momentum");
            var protocol = ModifierSource.Create("protocol");
            sheet.SetBase(StatId.DamageMultiplier, 10f);
            sheet.SetBase(StatId.GainMultiplier, 10f);
            sheet.AddModifier(StatId.DamageMultiplier, StatOp.Add, 5f, momentum);
            sheet.AddModifier(StatId.GainMultiplier, StatOp.Add, 5f, momentum);
            sheet.AddModifier(StatId.DamageMultiplier, StatOp.Add, 3f, protocol);

            int removed = sheet.RemoveBySource(momentum);

            Assert.AreEqual(2, removed);
            Assert.AreEqual(13f, sheet.GetValue(StatId.DamageMultiplier), 0.0001f); // protocol's +3 survives
            Assert.AreEqual(10f, sheet.GetValue(StatId.GainMultiplier), 0.0001f);
        }

        [Test]
        public void SetBase_AfterModifiers_Refolds()
        {
            var sheet = new StatSheet();
            var src = ModifierSource.Create("test");
            sheet.SetBase(StatId.AutoEngageRate, 1f);
            sheet.AddModifier(StatId.AutoEngageRate, StatOp.Mult, 2f, src);
            Assert.AreEqual(2f, sheet.GetValue(StatId.AutoEngageRate), 0.0001f);

            sheet.SetBase(StatId.AutoEngageRate, 3f);

            Assert.AreEqual(6f, sheet.GetValue(StatId.AutoEngageRate), 0.0001f);
        }
    }
}
