namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Keys for every tunable gameplay number. Extend as systems land
    /// (append only — do not renumber, values may be serialized by Protocol assets later).
    /// </summary>
    public enum StatId
    {
        // Auto-engage (bases set by AutoEngageSystem in M1)
        AutoEngageRate = 0,
        AutoEngageDamage = 1,
        AutoEngageArcDegrees = 2,

        // Cross-cutting multipliers (Momentum registers modifiers here)
        DamageMultiplier = 100,
        GainMultiplier = 101,
    }
}
