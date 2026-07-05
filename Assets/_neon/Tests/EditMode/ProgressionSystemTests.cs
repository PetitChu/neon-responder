using System.Collections.Generic;
using NUnit.Framework;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    public class ProgressionSystemTests
    {
        private GameplayClock _clock;
        private GameplaySignals _signals;
        private StatSystem _stats;
        private ProtocolService _protocols;
        private ProgressionSystem _progression;
        private readonly List<ProtocolDefinitionAsset> _createdAssets = new();
        private readonly List<LevelUpChoicesReady> _offers = new();
        private System.IDisposable _offerSub;

        private static GrowthConfig TestConfig => new(
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
            _stats = new StatSystem();
            _protocols = new ProtocolService(_stats, _signals, MakeCatalog(3), randomSeed: 777);
            _progression = new ProgressionSystem(_signals, _clock, _protocols, TestConfig);
            _offers.Clear();
            _offerSub = _signals.On<LevelUpChoicesReady>().Subscribe(e => _offers.Add(e));
        }

        [TearDown]
        public void TearDown()
        {
            _offerSub?.Dispose();
            _progression.Dispose();
            _signals.Dispose();
            foreach (var asset in _createdAssets) Object.DestroyImmediate(asset);
            _createdAssets.Clear();
        }

        private List<ProtocolDefinitionAsset> MakeCatalog(int count)
        {
            var catalog = new List<ProtocolDefinitionAsset>();
            for (int i = 0; i < count; i++)
            {
                var asset = ScriptableObject.CreateInstance<ProtocolDefinitionAsset>();
                asset.name = $"TestProtocol{i}";
                asset.DisplayName = asset.name;
                asset.Rarity = ProtocolRarity.Stock;
                asset.MaxStacks = 5;
                asset.FirstCopyModifiers.Add(new ProtocolStatModifier
                {
                    Sheet = StatSheetTarget.Player, Stat = StatId.HealPerFinish, Op = StatOp.Add, Value = 1f
                });
                asset.AdditionalCopyModifiers.Add(new ProtocolStatModifier
                {
                    Sheet = StatSheetTarget.Player, Stat = StatId.HealPerFinish, Op = StatOp.Add, Value = 1f
                });
                _createdAssets.Add(asset);
                catalog.Add(asset);
            }
            return catalog;
        }

        private void Xp(int total) => _signals.Publish(new XpGained(1, total));

        [Test]
        public void BelowFirstThreshold_NoLevelUp()
        {
            Xp(9); // cost(1) = ceil(10·1^1.35) = 10

            Assert.AreEqual(1, _progression.Level);
            Assert.IsFalse(_progression.AwaitingChoice);
        }

        [Test]
        public void FirstThreshold_LevelsAndOffersDraft_WithSlowMo()
        {
            Xp(10);

            Assert.AreEqual(2, _progression.Level);
            Assert.IsTrue(_progression.AwaitingChoice);
            Assert.AreEqual(1, _offers.Count);
            Assert.AreEqual(3, _offers[0].Choices.Length);
            Assert.AreEqual(0.1f, _clock.EffectiveScale, 0.0001f);
        }

        [Test]
        public void CurveMatchesDoc_SecondLevelAt36Total()
        {
            // cost(2) = ceil(10·2^1.35) = 26 → level 3 at 10 + 26 = 36 total XP.
            Xp(35);
            _progression.Choose(0); // clear the level-2 draft
            Assert.AreEqual(2, _progression.Level);

            Xp(36);
            Assert.AreEqual(3, _progression.Level);
        }

        [Test]
        public void Choose_Acquires_ClearsSlowMo()
        {
            Xp(10);
            var chosen = _offers[0].Choices[1];

            _progression.Choose(1);

            Assert.AreEqual(1, _protocols.GetStackCount(chosen));
            Assert.IsFalse(_progression.AwaitingChoice);
            Assert.AreEqual(1f, _clock.EffectiveScale, 0.0001f);
        }

        [Test]
        public void MultiLevelJump_BanksOffers_ServesThemSequentially()
        {
            Xp(36); // clears level-1 AND level-2 thresholds in one grant

            Assert.AreEqual(3, _progression.Level);
            Assert.AreEqual(1, _offers.Count);   // one draft at a time

            _progression.Choose(0);

            Assert.AreEqual(2, _offers.Count);   // the banked one follows immediately
            Assert.IsTrue(_progression.AwaitingChoice);
        }

        [Test]
        public void EmptyCatalog_LevelsWithoutDraft_NoSlowMo()
        {
            _progression.Dispose();
            var emptyService = new ProtocolService(_stats, _signals, new List<ProtocolDefinitionAsset>(), randomSeed: 1);
            _progression = new ProgressionSystem(_signals, _clock, emptyService, TestConfig);

            Xp(10);

            Assert.AreEqual(2, _progression.Level);
            Assert.IsFalse(_progression.AwaitingChoice);
            Assert.AreEqual(1f, _clock.EffectiveScale, 0.0001f);
        }

        [Test]
        public void Choose_WithNothingPending_IsIgnored()
        {
            Assert.DoesNotThrow(() => _progression.Choose(0));
            Assert.AreEqual(1, _progression.Level);
        }
    }
}
