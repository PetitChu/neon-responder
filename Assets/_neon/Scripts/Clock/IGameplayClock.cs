namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Sole owner of gameplay tick and gameplay time. All hitstop / slow-mo /
    /// pause effects route through scale sources here (product of all scales).
    /// Lower <c>order</c> ticks earlier; ties tick in registration order.
    /// Reserved order bands (spec §4.1): AutoEngage 0, FinishReady eval 10,
    /// selector 20, Momentum decay 30.
    /// </summary>
    public interface IGameplayClock
    {
        /// <summary>Accumulated gameplay-scaled time in seconds.</summary>
        float GameplayTime { get; }

        /// <summary>Gameplay-scaled delta of the most recent tick.</summary>
        float DeltaTime { get; }

        /// <summary>Product of all active scale sources (1 when none).</summary>
        float EffectiveScale { get; }

        void Register(IGameplayTickable tickable, int order);
        void Unregister(IGameplayTickable tickable);

        /// <summary>Set (or update) this source's time scale. 0 = paused, 1 = full speed.</summary>
        void SetScale(ModifierSource source, float scale);

        /// <summary>Remove this source's contribution.</summary>
        void ClearScale(ModifierSource source);
    }
}
