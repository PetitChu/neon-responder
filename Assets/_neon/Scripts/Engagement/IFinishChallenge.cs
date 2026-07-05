namespace BrainlessLabs.Neon
{
    /// <summary>
    /// A per-target, tier-scaled finish challenge (spec §5.1). Chaff use the
    /// implicit single-verb challenge inside the bridge; hero-tier uses
    /// SequenceFinishChallenge. Momentum pays out on COMPLETION only (v0.4).
    /// </summary>
    public interface IFinishChallenge
    {
        ATTACKTYPE ExpectedVerb { get; }
        int Progress { get; }
        int Total { get; }
        bool IsComplete { get; }

        /// <summary>Feed a landed verb hit. Returns true when this hit completes the challenge.</summary>
        bool TryAdvance(ATTACKTYPE verb, float gameplayNow);
    }
}
