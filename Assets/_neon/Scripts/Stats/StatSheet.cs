using System.Collections.Generic;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// A keyed collection of stats. Systems query folded values; modifier owners
    /// add/remove modifiers by source handle. Pure C#, no Unity dependencies.
    /// Unset stats read as 0 (base 0, no modifiers).
    /// </summary>
    public sealed class StatSheet
    {
        private readonly Dictionary<StatId, Stat> _stats = new();

        public float GetValue(StatId id)
        {
            return _stats.TryGetValue(id, out var stat) ? stat.Value : 0f;
        }

        public float GetBase(StatId id)
        {
            return _stats.TryGetValue(id, out var stat) ? stat.BaseValue : 0f;
        }

        public void SetBase(StatId id, float value)
        {
            GetOrCreate(id).BaseValue = value;
        }

        public void AddModifier(StatId id, StatOp op, float value, ModifierSource source)
        {
            GetOrCreate(id).Add(new StatModifier(op, value, source));
        }

        /// <summary>
        /// Removes every modifier owned by <paramref name="source"/> across all stats.
        /// Returns the number of modifiers removed.
        /// </summary>
        public int RemoveBySource(ModifierSource source)
        {
            int total = 0;
            foreach (var stat in _stats.Values)
            {
                total += stat.RemoveBySource(source);
            }
            return total;
        }

        private Stat GetOrCreate(StatId id)
        {
            if (!_stats.TryGetValue(id, out var stat))
            {
                stat = new Stat();
                _stats[id] = stat;
            }
            return stat;
        }
    }
}
