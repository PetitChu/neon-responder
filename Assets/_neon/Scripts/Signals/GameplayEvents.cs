using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>Momentum tiers (GDD §9): multipliers ×1.0 / ×1.3 / ×1.7 / ×2.5.</summary>
    public enum MomentumTier
    {
        Cool = 0,
        Warm = 1,
        Hot = 2,
        Overdrive = 3
    }

    /// <summary>A finishing hit completed on a Finish-Ready target. Momentum steps on THIS only (v0.4).</summary>
    public readonly struct EnemyFinished
    {
        public readonly Vector2 Position;
        public readonly bool WasChaff;

        public EnemyFinished(Vector2 position, bool wasChaff)
        {
            Position = position;
            WasChaff = wasChaff;
        }
    }

    public readonly struct MomentumTierChanged
    {
        public readonly MomentumTier Previous;
        public readonly MomentumTier Current;

        public MomentumTierChanged(MomentumTier previous, MomentumTier current)
        {
            Previous = previous;
            Current = current;
        }
    }

    /// <summary>Selector output — the SINGLE prompted target (R7 one-prompt rule) + total ready count.</summary>
    public readonly struct FinishReadyPromptChanged
    {
        public readonly bool HasTarget;
        public readonly Vector2 TargetPosition;
        public readonly ATTACKTYPE SuggestedVerb;
        public readonly int ReadyCount;

        public FinishReadyPromptChanged(bool hasTarget, Vector2 targetPosition, ATTACKTYPE suggestedVerb, int readyCount)
        {
            HasTarget = hasTarget;
            TargetPosition = targetPosition;
            SuggestedVerb = suggestedVerb;
            ReadyCount = readyCount;
        }
    }

    /// <summary>A completed punch/kick/weapon swing that hit nothing (grab whiffs exempt — v0.4).</summary>
    public readonly struct VerbWhiffed
    {
        public readonly ATTACKTYPE AttackType;

        public VerbWhiffed(ATTACKTYPE attackType)
        {
            AttackType = attackType;
        }
    }
}
