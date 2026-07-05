namespace BrainlessLabs.Neon
{
    /// <summary>Asset-free snapshot of the actives tuning (EditMode-testable systems).</summary>
    public readonly struct SpecialConfig
    {
        public readonly float SirenCooldownSeconds;
        public readonly int SirenChargeCost;
        public readonly float SirenRadius;
        public readonly float FinisherRadius;
        public readonly float FinisherFreezeSeconds;

        public SpecialConfig(float sirenCooldownSeconds, int sirenChargeCost, float sirenRadius,
            float finisherRadius, float finisherFreezeSeconds)
        {
            SirenCooldownSeconds = sirenCooldownSeconds;
            SirenChargeCost = sirenChargeCost;
            SirenRadius = sirenRadius;
            FinisherRadius = finisherRadius;
            FinisherFreezeSeconds = finisherFreezeSeconds;
        }

        public static SpecialConfig FromSettings()
        {
            var s = FeelSettingsAsset.InstanceAsset.Settings;
            return new SpecialConfig(s.SirenCooldownSeconds, s.SirenChargeCost, s.SirenRadius,
                s.FinisherRadius, s.FinisherFreezeSeconds);
        }
    }
}
