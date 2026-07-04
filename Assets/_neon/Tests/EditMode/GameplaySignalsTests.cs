using System;
using NUnit.Framework;
using R3;

namespace BrainlessLabs.Neon.Tests
{
    public class GameplaySignalsTests
    {
        private readonly struct TestSignalA
        {
            public readonly int Value;
            public TestSignalA(int value) { Value = value; }
        }

        private readonly struct TestSignalB
        {
            public readonly int Value;
            public TestSignalB(int value) { Value = value; }
        }

        [Test]
        public void Publish_DeliversPayloadToSubscriber()
        {
            using var signals = new GameplaySignals();
            int received = 0;
            using var subscription = signals.On<TestSignalA>().Subscribe(s => received = s.Value);

            signals.Publish(new TestSignalA(42));

            Assert.AreEqual(42, received);
        }

        [Test]
        public void Publish_DifferentSignalTypes_AreIsolated()
        {
            using var signals = new GameplaySignals();
            int aCount = 0;
            int bCount = 0;
            using var subA = signals.On<TestSignalA>().Subscribe(_ => aCount++);
            using var subB = signals.On<TestSignalB>().Subscribe(_ => bCount++);

            signals.Publish(new TestSignalA(1));

            Assert.AreEqual(1, aCount);
            Assert.AreEqual(0, bCount);
        }

        [Test]
        public void MultipleSubscribers_AllReceive()
        {
            using var signals = new GameplaySignals();
            int first = 0;
            int second = 0;
            using var sub1 = signals.On<TestSignalA>().Subscribe(s => first = s.Value);
            using var sub2 = signals.On<TestSignalA>().Subscribe(s => second = s.Value);

            signals.Publish(new TestSignalA(7));

            Assert.AreEqual(7, first);
            Assert.AreEqual(7, second);
        }

        [Test]
        public void DisposedSubscription_StopsReceiving()
        {
            using var signals = new GameplaySignals();
            int count = 0;
            var subscription = signals.On<TestSignalA>().Subscribe(_ => count++);

            signals.Publish(new TestSignalA(1));
            subscription.Dispose();
            signals.Publish(new TestSignalA(2));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Publish_WithNoSubscribers_DoesNotThrow()
        {
            using var signals = new GameplaySignals();

            Assert.DoesNotThrow(() => signals.Publish(new TestSignalA(1)));
        }
    }
}
