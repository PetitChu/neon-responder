using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Core level MonoBehaviour that lives in each level scene.
    /// Orchestrates spawning of players, NPCs, and enemy waves via SpawnerService.
    /// Replaces the legacy WaveManager.
    /// </summary>
    public class Level : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("The configuration asset that defines this level's spawn rules.")]
        [SerializeField]
        private LevelConfigurationAsset _configuration;

        [Header("Debug")]
        [ReadOnlyProperty, SerializeField]
        private int _currentWave;

        [ReadOnlyProperty, SerializeField]
        private int _totalWaves;

        [ReadOnlyProperty, SerializeField]
        private bool _allWavesCompleted;

        private SpawnerService _spawnerService;
        private bool _slowMotionInProgress;

        /// <summary>
        /// The level configuration asset.
        /// </summary>
        public LevelConfigurationAsset Configuration => _configuration;

        /// <summary>
        /// The spawner service for this level.
        /// </summary>
        public SpawnerService SpawnerService => _spawnerService;

        void Start()
        {
            if (_configuration == null)
            {
                Debug.LogError("[Level] No LevelConfigurationAsset assigned. Spawning will not work.");
                return;
            }

            // Resolve EntitiesService (registered as singleton in DI)
            var entitiesService = EntitiesService.Instance;
            if (entitiesService == null)
            {
                Debug.LogError("[Level] EntitiesService.Instance is null. Ensure it is registered and resolved before level loads.");
                return;
            }

            // Create level-scoped spawner
            _spawnerService = new SpawnerService(entitiesService, _configuration);
            _spawnerService.OnWaveStarted += OnWaveStarted;
            _spawnerService.OnWaveCompleted += OnWaveCompleted;
            _spawnerService.OnAllWavesCompleted += OnAllWavesCompleted;

            // Find spawnpoints in the scene
            var playerSpawnpoints = FindObjectsByType<PlayerSpawnpoint>(FindObjectsSortMode.None);
            var npcSpawnpoints = FindObjectsByType<NpcSpawnpoint>(FindObjectsSortMode.None);

            // Spawn players and NPCs
            _spawnerService.SpawnPlayers(playerSpawnpoints);
            _spawnerService.SpawnNpcs(npcSpawnpoints);

            // Start enemy waves
            _spawnerService.StartWaves();

            // Update debug fields
            _totalWaves = _spawnerService.TotalWaves;
        }

        void Update()
        {
            _spawnerService?.Tick(Time.deltaTime);

            // Update debug fields
            if (_spawnerService != null)
            {
                _currentWave = _spawnerService.CurrentWaveIndex;
                _allWavesCompleted = _spawnerService.AllWavesCompleted;
            }
        }

        void OnEnable()
        {
            HealthSystem.onUnitDeath += OnUnitDeath;
        }

        void OnDisable()
        {
            HealthSystem.onUnitDeath -= OnUnitDeath;
        }

        void OnDestroy()
        {
            if (_spawnerService != null)
            {
                _spawnerService.OnWaveStarted -= OnWaveStarted;
                _spawnerService.OnWaveCompleted -= OnWaveCompleted;
                _spawnerService.OnAllWavesCompleted -= OnAllWavesCompleted;
                _spawnerService.Dispose();
                _spawnerService = null;
            }
        }

        private void OnWaveStarted(int waveIndex)
        {
            Debug.Log($"[Level] Wave {waveIndex + 1}/{_totalWaves} started.");
        }

        private void OnWaveCompleted(int waveIndex)
        {
            Debug.Log($"[Level] Wave {waveIndex + 1}/{_totalWaves} completed.");
        }

        private void OnAllWavesCompleted()
        {
            if (!_configuration.EndLevelWhenAllWavesCompleted) return;

            SaveLevelProgress();

            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager == null)
            {
                Debug.LogWarning("[Level] No UIManager found. Cannot show level completion screen.");
                return;
            }

            if (LevelProgress.isLastLevel)
            {
                uiManager.ShowMenu(_configuration.AllLevelsCompletedMenu);
            }
            else
            {
                uiManager.ShowMenu(_configuration.LevelCompletedMenu);
            }
        }

        private void OnUnitDeath(GameObject unit)
        {
            var unitSettings = unit.GetComponent<UnitSettings>();
            if (unitSettings == null) return;

            if (unitSettings.unitType == UNITTYPE.PLAYER)
            {
                OnPlayerDeath();
                return;
            }

            if (unitSettings.unitType == UNITTYPE.ENEMY)
            {
                // Unregister the dead enemy from EntitiesService
                if (EntitiesService.Instance != null &&
                    EntitiesService.Instance.TryGetByGameObject(unit, out TrackedEntity entity))
                {
                    EntitiesService.Instance.Unregister(entity.Id);
                }

                // Slow motion on last kill
                if (_configuration.SlowMotionOnLastKill && _spawnerService.AllWavesCompleted)
                {
                    int remaining = EntitiesService.Instance?.GetCount(UNITTYPE.ENEMY) ?? 0;
                    if (remaining == 0)
                    {
                        StartCoroutine(SlowMotionRoutine());
                    }
                }
            }
        }

        private void OnPlayerDeath()
        {
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowMenu(_configuration.GameOverMenu);
            }
        }

        private void SaveLevelProgress()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            if (!LevelProgress.levelsCompleted.Contains(currentScene))
            {
                LevelProgress.levelsCompleted.Add(currentScene);
            }
        }

        private IEnumerator SlowMotionRoutine()
        {
            if (_slowMotionInProgress) yield break;
            _slowMotionInProgress = true;
            Time.timeScale = 0.5f;
            yield return new WaitForSecondsRealtime(1.5f);
            while (Time.timeScale < 1f)
            {
                Time.timeScale += Time.unscaledDeltaTime;
            }
            Time.timeScale = 1f;
            _slowMotionInProgress = false;
        }
    }
}
