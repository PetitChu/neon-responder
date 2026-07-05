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

    /// <summary>A chaff agent died (any cause — a finish also emits its death). Kill XP hangs off this.</summary>
    public readonly struct ChaffDied
    {
        public readonly Vector2 Position;

        public ChaffDied(Vector2 position)
        {
            Position = position;
        }
    }

    /// <summary>Raw XP grant from the economy (already Momentum-multiplied).</summary>
    public readonly struct XpGained
    {
        public readonly int Amount;
        public readonly int TotalXp;

        public XpGained(int amount, int totalXp)
        {
            Amount = amount;
            TotalXp = totalXp;
        }
    }

    /// <summary>Progression's view for the HUD bar: position within the current level.</summary>
    public readonly struct XpProgressChanged
    {
        public readonly int Level;
        public readonly int XpIntoLevel;
        public readonly int XpForNextLevel;

        public XpProgressChanged(int level, int xpIntoLevel, int xpForNextLevel)
        {
            Level = level;
            XpIntoLevel = xpIntoLevel;
            XpForNextLevel = xpForNextLevel;
        }
    }

    public readonly struct PlayerLevelChanged
    {
        public readonly int Level;

        public PlayerLevelChanged(int level)
        {
            Level = level;
        }
    }

    public readonly struct NeonChargeChanged
    {
        public readonly int Total;

        public NeonChargeChanged(int total)
        {
            Total = total;
        }
    }

    public readonly struct OverchargeChanged
    {
        public readonly int Value;
        public readonly int Cap;

        public OverchargeChanged(int value, int cap)
        {
            Value = value;
            Cap = cap;
        }
    }

    /// <summary>Level-up: pick 1 of 3 (slow-mo held until Choose).</summary>
    public readonly struct LevelUpChoicesReady
    {
        public readonly int Level;
        public readonly ProtocolDefinitionAsset[] Choices;

        public LevelUpChoicesReady(int level, ProtocolDefinitionAsset[] choices)
        {
            Level = level;
            Choices = choices;
        }
    }

    public readonly struct ProtocolAcquired
    {
        public readonly ProtocolDefinitionAsset Protocol;
        public readonly int StackCount;

        public ProtocolAcquired(ProtocolDefinitionAsset protocol, int stackCount)
        {
            Protocol = protocol;
            StackCount = stackCount;
        }
    }

    /// <summary>Hero-tier finish-challenge state for the HUD prompt (chaff stay single-verb).</summary>
    public readonly struct FinishChallengeChanged
    {
        public readonly bool Active;
        public readonly Vector2 TargetPosition;
        public readonly ATTACKTYPE ExpectedVerb;
        public readonly int Progress;
        public readonly int Total;

        public FinishChallengeChanged(bool active, Vector2 targetPosition, ATTACKTYPE expectedVerb, int progress, int total)
        {
            Active = active;
            TargetPosition = targetPosition;
            ExpectedVerb = expectedVerb;
            Progress = progress;
            Total = total;
        }
    }
}
