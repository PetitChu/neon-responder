namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Three ledgers at three timescales (spec §5.3 / GDD §8). Gains are
    /// ×Run.GainMultiplier internally — the economy never knows what Momentum is.
    /// </summary>
    public interface IEconomySystem
    {
        /// <summary>In-run XP total (kills). Progression levels off XpGained.</summary>
        int Xp { get; }

        /// <summary>Between-encounter currency (finishes). Spent in the M3 Specials shop.</summary>
        int NeonCharge { get; }

        /// <summary>Moment-to-moment meter (finishes). Spent by the M4 Overcharge finisher.</summary>
        int Overcharge { get; }

        /// <summary>Deduct Neon Charge if affordable. Returns false (no change) if too poor.</summary>
        bool TrySpend(int amount);

        /// <summary>True when the Overcharge meter is at cap (the finisher's gate).</summary>
        bool IsOverchargeFull { get; }

        /// <summary>Fire the Overcharge finisher: if the meter is full, zero it and return true.</summary>
        bool TryConsumeOvercharge();
    }
}
