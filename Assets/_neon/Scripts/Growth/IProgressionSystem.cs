namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Watches XP thresholds → level-up → slow-mo 1-of-3 protocol draft (spec §5.3).
    /// </summary>
    public interface IProgressionSystem
    {
        int Level { get; }

        /// <summary>True while a draft is on screen (slow-mo held).</summary>
        bool AwaitingChoice { get; }

        /// <summary>Apply the drafted pick (0-based). No-op when nothing is pending.</summary>
        void Choose(int index);
    }
}
