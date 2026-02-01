using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Core level MonoBehaviour that lives in each level scene.
    /// Extends LifetimeScope to create a scene-scoped DI container that inherits
    /// all application services. Injects scene MonoBehaviours and spawned units.
    /// Orchestrates spawning of players and enemy waves via SpawnerService.
    /// </summary>
    public class Level : LifetimeScope
    {
        [Header("Configuration")]
        [Tooltip("The configuration asset that defines this level's spawn rules.")]
        [SerializeField]
        private LevelConfigurationAsset _configuration;

        [Header("Level Geometry")]
        [Tooltip("The world X position where the level starts.")]
        [SerializeField]
        private float _levelStartX;

        [Tooltip("The total length of the level in world units. Used for progression-based calculations.")]
        [SerializeField]
        private float _levelLength = 50f;

        [Header("Debug")]
        [ReadOnlyProperty, SerializeField]
        private int _currentWave;

        [ReadOnlyProperty, SerializeField]
        private int _totalWaves;

        [ReadOnlyProperty, SerializeField]
        private bool _allWavesCompleted;

        private SpawnerService _spawnerService;
        private bool _slowMotionInProgress;
        private LevelBound _dynamicCameraBound;
        private IEntitiesService _entitiesService;

        /// <summary>
        /// The level configuration asset.
        /// </summary>
        public LevelConfigurationAsset Configuration => _configuration;

        /// <summary>
        /// The spawner service for this level.
        /// </summary>
        public SpawnerService SpawnerService => _spawnerService;

        /// <summary>
        /// The world X position where the level starts.
        /// </summary>
        public float LevelStartX => _levelStartX;

        /// <summary>
        /// The total length of the level in world units.
        /// </summary>
        public float LevelLength => _levelLength;

        /// <summary>
        /// Converts a level progression value (0-1) to a world X position.
        /// </summary>
        public float ProgressionToWorldX(float progression)
        {
            return _levelStartX + (_levelLength * Mathf.Clamp01(progression));
        }

        /// <summary>
        /// Sets the camera bound to a position calculated from level progression.
        /// Creates a dynamic LevelBound if one doesn't exist.
        /// </summary>
        public void SetCameraBoundFromProgression(float progression)
        {
            float worldX = ProgressionToWorldX(progression);

            if (_dynamicCameraBound == null)
            {
                var go = new GameObject("DynamicLevelBound");
                go.transform.SetParent(transform);
                _dynamicCameraBound = go.AddComponent<LevelBound>();
                _dynamicCameraBound.showDebugLine = true;
                _dynamicCameraBound.lineColor = Color.red;
            }

            _dynamicCameraBound.transform.position = new Vector3(worldX, transform.position.y, 0f);

            var camFollow = Camera.main?.GetComponent<CameraFollow>();
            if (camFollow != null)
            {
                camFollow.levelBound = _dynamicCameraBound;
            }
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterBuildCallback(container =>
            {
                // Inject all scene MonoBehaviours with [Inject] attributes
                foreach (var root in gameObject.scene.GetRootGameObjects())
                {
                    container.InjectGameObject(root);
                }
            });
        }

        void Start()
        {
            if (_configuration == null)
            {
                Debug.LogError("[Level] No LevelConfigurationAsset assigned. Spawning will not work.");
                return;
            }

            _entitiesService = Container.Resolve<IEntitiesService>();
            if (_entitiesService == null)
            {
                Debug.LogError("[Level] Could not resolve IEntitiesService from container.");
                return;
            }

            // Create level-scoped spawner with container access for injection
            _spawnerService = new SpawnerService(_entitiesService, _configuration, this, Container);
            _spawnerService.OnWaveStarted += OnWaveStarted;
            _spawnerService.OnWaveCompleted += OnWaveCompleted;
            _spawnerService.OnAllWavesCompleted += OnAllWavesCompleted;

            // Spawn player at configured progression point
            _spawnerService.SpawnPlayers();

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

        protected override void OnDestroy()
        {
            if (_spawnerService != null)
            {
                _spawnerService.OnWaveStarted -= OnWaveStarted;
                _spawnerService.OnWaveCompleted -= OnWaveCompleted;
                _spawnerService.OnAllWavesCompleted -= OnAllWavesCompleted;
                _spawnerService.Dispose();
                _spawnerService = null;
            }

            base.OnDestroy();
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
                if (_entitiesService != null &&
                    _entitiesService.TryGetByGameObject(unit, out TrackedEntity entity))
                {
                    _entitiesService.Unregister(entity.Id);
                }

                // Slow motion on last kill
                if (_configuration.SlowMotionOnLastKill && _spawnerService.AllWavesCompleted)
                {
                    int remaining = _entitiesService?.GetCount(UNITTYPE.ENEMY) ?? 0;
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

        private void OnDrawGizmos()
        {
            if (_levelLength <= 0) return;

            // Draw level bounds as vertical lines
            Gizmos.color = Color.yellow;
            float startX = _levelStartX;
            float endX = _levelStartX + _levelLength;
            float y = transform.position.y;
            Gizmos.DrawLine(new Vector3(startX, y - 5f, 0), new Vector3(startX, y + 5f, 0));
            Gizmos.DrawLine(new Vector3(endX, y - 5f, 0), new Vector3(endX, y + 5f, 0));

            // Draw progression markers at 25% intervals
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            for (float p = 0.25f; p < 1f; p += 0.25f)
            {
                float px = ProgressionToWorldX(p);
                Gizmos.DrawLine(new Vector3(px, y - 3f, 0), new Vector3(px, y + 3f, 0));
            }

            // Draw player spawn position (green)
            if (_configuration != null)
            {
                Gizmos.color = Color.green;
                float playerX = ProgressionToWorldX(_configuration.PlayerSpawnProgression);
                Gizmos.DrawLine(new Vector3(playerX, y - 4f, 0), new Vector3(playerX, y + 4f, 0));
                Gizmos.DrawWireSphere(new Vector3(playerX, y, 0), 0.5f);
            }
        }
    }
}
