using System.Collections.Generic;
using NUnit.Framework;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    public class ProtocolServiceTests
    {
        private StatSystem _stats;
        private GameplaySignals _signals;
        private readonly List<ProtocolDefinitionAsset> _createdAssets = new();

        [SetUp]
        public void SetUp()
        {
            _stats = new StatSystem();
            _signals = new GameplaySignals();
        }

        [TearDown]
        public void TearDown()
        {
            _signals.Dispose();
            foreach (var asset in _createdAssets) Object.DestroyImmediate(asset);
            _createdAssets.Clear();
        }

        private ProtocolDefinitionAsset MakeProtocol(string name, ProtocolRarity rarity, int maxStacks,
            ProtocolDefinitionAsset prerequisite,
            (StatId stat, StatOp op, float value)[] firstCopy,
            (StatId stat, StatOp op, float value)[] additionalCopy = null)
        {
            var asset = ScriptableObject.CreateInstance<ProtocolDefinitionAsset>();
            asset.name = name;
            asset.DisplayName = name;
            asset.Rarity = rarity;
            asset.MaxStacks = maxStacks;
            asset.Prerequisite = prerequisite;
            foreach (var (stat, op, value) in firstCopy)
                asset.FirstCopyModifiers.Add(new ProtocolStatModifier { Sheet = StatSheetTarget.Player, Stat = stat, Op = op, Value = value });
            foreach (var (stat, op, value) in additionalCopy ?? new (StatId, StatOp, float)[0])
                asset.AdditionalCopyModifiers.Add(new ProtocolStatModifier { Sheet = StatSheetTarget.Player, Stat = stat, Op = op, Value = value });
            _createdAssets.Add(asset);
            return asset;
        }

        private ProtocolService MakeService(params ProtocolDefinitionAsset[] catalog)
        {
            return new ProtocolService(_stats, _signals, catalog, randomSeed: 12345);
        }

        [Test]
        public void Acquire_AppliesFirstCopyModifiers()
        {
            _stats.Player.SetBase(StatId.AutoEngageArcDegrees, 120f);
            var wideSweep = MakeProtocol("WideSweep", ProtocolRarity.Stock, 2, null,
                new[] { (StatId.AutoEngageArcDegrees, StatOp.Add, 30f) });
            var service = MakeService(wideSweep);

            service.Acquire(wideSweep);

            Assert.AreEqual(150f, _stats.Player.GetValue(StatId.AutoEngageArcDegrees), 0.0001f);
            Assert.AreEqual(1, service.GetStackCount(wideSweep));
        }

        [Test]
        public void SecondCopy_UsesAdditionalModifiers()
        {
            _stats.Player.SetBase(StatId.HealPerFinish, 0f);
            var vampiric = MakeProtocol("Vampiric", ProtocolRarity.Tuned, 3, null,
                new[] { (StatId.HealPerFinish, StatOp.Add, 2f) },
                new[] { (StatId.HealPerFinish, StatOp.Add, 1f) });
            var service = MakeService(vampiric);

            service.Acquire(vampiric);
            service.Acquire(vampiric);

            Assert.AreEqual(3f, _stats.Player.GetValue(StatId.HealPerFinish), 0.0001f); // 2 + 1
            Assert.AreEqual(2, service.GetStackCount(vampiric));
        }

        [Test]
        public void MaxStacks_BlocksFurtherCopies()
        {
            var unique = MakeProtocol("IronGrip", ProtocolRarity.Tuned, 1, null,
                new[] { (StatId.GrabDurationScale, StatOp.PctAdd, 0.5f) });
            var service = MakeService(unique);

            service.Acquire(unique);
            service.Acquire(unique); // ignored

            Assert.AreEqual(1, service.GetStackCount(unique));
            Assert.IsFalse(service.IsAvailable(unique));
        }

        [Test]
        public void Prerequisite_GatesUntilAcquired()
        {
            var afterburner = MakeProtocol("Afterburner", ProtocolRarity.Tuned, 1, null,
                new[] { (StatId.MomentumDecaySeconds, StatOp.Add, 1.7f) });
            var governor = MakeProtocol("RedlineGovernor", ProtocolRarity.Prototype, 1, afterburner,
                new[] { (StatId.OverdriveMultiplier, StatOp.Add, 0.5f) });
            var service = MakeService(afterburner, governor);

            Assert.IsFalse(service.IsAvailable(governor));
            for (int i = 0; i < 20; i++)
            {
                CollectionAssert.DoesNotContain(service.RollChoices(3), governor);
            }

            service.Acquire(afterburner);

            Assert.IsTrue(service.IsAvailable(governor));
        }

        [Test]
        public void GatedAcquire_IsIgnored()
        {
            var afterburner = MakeProtocol("Afterburner", ProtocolRarity.Tuned, 1, null,
                new[] { (StatId.MomentumDecaySeconds, StatOp.Add, 1.7f) });
            var governor = MakeProtocol("RedlineGovernor", ProtocolRarity.Prototype, 1, afterburner,
                new[] { (StatId.OverdriveMultiplier, StatOp.Add, 0.5f) });
            var service = MakeService(afterburner, governor);

            service.Acquire(governor); // gated → no-op

            Assert.AreEqual(0, service.GetStackCount(governor));
        }

        [Test]
        public void RollChoices_NoDuplicatesWithinOneDraft()
        {
            var a = MakeProtocol("A", ProtocolRarity.Stock, 3, null, new[] { (StatId.HealPerFinish, StatOp.Add, 1f) });
            var b = MakeProtocol("B", ProtocolRarity.Stock, 3, null, new[] { (StatId.HealPerFinish, StatOp.Add, 1f) });
            var c = MakeProtocol("C", ProtocolRarity.Stock, 3, null, new[] { (StatId.HealPerFinish, StatOp.Add, 1f) });
            var service = MakeService(a, b, c);

            var choices = service.RollChoices(3);

            Assert.AreEqual(3, choices.Count);
            CollectionAssert.AllItemsAreUnique(choices);
        }

        [Test]
        public void RollChoices_ShrinksWhenPoolIsExhausted()
        {
            var only = MakeProtocol("Only", ProtocolRarity.Stock, 1, null, new[] { (StatId.HealPerFinish, StatOp.Add, 1f) });
            var service = MakeService(only);

            Assert.AreEqual(1, service.RollChoices(3).Count);

            service.Acquire(only);

            Assert.AreEqual(0, service.RollChoices(3).Count);
        }

        [Test]
        public void Acquire_PublishesProtocolAcquired()
        {
            ProtocolAcquired received = default;
            using var sub = _signals.On<ProtocolAcquired>().Subscribe(e => received = e);
            var wideSweep = MakeProtocol("WideSweep", ProtocolRarity.Stock, 2, null,
                new[] { (StatId.AutoEngageArcDegrees, StatOp.Add, 30f) });
            var service = MakeService(wideSweep);

            service.Acquire(wideSweep);

            Assert.AreEqual(wideSweep, received.Protocol);
            Assert.AreEqual(1, received.StackCount);
        }
    }
}
