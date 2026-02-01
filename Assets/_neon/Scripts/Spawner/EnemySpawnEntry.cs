using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Defines a single enemy type and count within a wave.
    /// </summary>
    [System.Serializable]
    public class EnemySpawnEntry
    {
        [Tooltip("The unit definition for the enemy to spawn.")]
        public UnitDefinitionAsset UnitDefinition;

        [Tooltip("How many of this enemy type to spawn in this wave.")]
        public int Count = 1;

        [Tooltip("Delay in seconds between spawning individual enemies of this entry.")]
        public float SpawnInterval = 0.5f;
    }
}
