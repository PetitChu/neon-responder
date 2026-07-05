using System.Collections.Generic;
using NUnit.Framework;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    public class OverchargeFinisherTests
    {
        private GameplayClock _clock;
        private GameplaySignals _signals;
        private FakeEntitiesService _entities;
        private FakeSwarmBridge _bridge;
        private FakeInputService _input;
        private FakeEconomy _economy;
        private OverchargeFinisher _finisher;
        private GameObject _player;

        private static SpecialConfig TestConfig => new(
            sirenCooldownSeconds: 6f, sirenChargeCost: 20, sirenRadius: 5f,
            finisherRadius: 12f, finisherFreezeSeconds: 0.35f);

        [SetUp]
        public void SetUp()
        {
            _clock = new GameplayClock();
            _signals = new GameplaySignals();
            _entities = new FakeEntitiesService();
            _bridge = new FakeSwarmBridge { FinishAllChaffReturn = 42 };
            _input = new FakeInputService();
            _economy = new FakeEconomy { OverchargeFull = true };
            _player = new GameObject("Player");
            _entities.Register(_player, UNITTYPE.PLAYER);
            _finisher = new OverchargeFinisher(_clock, _signals, _entities, _bridge, _input, _economy, TestConfig);
        }

        [TearDown]
        public void TearDown()
        {
            _finisher.Dispose();
            _signals.Dispose();
            Object.DestroyImmediate(_player);
        }

        [Test]
        public void Ready_WhenMeterFull()
        {
            Assert.IsTrue(_finisher.IsReady);
        }

        [Test]
        public void Fire_WhenReady_ClearsChaff_ConsumesMeter()
        {
            _input.Finisher = true;
            _clock.Advance(0.016f);

            Assert.AreEqual(1, _bridge.FinishAllChaffCalls.Count);
            Assert.AreEqual(12f, _bridge.FinishAllChaffCalls[0].Radius, 0.0001f);
            Assert.IsFalse(_economy.OverchargeFull); // consumed
            Assert.IsFalse(_finisher.IsReady);
        }

        [Test]
        public void Fire_WhenNotFull_DoesNothing()
        {
            _economy.OverchargeFull = false;
            _input.Finisher = true;
            _clock.Advance(0.016f);

            Assert.AreEqual(0, _bridge.FinishAllChaffCalls.Count);
        }

        [Test]
        public void Fire_AppliesFreezeFrame_ThenReleases()
        {
            _input.Finisher = true;
            _clock.Advance(0.016f);

            Assert.AreEqual(0f, _clock.EffectiveScale, 0.0001f); // frozen

            _finisher.Tick(0f); // simulate the unscaled release path (drives the timer)
            // Freeze release is unscaled-timed at runtime; the finisher exposes ReleaseFreezeForTest.
            _finisher.ReleaseFreezeForTest();
            Assert.AreEqual(1f, _clock.EffectiveScale, 0.0001f);
        }

        [Test]
        public void PublishesFinisherFired()
        {
            OverchargeFinisherFired got = default;
            using var sub = _signals.On<OverchargeFinisherFired>().Subscribe(e => got = e);

            _input.Finisher = true;
            _clock.Advance(0.016f);

            Assert.AreEqual(42, got.ChaffCleared);
        }

        [Test]
        public void PublishesReadyChanged_OnConsume()
        {
            var readies = new List<OverchargeReadyChanged>();
            using var sub = _signals.On<OverchargeReadyChanged>().Subscribe(readies.Add);

            _input.Finisher = true;
            _clock.Advance(0.016f);

            Assert.IsTrue(readies.Count >= 1);
            Assert.IsFalse(readies[readies.Count - 1].Ready);
        }
    }
}
