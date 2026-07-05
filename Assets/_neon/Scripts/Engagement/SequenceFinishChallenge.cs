using System.Collections.Generic;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Land the verb sequence on the target within a per-input window. A stale or
    /// wrong input resets — but a hit matching the sequence START re-opens a fresh
    /// attempt at step 1 (missed inputs shouldn't feel like a dead target).
    /// </summary>
    public sealed class SequenceFinishChallenge : IFinishChallenge
    {
        private readonly IReadOnlyList<ATTACKTYPE> _sequence;
        private readonly float _inputWindowSeconds;
        private float _lastAdvanceTime;

        public int Progress { get; private set; }
        public int Total => _sequence.Count;
        public bool IsComplete => Progress >= Total;
        public ATTACKTYPE ExpectedVerb => IsComplete ? ATTACKTYPE.NONE : _sequence[Progress];

        public SequenceFinishChallenge(IReadOnlyList<ATTACKTYPE> sequence, float inputWindowSeconds, float startTime)
        {
            _sequence = sequence;
            _inputWindowSeconds = inputWindowSeconds;
            _lastAdvanceTime = startTime;
        }

        public bool TryAdvance(ATTACKTYPE verb, float gameplayNow)
        {
            if (IsComplete) return false;

            bool expired = Progress > 0 && gameplayNow - _lastAdvanceTime > _inputWindowSeconds;
            if (expired || verb != ExpectedVerb)
            {
                Progress = verb == _sequence[0] ? 1 : 0;
                _lastAdvanceTime = gameplayNow;
                return IsComplete; // true only for degenerate 1-length sequences
            }

            Progress++;
            _lastAdvanceTime = gameplayNow;
            return IsComplete;
        }
    }
}
