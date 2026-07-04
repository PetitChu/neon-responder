using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>Per-level swarm parameters, built by Level from its LevelConfigurationAsset + geometry.</summary>
    public readonly struct SwarmConfig
    {
        public readonly bool Enabled;
        public readonly int ChaffCap;
        public readonly int AmbientCap;
        public readonly float SpawnRatePerSecond;
        public readonly int ChaffMaxHealth;
        public readonly float ChaffMoveSpeed;
        public readonly Vector2 BeltMin;
        public readonly Vector2 BeltMax;
        public readonly float FinishReadyThreshold;

        public SwarmConfig(bool enabled, int chaffCap, int ambientCap, float spawnRatePerSecond,
            int chaffMaxHealth, float chaffMoveSpeed, Vector2 beltMin, Vector2 beltMax, float finishReadyThreshold)
        {
            Enabled = enabled;
            ChaffCap = chaffCap;
            AmbientCap = ambientCap;
            SpawnRatePerSecond = spawnRatePerSecond;
            ChaffMaxHealth = chaffMaxHealth;
            ChaffMoveSpeed = chaffMoveSpeed;
            BeltMin = beltMin;
            BeltMax = beltMax;
            FinishReadyThreshold = finishReadyThreshold;
        }

        public static SwarmConfig From(LevelConfigurationAsset config, Level level)
        {
            var block = config.Swarm;
            var settings = EngagementSettingsAsset.InstanceAsset.Settings;
            return new SwarmConfig(
                block.EnableSwarm,
                block.ChaffCap,
                block.AmbientCap,
                block.ChaffSpawnRatePerSecond,
                settings.ChaffMaxHealth,
                block.ChaffMoveSpeed,
                new Vector2(level.LevelStartX, block.BeltYMin),
                new Vector2(level.LevelStartX + level.LevelLength, block.BeltYMax),
                settings.FinishReadyHealthThreshold);
        }
    }
}
