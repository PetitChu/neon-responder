using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>Asset-free snapshot of EngagementSettings for the per-level systems (EditMode-testable).</summary>
    public readonly struct EngagementConfig
    {
        public readonly float AutoEngageRatePerSecond;
        public readonly int AutoEngageChipDamage;
        public readonly float AutoEngageArcDegrees;
        public readonly float AutoEngageRange;
        public readonly float FinishReadyHealthThreshold;
        public readonly Color FinishReadyGlow;
        public readonly float WhiffStaggerSeconds;

        public EngagementConfig(float ratePerSecond, int chipDamage, float arcDegrees, float range,
            float finishReadyThreshold, Color finishReadyGlow, float whiffStaggerSeconds)
        {
            AutoEngageRatePerSecond = ratePerSecond;
            AutoEngageChipDamage = chipDamage;
            AutoEngageArcDegrees = arcDegrees;
            AutoEngageRange = range;
            FinishReadyHealthThreshold = finishReadyThreshold;
            FinishReadyGlow = finishReadyGlow;
            WhiffStaggerSeconds = whiffStaggerSeconds;
        }

        public static EngagementConfig FromSettings()
        {
            var s = EngagementSettingsAsset.InstanceAsset.Settings;
            return new EngagementConfig(s.AutoEngageRatePerSecond, s.AutoEngageChipDamage,
                s.AutoEngageArcDegrees, s.AutoEngageRange, s.FinishReadyHealthThreshold,
                s.FinishReadyGlow, s.WhiffStaggerSeconds);
        }
    }

    /// <summary>Asset-free snapshot of the Momentum tuning.</summary>
    public readonly struct MomentumConfig
    {
        public readonly int StepsPerTier;
        public readonly float DecaySeconds;
        public readonly float[] TierMultipliers;

        public MomentumConfig(int stepsPerTier, float decaySeconds, float[] tierMultipliers)
        {
            StepsPerTier = stepsPerTier;
            DecaySeconds = decaySeconds;
            TierMultipliers = tierMultipliers;
        }

        public static MomentumConfig FromSettings()
        {
            var s = EngagementSettingsAsset.InstanceAsset.Settings;
            return new MomentumConfig(s.MomentumStepsPerTier, s.MomentumDecaySeconds, s.MomentumTierMultipliers);
        }
    }
}
