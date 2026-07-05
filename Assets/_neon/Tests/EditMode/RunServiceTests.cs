using System.Collections.Generic;
using NUnit.Framework;
using R3;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    public class RunServiceTests
    {
        private GameplayClock _clock;
        private GameplaySignals _signals;
        private StatSystem _stats;
        private FakeEntitiesService _entities;
        private SignalSystem _signal;
        private RunService _run;
        private GameObject _player;
        private readonly List<int> _wavesTriggered = new();
        private readonly List<RunPhaseChanged> _phases = new();
        private RunEnded? _ended;
        private System.IDisposable _phaseSub;
        private System.IDisposable _endSub;

        // 2 encounters, node at origin, tiny reboot for fast tests.
        private static RunConfig TwoEncounterConfig => new(
            enabled: true,
            nodePositions: new[] { Vector2.zero, Vector2.zero },
            nodeRadius: 2f, rebootDurationSeconds: 1f, encounterIntroSeconds: 0.5f,
            dawnValue: 1f, shopHealCost: 25, shopHealAmount: 40, shopPauseScale: 0f);

        [SetUp]
        public void SetUp()
        {
            _clock = new GameplayClock();
            _signals = new GameplaySignals();
            _stats = new StatSystem();
            _stats.Run.SetBase(StatId.GainMultiplier, 1f);
            _entities = new FakeEntitiesService();
            _signal = new SignalSystem(_signals, _stats, dawnValue: 1f, maxSpawnNastinessBonus: 1f);
            _player = new GameObject("Player");
            _entities.Register(_player, UNITTYPE.PLAYER); // at origin → inside every node

            _run = new RunService(_clock, _signals, _stats, _entities, _signal, TwoEncounterConfig);
            _wavesTriggered.Clear();
            _phases.Clear();
            _ended = null;
            _phaseSub = _signals.On<RunPhaseChanged>().Subscribe(e => _phases.Add(e));
            _endSub = _signals.On<RunEnded>().Subscribe(e => _ended = e);
            _run.BeginRun(_wavesTriggered.Add);
        }

        [TearDown]
        public void TearDown()
        {
            _phaseSub?.Dispose();
            _endSub?.Dispose();
            _run.Dispose();
            _signal.Dispose();
            _signals.Dispose();
            Object.DestroyImmediate(_player);
        }

        [Test]
        public void BeginRun_EntersEncounterIntro()
        {
            Assert.AreEqual(RunPhase.EncounterIntro, _run.Phase);
        }

        [Test]
        public void Intro_AdvancesToActive_AfterIntroTime_AndTriggersWave()
        {
            _clock.Advance(0.6f); // > 0.5s intro

            Assert.AreEqual(RunPhase.EncounterActive, _run.Phase);
            CollectionAssert.AreEqual(new[] { 0 }, _wavesTriggered); // encounter 0's Manual wave
        }

        [Test]
        public void Active_FillsObjective_ThenEntersShop_RaisingSignal()
        {
            _clock.Advance(0.6f);  // intro → active
            _clock.Advance(1.1f);  // > 1s reboot → complete

            Assert.AreEqual(RunPhase.Shop, _run.Phase);        // not the last encounter
            Assert.AreEqual(0.5f, _signal.Value, 0.0001f);     // +1/2 per objective
        }

        [Test]
        public void Shop_FreezesTheClock()
        {
            _clock.Advance(0.6f);
            _clock.Advance(1.1f);  // now in Shop

            Assert.AreEqual(0f, _clock.EffectiveScale, 0.0001f); // shopPauseScale 0
        }

        [Test]
        public void ContinueFromShop_ResumesClock_NextEncounter()
        {
            _clock.Advance(0.6f);
            _clock.Advance(1.1f);  // Shop after encounter 0

            _run.ContinueFromShop();

            Assert.AreEqual(1f, _clock.EffectiveScale, 0.0001f); // resumed
            Assert.AreEqual(RunPhase.EncounterIntro, _run.Phase);
            _clock.Advance(0.6f);
            CollectionAssert.AreEqual(new[] { 0, 1 }, _wavesTriggered); // encounter 1's wave
        }

        [Test]
        public void LastObjective_ReachesDawn_WinsViaBossStub()
        {
            _clock.Advance(0.6f); _clock.Advance(1.1f);  // enc0 → Shop
            _run.ContinueFromShop();
            _clock.Advance(0.6f); _clock.Advance(1.1f);  // enc1 → dawn

            Assert.IsTrue(_signal.IsDawn);
            Assert.AreEqual(RunPhase.RunWon, _run.Phase);   // BossStub passes straight through
            Assert.IsTrue(_ended.HasValue);
            Assert.IsTrue(_ended.Value.Won);
        }

        [Test]
        public void PlayerDeath_LosesFromAnyPhase()
        {
            _clock.Advance(0.6f); // active
            _signals.Publish(new PlayerLevelChanged(1)); // noise
            HealthSystemDeath(_player);

            Assert.AreEqual(RunPhase.RunLost, _run.Phase);
            Assert.IsTrue(_ended.HasValue);
            Assert.IsFalse(_ended.Value.Won);
        }

        [Test]
        public void ObjectiveDoesNotFill_WhenPlayerOutOfZone()
        {
            _player.transform.position = new Vector3(50f, 0f, 0f); // far from node
            _clock.Advance(0.6f);  // active, wave triggered
            _clock.Advance(2f);    // would complete if in zone

            Assert.AreEqual(RunPhase.EncounterActive, _run.Phase); // still holding
        }

        // RunService subscribes to HealthSystem.onUnitDeath at runtime, but that static
        // event can't be raised from a test — so drive the same public handler directly
        // (the EconomySystem/FinishResolver test-seam pattern). This avoids the EditMode
        // null-ref from HealthSystem.SubstractHealth touching the injected audio service.
        private void HealthSystemDeath(GameObject unit)
        {
            unit.tag = "Player";
            _run.HandleUnitDeath(unit);
        }
    }
}
