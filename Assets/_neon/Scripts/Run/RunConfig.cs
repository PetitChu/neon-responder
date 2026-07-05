using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>Asset-free run snapshot: per-level encounter geometry + global tuning.</summary>
    public readonly struct RunConfig
    {
        public readonly bool Enabled;
        public readonly Vector2[] NodePositions;
        public readonly float NodeRadius;
        public readonly float RebootDurationSeconds;
        public readonly float EncounterIntroSeconds;
        public readonly float DawnValue;
        public readonly int ShopHealCost;
        public readonly int ShopHealAmount;
        public readonly float ShopPauseScale;

        public int EncounterCount => NodePositions?.Length ?? 0;
        public float SignalPerObjective => EncounterCount > 0 ? DawnValue / EncounterCount : DawnValue;

        public RunConfig(bool enabled, Vector2[] nodePositions, float nodeRadius,
            float rebootDurationSeconds, float encounterIntroSeconds, float dawnValue,
            int shopHealCost, int shopHealAmount, float shopPauseScale)
        {
            Enabled = enabled;
            NodePositions = nodePositions;
            NodeRadius = nodeRadius;
            RebootDurationSeconds = rebootDurationSeconds;
            EncounterIntroSeconds = encounterIntroSeconds;
            DawnValue = dawnValue;
            ShopHealCost = shopHealCost;
            ShopHealAmount = shopHealAmount;
            ShopPauseScale = shopPauseScale;
        }

        public static RunConfig From(LevelConfigurationAsset config, RunSettings settings)
        {
            var block = config.Run;
            return new RunConfig(
                block.EnableRun,
                block.EncounterNodePositions,
                block.NodeRadius,
                settings.RebootDurationSeconds,
                settings.EncounterIntroSeconds,
                settings.DawnValue,
                settings.ShopHealCost,
                settings.ShopHealAmount,
                settings.ShopPauseScale);
        }
    }
}
