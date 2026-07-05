using NUnit.Framework;

namespace BrainlessLabs.Neon.Tests
{
    public class SequenceFinishChallengeTests
    {
        private static readonly ATTACKTYPE[] PunchKick = { ATTACKTYPE.PUNCH, ATTACKTYPE.KICK };

        [Test]
        public void ExpectedVerb_Advances()
        {
            var challenge = new SequenceFinishChallenge(PunchKick, inputWindowSeconds: 0.9f, startTime: 0f);

            bool completed = challenge.TryAdvance(ATTACKTYPE.PUNCH, 0.1f);

            Assert.IsFalse(completed);
            Assert.AreEqual(1, challenge.Progress);
            Assert.AreEqual(ATTACKTYPE.KICK, challenge.ExpectedVerb);
        }

        [Test]
        public void FullSequence_Completes()
        {
            var challenge = new SequenceFinishChallenge(PunchKick, 0.9f, 0f);

            challenge.TryAdvance(ATTACKTYPE.PUNCH, 0.1f);
            bool completed = challenge.TryAdvance(ATTACKTYPE.KICK, 0.5f);

            Assert.IsTrue(completed);
            Assert.IsTrue(challenge.IsComplete);
        }

        [Test]
        public void WrongVerb_MatchingSequenceStart_RestartsAtOne()
        {
            var challenge = new SequenceFinishChallenge(PunchKick, 0.9f, 0f);
            challenge.TryAdvance(ATTACKTYPE.PUNCH, 0.1f);

            challenge.TryAdvance(ATTACKTYPE.PUNCH, 0.3f); // expected KICK

            Assert.AreEqual(1, challenge.Progress); // the punch re-opens a fresh attempt
        }

        [Test]
        public void WrongVerb_NotSequenceStart_ResetsToZero()
        {
            var challenge = new SequenceFinishChallenge(PunchKick, 0.9f, 0f);
            challenge.TryAdvance(ATTACKTYPE.PUNCH, 0.1f);

            challenge.TryAdvance(ATTACKTYPE.WEAPON, 0.3f);

            Assert.AreEqual(0, challenge.Progress);
        }

        [Test]
        public void ExpiredWindow_ResetsBeforeEvaluating()
        {
            var challenge = new SequenceFinishChallenge(PunchKick, 0.9f, 0f);
            challenge.TryAdvance(ATTACKTYPE.PUNCH, 0.1f);

            bool completed = challenge.TryAdvance(ATTACKTYPE.KICK, 2f); // 1.9s later — stale

            Assert.IsFalse(completed);
            Assert.AreEqual(0, challenge.Progress); // KICK isn't the sequence start
        }
    }
}
