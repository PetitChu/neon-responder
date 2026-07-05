using System.Collections.Generic;
using NUnit.Framework;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    public class SpecialSystemTests
    {
        private GameplayClock _clock;
        private GameplaySignals _signals;
        private FakeEntitiesService _entities;
        private FakeSwarmBridge _bridge;
        private FakeInputService _input;
        private FakeEconomy _economy;
        private SpecialSystem _special;
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
            _bridge = new FakeSwarmBridge();
            _input = new FakeInputService();
            _economy = new FakeEconomy { NeonChargeValue = 100 };
            _player = new GameObject("Player");
            _entities.Register(_player, UNITTYPE.PLAYER);
            _special = new SpecialSystem(_clock, _signals, _entities, _bridge, _input, _economy, TestConfig);
        }

        [TearDown]
        public void TearDown()
        {
            _special.Dispose();
            _signals.Dispose();
            Object.DestroyImmediate(_player);
        }

        [Test]
        public void StartsReady()
        {
            Assert.IsTrue(_special.CanActivate);
            Assert.AreEqual(1f, _special.CooldownNormalized, 0.0001f);
        }

        [Test]
        public void Activate_SpendsCharge_FiresMassFinishReady()
        {
            _input.Special = true;
            _clock.Advance(0.016f);

            Assert.AreEqual(80, _economy.NeonChargeValue);          // 100 - 20
            Assert.AreEqual(1, _bridge.MassFinishReadyCalls.Count);
            Assert.AreEqual(5f, _bridge.MassFinishReadyCalls[0].Radius, 0.0001f);
            Assert.IsFalse(_special.CanActivate);                    // now on cooldown
        }

        [Test]
        public void Activate_WhenTooPoor_DoesNothing()
        {
            _economy.NeonChargeValue = 10; // < 20
            _input.Special = true;
            _clock.Advance(0.016f);

            Assert.AreEqual(10, _economy.NeonChargeValue);
            Assert.AreEqual(0, _bridge.MassFinishReadyCalls.Count);
            Assert.IsTrue(_special.CanActivate); // stayed ready
        }

        [Test]
        public void Cooldown_BlocksReactivation_ThenRecovers()
        {
            _input.Special = true;
            _clock.Advance(0.016f);   // fired
            _input.Special = true;
            _clock.Advance(0.016f);   // still cooling — ignored
            Assert.AreEqual(1, _bridge.MassFinishReadyCalls.Count);

            _clock.Advance(6f);       // full cooldown
            Assert.IsTrue(_special.CanActivate);

            _input.Special = true;
            _clock.Advance(0.016f);
            Assert.AreEqual(2, _bridge.MassFinishReadyCalls.Count);
        }

        [Test]
        public void PublishesSpecialStateChanged_OnFireAndRecover()
        {
            var states = new List<SpecialStateChanged>();
            using var sub = _signals.On<SpecialStateChanged>().Subscribe(states.Add);

            _input.Special = true;
            _clock.Advance(0.016f);              // fire → Ready false
            _clock.Advance(6f);                  // recover → Ready true

            Assert.IsTrue(states.Count >= 2);
            Assert.IsFalse(states[0].Ready);     // first emit on fire
            Assert.IsTrue(states[states.Count - 1].Ready);
        }

        [Test]
        public void PublishesCallout_OnFire()
        {
            Callout got = default;
            using var sub = _signals.On<Callout>().Subscribe(e => got = e);

            _input.Special = true;
            _clock.Advance(0.016f);

            Assert.AreEqual("SIREN PULSE", got.Text);
        }
    }
}
