namespace BrainlessLabs.Neon
{
    /// <summary>
    /// The MVP Special — Siren Pulse (spec §5.3, special-moves doc §2): a cooldown +
    /// Neon Charge active that manufactures a Finish-Ready wave. Grants no Momentum
    /// itself — the finishes it enables do (v0.4 rule).
    /// </summary>
    public interface ISpecialSystem
    {
        bool CanActivate { get; }
        float CooldownNormalized { get; } // 0 (just fired) → 1 (ready)
    }
}
