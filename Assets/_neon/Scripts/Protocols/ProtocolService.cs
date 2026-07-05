using System.Collections.Generic;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    public sealed class ProtocolService : IProtocolService
    {
        // Doc §8.2 base weights. Signal scaling (×band) arrives with M3's Signal.
        private const float WEIGHT_STOCK = 100f;
        private const float WEIGHT_TUNED = 50f;
        private const float WEIGHT_PROTOTYPE = 18f;
        // Blacksite: 0 until gated + guaranteed-offered — lands with the first Blacksite (M3+).

        private readonly IStatSystem _stats;
        private readonly IGameplaySignals _signals;
        private readonly List<ProtocolDefinitionAsset> _catalog;
        private readonly Dictionary<ProtocolDefinitionAsset, int> _stacks = new();
        private readonly System.Random _random;

        public ProtocolService(IStatSystem stats, IGameplaySignals signals,
            IReadOnlyList<ProtocolDefinitionAsset> catalog, int randomSeed)
        {
            _stats = stats;
            _signals = signals;
            _catalog = catalog != null
                ? new List<ProtocolDefinitionAsset>(catalog)
                : new List<ProtocolDefinitionAsset>();
            _random = randomSeed == 0 ? new System.Random() : new System.Random(randomSeed);
        }

        public IReadOnlyList<ProtocolDefinitionAsset> Catalog => _catalog;

        public int GetStackCount(ProtocolDefinitionAsset protocol)
        {
            return protocol != null && _stacks.TryGetValue(protocol, out int count) ? count : 0;
        }

        public bool IsAvailable(ProtocolDefinitionAsset protocol)
        {
            if (protocol == null) return false;
            if (GetStackCount(protocol) >= Mathf.Max(1, protocol.MaxStacks)) return false;
            // Hidden-tree gating (doc §3): invisible until the prerequisite is stacked.
            if (protocol.Prerequisite != null && GetStackCount(protocol.Prerequisite) == 0) return false;
            return true;
        }

        public IReadOnlyList<ProtocolDefinitionAsset> RollChoices(int count)
        {
            var pool = new List<ProtocolDefinitionAsset>();
            foreach (var protocol in _catalog)
            {
                if (IsAvailable(protocol)) pool.Add(protocol);
            }

            var choices = new List<ProtocolDefinitionAsset>();
            while (choices.Count < count && pool.Count > 0)
            {
                float totalWeight = 0f;
                foreach (var protocol in pool) totalWeight += RarityWeight(protocol.Rarity);

                float roll = (float)(_random.NextDouble() * totalWeight);
                int pickedIndex = pool.Count - 1;
                for (int i = 0; i < pool.Count; i++)
                {
                    roll -= RarityWeight(pool[i].Rarity);
                    if (roll <= 0f)
                    {
                        pickedIndex = i;
                        break;
                    }
                }

                choices.Add(pool[pickedIndex]);
                pool.RemoveAt(pickedIndex); // no duplicates within one draft
            }
            return choices;
        }

        public void Acquire(ProtocolDefinitionAsset protocol)
        {
            if (!IsAvailable(protocol)) return;

            int newStackCount = GetStackCount(protocol) + 1;
            _stacks[protocol] = newStackCount;

            var modifiers = newStackCount == 1 ? protocol.FirstCopyModifiers : protocol.AdditionalCopyModifiers;
            var source = ModifierSource.Create($"protocol:{protocol.name}#{newStackCount}");
            foreach (var modifier in modifiers)
            {
                var sheet = modifier.Sheet == StatSheetTarget.Run ? _stats.Run : _stats.Player;
                sheet.AddModifier(modifier.Stat, modifier.Op, modifier.Value, source);
            }

            _signals.Publish(new ProtocolAcquired(protocol, newStackCount));
        }

        private static float RarityWeight(ProtocolRarity rarity)
        {
            switch (rarity)
            {
                case ProtocolRarity.Stock: return WEIGHT_STOCK;
                case ProtocolRarity.Tuned: return WEIGHT_TUNED;
                case ProtocolRarity.Prototype: return WEIGHT_PROTOTYPE;
                default: return 0f;
            }
        }
    }
}
