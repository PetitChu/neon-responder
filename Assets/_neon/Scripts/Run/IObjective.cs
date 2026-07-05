using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// A run objective (spec §5.4). MVP impl is RebootNodeObjective; Rescue /
    /// Purge-Jammer / Hold-the-Line are later impls of this same seam.
    /// </summary>
    public interface IObjective
    {
        float Normalized { get; }
        bool IsComplete { get; }
        Vector2 Position { get; }

        /// <summary>Advance by gameplay dt given the player's position. Returns true on the tick it completes.</summary>
        bool Tick(float deltaTime, Vector2 playerPosition);
    }
}
