using System.Collections.Generic;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// The in-run protocol stack (spec §5.3, protocol doc v0.3 Hard Split:
    /// level-up-draft only). Acquiring = applying the asset's stat-modifier bundle.
    /// </summary>
    public interface IProtocolService
    {
        IReadOnlyList<ProtocolDefinitionAsset> Catalog { get; }
        int GetStackCount(ProtocolDefinitionAsset protocol);

        /// <summary>Below max stacks AND its prerequisite (if any) is in the stack.</summary>
        bool IsAvailable(ProtocolDefinitionAsset protocol);

        /// <summary>Rarity-weighted pick-N (no duplicates within one draft). May return fewer when the pool runs dry.</summary>
        IReadOnlyList<ProtocolDefinitionAsset> RollChoices(int count);

        void Acquire(ProtocolDefinitionAsset protocol);
    }
}
