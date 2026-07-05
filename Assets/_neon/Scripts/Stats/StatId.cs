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
        AutoEngageRange = 3,

        // Cross-cutting multipliers (Momentum registers modifiers here)
        DamageMultiplier = 100,
        GainMultiplier = 101,

        // Momentum knobs (bases seeded by MomentumSystem; Protocols modify via modifiers)
        MomentumDecaySeconds = 200,
        MomentumBonusStepsBelowHot = 201,
        OverdriveMultiplier = 202,

        // Growth-layer derived knobs (bases seeded + consumed by ProtocolEffectsSystem)
        PlayerMaxHealthPct = 300,
        GrabDurationScale = 301,
        FinishAoeRadius = 302,
        HealPerFinish = 303,

        // Run layer (M3)
        SpawnNastiness = 400,       // Run sheet; base 1, Signal raises it, SwarmBridge reads it
        ObjectiveFillRateScale = 401, // Run sheet; base 1, Objective protocols / Priority Override tune it
    }
}
