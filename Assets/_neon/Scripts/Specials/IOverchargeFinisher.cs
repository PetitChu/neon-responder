namespace BrainlessLabs.Neon
{
    /// <summary>
    /// The Overcharge finisher (spec §5.3, GDD §6.9): a meter-gated screen-clear,
    /// distinct from the cooldown Special. Manual-fire when full (F2). R4: clears chaff.
    /// </summary>
    public interface IOverchargeFinisher
    {
        bool IsReady { get; }
    }
}
