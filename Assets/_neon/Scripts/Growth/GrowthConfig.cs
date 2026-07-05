namespace BrainlessLabs.Neon
{
    /// <summary>Asset-free snapshot of GrowthSettings (EditMode-testable systems).</summary>
    public readonly struct GrowthConfig
    {
        public readonly int XpPerKill;
        public readonly int ChargePerFinish;
        public readonly int OverchargePerFinish;
        public readonly int OverchargeCap;
        public readonly float XpCostBase;
        public readonly float XpCostExponent;
        public readonly float LevelUpSlowMoScale;
        public readonly ATTACKTYPE[] ChallengeSequenceBase;
        public readonly ATTACKTYPE[] ChallengeSequenceHot;
        public readonly float ChallengeInputWindowSeconds;
        public readonly float ChallengeWindowTightenPerTier;
        public readonly int FinishAoeDamage;

        public GrowthConfig(int xpPerKill, int chargePerFinish, int overchargePerFinish, int overchargeCap,
            float xpCostBase, float xpCostExponent, float levelUpSlowMoScale,
            ATTACKTYPE[] challengeSequenceBase, ATTACKTYPE[] challengeSequenceHot,
            float challengeInputWindowSeconds, float challengeWindowTightenPerTier, int finishAoeDamage)
        {
            XpPerKill = xpPerKill;
            ChargePerFinish = chargePerFinish;
            OverchargePerFinish = overchargePerFinish;
            OverchargeCap = overchargeCap;
            XpCostBase = xpCostBase;
            XpCostExponent = xpCostExponent;
            LevelUpSlowMoScale = levelUpSlowMoScale;
            ChallengeSequenceBase = challengeSequenceBase;
            ChallengeSequenceHot = challengeSequenceHot;
            ChallengeInputWindowSeconds = challengeInputWindowSeconds;
            ChallengeWindowTightenPerTier = challengeWindowTightenPerTier;
            FinishAoeDamage = finishAoeDamage;
        }

        public static GrowthConfig FromSettings()
        {
            var s = GrowthSettingsAsset.InstanceAsset.Settings;
            return new GrowthConfig(s.XpPerKill, s.ChargePerFinish, s.OverchargePerFinish, s.OverchargeCap,
                s.XpCostBase, s.XpCostExponent, s.LevelUpSlowMoScale,
                s.ChallengeSequenceBase, s.ChallengeSequenceHot,
                s.ChallengeInputWindowSeconds, s.ChallengeWindowTightenPerTier, s.FinishAoeDamage);
        }
    }
}
