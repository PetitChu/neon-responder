using System.Collections.Generic;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Configuration asset for a level. Defines player spawn setup,
    /// enemy wave definitions, and level completion behavior.
    /// Referenced by the Level MonoBehaviour in each level scene.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelConfiguration", menuName = "Neon/Level/Level Configuration")]
    public class LevelConfigurationAsset : ScriptableObject
    {
        [Header("Player")]
        [Tooltip("The player unit definition to spawn.")]
        public UnitDefinitionAsset DefaultPlayerDefinition;

        [Tooltip("Where the player spawns as a progression % of the level (0 = start, 1 = end).")]
        [Range(0f, 1f)]
        public float PlayerSpawnProgression = 0.02f;

        [Tooltip("The direction the player faces when spawning.")]
        public DIRECTION PlayerSpawnDirection = DIRECTION.RIGHT;

        [Header("Waves")]
        [Tooltip("Enemy waves for this level, processed in order.")]
        public List<EnemyWaveDefinition> Waves = new();

        [Header("Completion")]
        [Tooltip("Automatically end the level when all waves are completed.")]
        public bool EndLevelWhenAllWavesCompleted = true;

        [Tooltip("Trigger slow motion effect when the last enemy is killed.")]
        public bool SlowMotionOnLastKill = true;

        [Header("UI Menus")]
        [Tooltip("Menu to open when the level is completed.")]
        public string LevelCompletedMenu = "LevelCompleted";

        [Tooltip("Menu to open when all levels are completed.")]
        public string AllLevelsCompletedMenu = "AllLevelsCompleted";

        [Tooltip("Menu to open when the player dies.")]
        public string GameOverMenu = "GameOver";
    }
}
