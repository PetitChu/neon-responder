using System.Collections.Generic;
using NUnit.Framework;
using R3;
using Unity.Entities;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    public class FinishReadySelectorTests
    {
        private GameplayClock _clock;
        private GameplaySignals _signals;
        private FakeEntitiesService _entities;
        private FakeSwarmBridge _bridge;
        private FinishReadySelector _selector;
        private GameObject _player;
        private readonly List<FinishReadyPromptChanged> _received = new();
        private System.IDisposable _subscription;
        private readonly List<GameObject> _spawned = new();

        [SetUp]
        public void SetUp()
        {
            _clock = new GameplayClock();
            _signals = new GameplaySignals();
            _entities = new FakeEntitiesService();
            _bridge = new FakeSwarmBridge();
            _player = Spawn("TestPlayer", Vector2.zero);
            _entities.Register(_player, UNITTYPE.PLAYER);
            _selector = new FinishReadySelector(_clock, _signals, _entities, _bridge);
            _received.Clear();
            _subscription = _signals.On<FinishReadyPromptChanged>().Subscribe(e => _received.Add(e));
        }

        [TearDown]
        public void TearDown()
        {
            _subscription?.Dispose();
            _selector.Dispose();
            _signals.Dispose();
            foreach (var go in _spawned) Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        private GameObject Spawn(string name, Vector2 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            _spawned.Add(go);
            return go;
        }

        private GameObject SpawnReadyEnemy(Vector2 position)
        {
            var enemy = Spawn("ReadyEnemy", position);
            var marker = enemy.AddComponent<FinishReadyMarker>();
            marker.SetReady(true, Color.yellow);
            _entities.Register(enemy, UNITTYPE.ENEMY);
            return enemy;
        }

        [Test]
        public void NoReadyTargets_PublishesNoTarget()
        {
            _clock.Advance(0.016f);

            Assert.AreEqual(1, _received.Count);
            Assert.IsFalse(_received[0].HasTarget);
            Assert.AreEqual(0, _received[0].ReadyCount);
        }

        [Test]
        public void ReadyHero_IsPrompted()
        {
            SpawnReadyEnemy(new Vector2(2f, 0f));

            _clock.Advance(0.016f);

            Assert.IsTrue(_received[_received.Count - 1].HasTarget);
            Assert.AreEqual(new Vector2(2f, 0f), _received[_received.Count - 1].TargetPosition);
            Assert.AreEqual(1, _received[_received.Count - 1].ReadyCount);
        }

        [Test]
        public void OnePromptOnly_NearestHeroWins()
        {
            SpawnReadyEnemy(new Vector2(5f, 0f));
            SpawnReadyEnemy(new Vector2(1f, 0f));

            _clock.Advance(0.016f);

            var last = _received[_received.Count - 1];
            Assert.AreEqual(new Vector2(1f, 0f), last.TargetPosition); // single prompt, nearest
            Assert.AreEqual(2, last.ReadyCount);                      // but the count shows all
        }

        [Test]
        public void NearerChaff_WinsThePrompt()
        {
            SpawnReadyEnemy(new Vector2(5f, 0f));
            _bridge.NearestFinishReady = new TargetRef(new Entity { Index = 1, Version = 1 }, new Vector2(1f, 0f));
            _bridge.FinishReadyCount = 3;

            _clock.Advance(0.016f);

            var last = _received[_received.Count - 1];
            Assert.AreEqual(new Vector2(1f, 0f), last.TargetPosition);
            Assert.AreEqual(4, last.ReadyCount); // 1 hero + 3 chaff
        }

        [Test]
        public void UnchangedState_DoesNotRepublish()
        {
            SpawnReadyEnemy(new Vector2(2f, 0f));

            _clock.Advance(0.016f);
            _clock.Advance(0.016f);
            _clock.Advance(0.016f);

            Assert.AreEqual(1, _received.Count);
        }
    }
}
