using System.Collections.Generic;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// How a wave is triggered to start.
    /// </summary>
    public enum WaveTriggerType
    {
        /// <summary>Starts automatically when the previous wave is completed.</summary>
        PreviousWaveCompleted,

        /// <summary>Starts when the player reaches a certain progression % of the level.</summary>
        ProgressionPercent,

        /// <summary>Starts when the player reaches a minimum distance from the level start.</summary>
        DistanceFromStart,

        /// <summary>Triggered programmatically (e.g. by a trigger zone or event).</summary>
        Manual
    }

    /// <summary>
    /// How enemies are positioned when spawned.
    /// </summary>
    public enum SpawnPositionMode
    {
        /// <summary>Spawn at a distance from the player (offscreen).</summary>
        RelativeToPlayer,

        /// <summary>Spawn at a specific level progression percentage.</summary>
        AtProgression
    }

    /// <summary>
    /// Defines a wave of enemies: what to spawn, when, where, and constraints.
    /// </summary>
    [System.Serializable]
    public class EnemyWaveDefinition
    {
        [Header("Identity")]
        [Tooltip("Name of this wave (for debugging and UI).")]
        public string WaveName = "Wave";

        [Header("Trigger")]
        [Tooltip("How this wave is triggered to start.")]
        public WaveTriggerType TriggerType = WaveTriggerType.PreviousWaveCompleted;

        [ShowIf("TriggerType", WaveTriggerType.ProgressionPercent)]
        [Tooltip("Progression % to trigger this wave (0 = level start, 1 = level end).")]
        [Range(0f, 1f)]
        public float TriggerProgressionPercent;

        [ShowIf("TriggerType", WaveTriggerType.DistanceFromStart)]
        [Tooltip("Distance from level start to trigger this wave.")]
        public float TriggerDistance;

        [Header("Enemies")]
        [Tooltip("The enemies to spawn in this wave.")]
        public List<EnemySpawnEntry> Entries = new();

        [Header("Spawn Constraints")]
        [Tooltip("Maximum number of enemies from this wave that can be alive at the same time.")]
        public int MaxActiveEnemies = 5;

        [Tooltip("Cooldown in seconds between spawning enemies.")]
        public float CooldownBetweenSpawns = 1f;

        [Header("Positioning")]
        [Tooltip("How enemies are positioned when spawned.")]
        public SpawnPositionMode SpawnPositionMode = SpawnPositionMode.RelativeToPlayer;

        [ShowIf("SpawnPositionMode", SpawnPositionMode.RelativeToPlayer)]
        [Tooltip("Distance from the player at which enemies spawn (used with RelativeToPlayer mode).")]
        public float SpawnDistanceFromPlayer = 8f;

        [ShowIf("SpawnPositionMode", SpawnPositionMode.AtProgression)]
        [Tooltip("Where enemies spawn as a progression % of the level (0 = start, 1 = end).")]
        [Range(0f, 1f)]
        public float SpawnProgression = 0.5f;

        [Tooltip("Vertical range for random Y offset when spawning.")]
        public Vector2 SpawnYRange = new(-1f, 1f);

        [Header("Camera")]
        [Tooltip("Whether this wave changes the camera bound.")]
        public bool HasCameraBound;

        [ShowIf("HasCameraBound")]
        [Tooltip("Where the camera bound is placed as a progression % of the level (0 = start, 1 = end).")]
        [Range(0f, 1f)]
        public float CameraBoundProgression;
    }
}
