using System;
using System.Threading;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Opaque handle identifying who added a modifier (a Protocol, Momentum, the Signal, ...).
    /// Also used by IGameplayClock as a time-scale source handle.
    /// </summary>
    public readonly struct ModifierSource : IEquatable<ModifierSource>
    {
        private static int s_nextId;

        public readonly int Id;
        public readonly string DebugName;

        private ModifierSource(int id, string debugName)
        {
            Id = id;
            DebugName = debugName;
        }

        public static ModifierSource Create(string debugName)
        {
            return new ModifierSource(Interlocked.Increment(ref s_nextId), debugName);
        }

        public bool Equals(ModifierSource other) => Id == other.Id;
        public override bool Equals(object obj) => obj is ModifierSource other && Equals(other);
        public override int GetHashCode() => Id;
        public override string ToString() => $"{DebugName}#{Id}";
    }
}
