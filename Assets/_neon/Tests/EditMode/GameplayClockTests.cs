using System.Collections.Generic;
using NUnit.Framework;

namespace BrainlessLabs.Neon.Tests
{
    public class GameplayClockTests
    {
        private sealed class RecordingTickable : IGameplayTickable
        {
            private readonly List<string> _log;
            private readonly string _name;
            public float LastDelta { get; private set; }

            public RecordingTickable(string name, List<string> log)
            {
                _name = name;
                _log = log;
            }

            public void Tick(float deltaTime)
            {
                LastDelta = deltaTime;
                _log.Add(_name);
            }
        }

        [Test]
        public void Advance_AccumulatesGameplayTime()
        {
            var clock = new GameplayClock();

            clock.Advance(0.5f);
            clock.Advance(0.5f);

            Assert.AreEqual(1f, clock.GameplayTime, 0.0001f);
            Assert.AreEqual(0.5f, clock.DeltaTime, 0.0001f);
        }

        [Test]
        public void SetScale_ScalesDeltaAndTime()
        {
            var clock = new GameplayClock();
            var slowMo = ModifierSource.Create("slowmo");

            clock.SetScale(slowMo, 0.5f);
            clock.Advance(1f);

            Assert.AreEqual(0.5f, clock.DeltaTime, 0.0001f);
            Assert.AreEqual(0.5f, clock.GameplayTime, 0.0001f);
        }

        [Test]
        public void MultipleScaleSources_Multiply()
        {
            var clock = new GameplayClock();
            clock.SetScale(ModifierSource.Create("hitstop"), 0.5f);
            clock.SetScale(ModifierSource.Create("slowmo"), 0.5f);

            clock.Advance(1f);

            Assert.AreEqual(0.25f, clock.DeltaTime, 0.0001f);
        }

        [Test]
        public void ClearScale_RestoresFullSpeed()
        {
            var clock = new GameplayClock();
            var pause = ModifierSource.Create("pause");
            clock.SetScale(pause, 0f);
            clock.Advance(1f);
            Assert.AreEqual(0f, clock.GameplayTime, 0.0001f);

            clock.ClearScale(pause);
            clock.Advance(1f);

            Assert.AreEqual(1f, clock.GameplayTime, 0.0001f);
        }

        [Test]
        public void Tickables_RunInOrder_NotRegistrationOrder()
        {
            var clock = new GameplayClock();
            var log = new List<string>();
            // Registered out of order on purpose. Spec order: AutoEngage(0) → FinishReady(10) → Selector(20) → MomentumDecay(30).
            clock.Register(new RecordingTickable("selector", log), order: 20);
            clock.Register(new RecordingTickable("autoEngage", log), order: 0);
            clock.Register(new RecordingTickable("finishReady", log), order: 10);

            clock.Advance(0.016f);

            CollectionAssert.AreEqual(new[] { "autoEngage", "finishReady", "selector" }, log);
        }

        [Test]
        public void SameOrder_PreservesRegistrationOrder()
        {
            var clock = new GameplayClock();
            var log = new List<string>();
            clock.Register(new RecordingTickable("first", log), order: 5);
            clock.Register(new RecordingTickable("second", log), order: 5);

            clock.Advance(0.016f);

            CollectionAssert.AreEqual(new[] { "first", "second" }, log);
        }

        [Test]
        public void Tickables_ReceiveScaledDelta()
        {
            var clock = new GameplayClock();
            var log = new List<string>();
            var tickable = new RecordingTickable("t", log);
            clock.Register(tickable, order: 0);
            clock.SetScale(ModifierSource.Create("slowmo"), 0.25f);

            clock.Advance(1f);

            Assert.AreEqual(0.25f, tickable.LastDelta, 0.0001f);
        }

        [Test]
        public void Unregister_StopsTicking()
        {
            var clock = new GameplayClock();
            var log = new List<string>();
            var tickable = new RecordingTickable("t", log);
            clock.Register(tickable, order: 0);
            clock.Advance(0.016f);

            clock.Unregister(tickable);
            clock.Advance(0.016f);

            Assert.AreEqual(1, log.Count);
        }
    }
}
