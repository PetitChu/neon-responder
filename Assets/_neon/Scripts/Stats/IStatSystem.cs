namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Owns the stat sheets. Momentum, Protocols, and the Signal are all just
    /// modifier sources on these sheets — they never reference each other.
    /// </summary>
    public interface IStatSystem
    {
        /// <summary>Per-player stats (auto-engage rate/damage, damage multiplier, ...).</summary>
        StatSheet Player { get; }

        /// <summary>Run/global stats (gain multiplier, spawn nastiness, objective speed, ...).</summary>
        StatSheet Run { get; }
    }
}
