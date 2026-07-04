namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Momentum: the skill meter (spec §5.1). Steps on EnemyFinished only;
    /// its tier is the ONLY global multiplier in the game (protocol doc §8.1),
    /// applied as one Mult modifier on Player.DamageMultiplier + Run.GainMultiplier.
    /// </summary>
    public interface IMomentumSystem
    {
        MomentumTier Tier { get; }
    }
}
