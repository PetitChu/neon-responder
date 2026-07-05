using System.Collections.Generic;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// One stat modifier a protocol applies (guardrail §8.1: Add/PctAdd only —
    /// Momentum is the game's single multiplier; never author Mult here).
    /// </summary>
    [System.Serializable]
    public class ProtocolStatModifier
    {
        public StatSheetTarget Sheet = StatSheetTarget.Player;
        public StatId Stat;
        public StatOp Op = StatOp.Add;
        public float Value;
    }

    /// <summary>
    /// A data-driven upgrade (spec §5.3): a bundle of stat modifiers + stacking +
    /// an optional prerequisite (hidden-tree gating, protocol doc §3). Applying one
    /// is IProtocolService.Acquire — adding a Protocol to the game is authoring an
    /// asset, not writing system code.
    /// </summary>
    [CreateAssetMenu(fileName = "Protocol", menuName = "Neon/Protocols/Protocol Definition")]
    public class ProtocolDefinitionAsset : ScriptableObject
    {
        [Tooltip("Card title, e.g. \"Overclocked Coil\".")]
        public string DisplayName;

        public ProtocolFamily Family;
        public ProtocolRarity Rarity;

        [TextArea]
        [Tooltip("Card body — what the player reads on the level-up pick.")]
        public string Description;

        [Tooltip("1 = unique (behavior protocols). >1 = flat-bump stackable (doc §8.1 rule 6).")]
        public int MaxStacks = 1;

        [Tooltip("Hidden-tree gate (doc §3, Requires-X): not offered until this protocol is in the stack. N-of-family / pair gates land with the first Blacksite (M3+).")]
        public ProtocolDefinitionAsset Prerequisite;

        [Tooltip("Modifiers applied by the FIRST copy.")]
        public List<ProtocolStatModifier> FirstCopyModifiers = new();

        [Tooltip("Modifiers applied by each ADDITIONAL copy (lets stacks diminish per doc §8.4, e.g. Vampiric +2 then +1).")]
        public List<ProtocolStatModifier> AdditionalCopyModifiers = new();
    }
}
