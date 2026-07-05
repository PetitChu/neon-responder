namespace BrainlessLabs.Neon
{
    /// <summary>
    /// The dawn meta-scalar (spec §5.4): 0 → dawn. Objectives raise it; it is a
    /// modifier source on the Run sheet's SpawnNastiness (same pattern as Momentum
    /// on the multipliers). Win = the Signal hits dawn.
    /// </summary>
    public interface ISignalSystem
    {
        float Value { get; }
        float Dawn { get; }
        bool IsDawn { get; }

        void Raise(float amount);
        void Lower(float amount);
    }
}
