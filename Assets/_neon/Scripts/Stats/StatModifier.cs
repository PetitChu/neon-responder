namespace BrainlessLabs.Neon
{
    /// <summary>
    /// One modifier on a stat: operation, value, and the source that owns it.
    /// </summary>
    public readonly struct StatModifier
    {
        public readonly StatOp Op;
        public readonly float Value;
        public readonly ModifierSource Source;

        public StatModifier(StatOp op, float value, ModifierSource source)
        {
            Op = op;
            Value = value;
            Source = source;
        }
    }
}
