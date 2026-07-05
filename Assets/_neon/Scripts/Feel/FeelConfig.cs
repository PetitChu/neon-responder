using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>One feedback profile: a hitstop dip (clock scale for a moment) + a camera shake.</summary>
    [System.Serializable]
    public struct HitProfile
    {
        [Range(0f, 1f)] public float HitstopScale;   // 0.05 = near-freeze; 1 = none
        public float HitstopSeconds;                  // unscaled
        public float ShakeIntensity;
        public float ShakeSeconds;

        public HitProfile(float hitstopScale, float hitstopSeconds, float shakeIntensity, float shakeSeconds)
        {
            HitstopScale = hitstopScale;
            HitstopSeconds = hitstopSeconds;
            ShakeIntensity = shakeIntensity;
            ShakeSeconds = shakeSeconds;
        }
    }

    /// <summary>Asset-free snapshot of FeelSettings + the per-verb profile selector (EditMode-testable).</summary>
    public readonly struct FeelConfig
    {
        public readonly HitProfile Punch;
        public readonly HitProfile Kick;
        public readonly HitProfile Weapon;
        public readonly HitProfile Throw;    // throw-enemy = the biggest hit in the kit (§0.4.f)
        public readonly HitProfile DefaultHit;
        public readonly HitProfile Finish;
        public readonly HitProfile TierUp;
        public readonly float FinisherFreezeSeconds;
        public readonly float WhiffFlashSeconds;

        public FeelConfig(HitProfile punch, HitProfile kick, HitProfile weapon, HitProfile @throw,
            HitProfile defaultHit, HitProfile finish, HitProfile tierUp,
            float finisherFreezeSeconds, float whiffFlashSeconds)
        {
            Punch = punch; Kick = kick; Weapon = weapon; Throw = @throw;
            DefaultHit = defaultHit; Finish = finish; TierUp = tierUp;
            FinisherFreezeSeconds = finisherFreezeSeconds; WhiffFlashSeconds = whiffFlashSeconds;
        }

        /// <summary>Pick the profile for a landed verb (throw is the heaviest; grab-punch/kick map to their base).</summary>
        public HitProfile ProfileForVerb(ATTACKTYPE attackType)
        {
            switch (attackType)
            {
                case ATTACKTYPE.PUNCH:
                case ATTACKTYPE.GRABPUNCH:
                    return Punch;
                case ATTACKTYPE.KICK:
                case ATTACKTYPE.GRABKICK:
                    return Kick;
                case ATTACKTYPE.WEAPON:
                    return Weapon;
                case ATTACKTYPE.GRABTHROW:
                    return Throw;
                default:
                    return DefaultHit;
            }
        }

        public static FeelConfig FromSettings()
        {
            var s = FeelSettingsAsset.InstanceAsset.Settings;
            return new FeelConfig(s.Punch, s.Kick, s.Weapon, s.Throw, s.DefaultHit, s.Finish, s.TierUp,
                s.FinisherFreezeSeconds, s.WhiffFlashSeconds);
        }
    }
}
