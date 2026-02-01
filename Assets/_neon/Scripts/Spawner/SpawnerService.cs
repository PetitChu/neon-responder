using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Level-scoped service that handles all unit spawning: players, NPCs, and enemy waves.
    /// Created and owned by the Level MonoBehaviour for each level scene.
    /// </summary>
    public class SpawnerService : IDisposable
    {
        private readonly IEntitiesService _entitiesService;
        private readonly LevelConfigurationAsset _config;

        // Wave state
        private int _currentWaveIndex = -1;
        private bool _wavesStarted;
        private bool _allWavesCompleted;

        // Per-wave tracking
        private int _enemiesSpawnedInWave;
        private int _totalEnemiesToSpawnInWave;
        private int _enemiesAliveInWave;
        private float _lastSpawnTime;
        private int _currentEntryIndex;
        private int _currentEntrySpawned;

        // Spawned entity tracking (for wave management)
        private readonly HashSet<int> _waveEntityIds = new();

        // Player tracking
        private readonly List<int> _playerEntityIds = new();

        public event Action<int> OnWaveStarted;
        public event Action<int> OnWaveCompleted;
        public event Action OnAllWavesCompleted;
        public event Action<GameObject> OnPlayerSpawned;
        public event Action<GameObject> OnEnemySpawned;
        public event Action<GameObject> OnNpcSpawned;

        public int CurrentWaveIndex => _currentWaveIndex;
        public int TotalWaves => _config?.Waves?.Count ?? 0;
        public bool AllWavesCompleted => _allWavesCompleted;
        public bool WavesStarted => _wavesStarted;

        public SpawnerService(IEntitiesService entitiesService, LevelConfigurationAsset config)
        {
            _entitiesService = entitiesService;
            _config = config;

            // Listen for entity removals to track wave enemy deaths
            _entitiesService.OnEntityUnregistered += OnEntityUnregistered;
        }

        /// <summary>
        /// Spawns players at the given spawnpoints.
        /// Uses the spawnpoint's override definition, or falls back to the level's default.
        /// </summary>
        public void SpawnPlayers(PlayerSpawnpoint[] spawnpoints)
        {
            if (spawnpoints == null || spawnpoints.Length == 0)
            {
                Debug.LogWarning("[SpawnerService] No player spawnpoints found in the level.");
                return;
            }

            foreach (var spawnpoint in spawnpoints)
            {
                var definition = spawnpoint.PlayerDefinitionOverride != null
                    ? spawnpoint.PlayerDefinitionOverride
                    : _config.DefaultPlayerDefinition;

                if (definition == null || definition.Prefab == null)
                {
                    Debug.LogWarning($"[SpawnerService] No valid player definition for spawnpoint (Player {spawnpoint.PlayerIndex + 1}).");
                    continue;
                }

                var player = SpawnUnit(definition, spawnpoint.transform.position, spawnpoint.SpawnDirection);
                if (player == null) continue;

                // Set player ID on UnitSettings
                var unitSettings = player.GetComponent<UnitSettings>();
                if (unitSettings != null)
                {
                    unitSettings.playerId = spawnpoint.PlayerIndex + 1;
                }

                int entityId = _entitiesService.Register(player, UNITTYPE.PLAYER, definition);
                _playerEntityIds.Add(entityId);

                OnPlayerSpawned?.Invoke(player);
            }
        }

        /// <summary>
        /// Spawns NPCs at the given spawnpoints that are marked for level load spawning.
        /// </summary>
        public void SpawnNpcs(NpcSpawnpoint[] spawnpoints)
        {
            if (spawnpoints == null || spawnpoints.Length == 0) return;

            foreach (var spawnpoint in spawnpoints)
            {
                if (!spawnpoint.SpawnOnLevelLoad) continue;
                SpawnNpcAtPoint(spawnpoint);
            }
        }

        /// <summary>
        /// Spawns a single NPC at the specified spawnpoint.
        /// Can be called manually for NPC spawnpoints that don't spawn on level load.
        /// </summary>
        public void SpawnNpcAtPoint(NpcSpawnpoint spawnpoint)
        {
            if (spawnpoint.NpcDefinition == null || spawnpoint.NpcDefinition.Prefab == null)
            {
                Debug.LogWarning($"[SpawnerService] NpcSpawnpoint has no valid NPC definition assigned.");
                return;
            }

            var npc = SpawnUnit(spawnpoint.NpcDefinition, spawnpoint.transform.position, spawnpoint.SpawnDirection);
            if (npc == null) return;

            _entitiesService.Register(npc, UNITTYPE.NPC, spawnpoint.NpcDefinition);

            OnNpcSpawned?.Invoke(npc);
        }

        /// <summary>
        /// Begins wave processing. Call after players and NPCs are spawned.
        /// </summary>
        public void StartWaves()
        {
            if (_config.Waves == null || _config.Waves.Count == 0)
            {
                Debug.Log("[SpawnerService] No waves configured for this level.");
                _allWavesCompleted = true;
                OnAllWavesCompleted?.Invoke();
                return;
            }

            _wavesStarted = true;
            AdvanceToNextWave();
        }

        /// <summary>
        /// Must be called every frame by the Level MonoBehaviour to drive wave spawning.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!_wavesStarted || _allWavesCompleted) return;
            if (_currentWaveIndex < 0 || _currentWaveIndex >= _config.Waves.Count) return;

            var wave = _config.Waves[_currentWaveIndex];
            TrySpawnNextEnemy(wave);
        }

        /// <summary>
        /// Manually triggers a wave by index. Used for Manual trigger type waves.
        /// </summary>
        public void TriggerWave(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= _config.Waves.Count)
            {
                Debug.LogWarning($"[SpawnerService] Invalid wave index: {waveIndex}");
                return;
            }

            StartWave(waveIndex);
        }

        public void Dispose()
        {
            if (_entitiesService != null)
            {
                _entitiesService.OnEntityUnregistered -= OnEntityUnregistered;
            }

            _waveEntityIds.Clear();
            _playerEntityIds.Clear();
            OnWaveStarted = null;
            OnWaveCompleted = null;
            OnAllWavesCompleted = null;
            OnPlayerSpawned = null;
            OnEnemySpawned = null;
            OnNpcSpawned = null;
        }

        private void AdvanceToNextWave()
        {
            int nextIndex = _currentWaveIndex + 1;

            if (nextIndex >= _config.Waves.Count)
            {
                _allWavesCompleted = true;
                OnAllWavesCompleted?.Invoke();
                return;
            }

            var wave = _config.Waves[nextIndex];

            // Check if the wave needs a specific trigger
            if (wave.TriggerType == WaveTriggerType.Manual)
            {
                // Manual waves wait for TriggerWave() call
                _currentWaveIndex = nextIndex;
                return;
            }

            // For PreviousWaveCompleted (and first wave), start immediately
            if (wave.TriggerType == WaveTriggerType.PreviousWaveCompleted)
            {
                StartWave(nextIndex);
                return;
            }

            // For other trigger types, just advance the index and let Tick check conditions
            _currentWaveIndex = nextIndex;
        }

        private void StartWave(int waveIndex)
        {
            _currentWaveIndex = waveIndex;
            var wave = _config.Waves[waveIndex];

            _enemiesSpawnedInWave = 0;
            _enemiesAliveInWave = 0;
            _currentEntryIndex = 0;
            _currentEntrySpawned = 0;
            _lastSpawnTime = -wave.CooldownBetweenSpawns; // Allow immediate first spawn
            _waveEntityIds.Clear();

            _totalEnemiesToSpawnInWave = 0;
            foreach (var entry in wave.Entries)
            {
                _totalEnemiesToSpawnInWave += entry.Count;
            }

            Debug.Log($"[SpawnerService] Starting wave {waveIndex}: '{wave.WaveName}' ({_totalEnemiesToSpawnInWave} enemies)");

            // Apply camera level bound if configured
            if (wave.WaveLevelBound != null)
            {
                var camFollow = Camera.main?.GetComponent<CameraFollow>();
                if (camFollow != null)
                {
                    camFollow.levelBound = wave.WaveLevelBound;
                }
            }

            OnWaveStarted?.Invoke(waveIndex);
        }

        private void TrySpawnNextEnemy(EnemyWaveDefinition wave)
        {
            // All enemies spawned for this wave, waiting for them to die
            if (_enemiesSpawnedInWave >= _totalEnemiesToSpawnInWave) return;

            // Max active enemies reached
            if (_enemiesAliveInWave >= wave.MaxActiveEnemies) return;

            // Cooldown between spawns
            if (Time.time - _lastSpawnTime < wave.CooldownBetweenSpawns) return;

            // Find the current entry to spawn from
            while (_currentEntryIndex < wave.Entries.Count)
            {
                var entry = wave.Entries[_currentEntryIndex];
                if (_currentEntrySpawned < entry.Count)
                {
                    SpawnWaveEnemy(wave, entry);
                    return;
                }

                // Move to next entry
                _currentEntryIndex++;
                _currentEntrySpawned = 0;
            }
        }

        private void SpawnWaveEnemy(EnemyWaveDefinition wave, EnemySpawnEntry entry)
        {
            if (entry.UnitDefinition == null || entry.UnitDefinition.Prefab == null)
            {
                Debug.LogWarning("[SpawnerService] Enemy spawn entry has no valid unit definition.");
                _currentEntrySpawned++;
                _enemiesSpawnedInWave++;
                return;
            }

            Vector2 spawnPos = CalculateEnemySpawnPosition(wave);
            DIRECTION spawnDir = GetSpawnDirectionTowardsPlayer(spawnPos);

            var enemy = SpawnUnit(entry.UnitDefinition, spawnPos, spawnDir);
            if (enemy == null) return;

            int entityId = _entitiesService.Register(enemy, UNITTYPE.ENEMY, entry.UnitDefinition);
            _waveEntityIds.Add(entityId);

            _currentEntrySpawned++;
            _enemiesSpawnedInWave++;
            _enemiesAliveInWave++;
            _lastSpawnTime = Time.time;

            OnEnemySpawned?.Invoke(enemy);
        }

        private Vector2 CalculateEnemySpawnPosition(EnemyWaveDefinition wave)
        {
            // Find the first player as reference
            var playerEntity = _entitiesService.GetFirstByType(UNITTYPE.PLAYER);
            Vector2 playerPos = playerEntity.GameObject != null
                ? (Vector2)playerEntity.GameObject.transform.position
                : Vector2.zero;

            switch (wave.SpawnPositionMode)
            {
                case SpawnPositionMode.RelativeToPlayer:
                    // Alternate left/right of player
                    float side = (_enemiesSpawnedInWave % 2 == 0) ? 1f : -1f;
                    float x = playerPos.x + (wave.SpawnDistanceFromPlayer * side);
                    float y = playerPos.y + UnityEngine.Random.Range(wave.SpawnYRange.x, wave.SpawnYRange.y);
                    return new Vector2(x, y);

                case SpawnPositionMode.FixedPosition:
                    // Use the wave level bound position as reference, with Y offset
                    if (wave.WaveLevelBound != null)
                    {
                        float fy = wave.WaveLevelBound.transform.position.y
                            + UnityEngine.Random.Range(wave.SpawnYRange.x, wave.SpawnYRange.y);
                        return new Vector2(wave.WaveLevelBound.transform.position.x, fy);
                    }
                    return playerPos;

                default:
                    return playerPos + Vector2.right * wave.SpawnDistanceFromPlayer;
            }
        }

        private DIRECTION GetSpawnDirectionTowardsPlayer(Vector2 spawnPos)
        {
            var playerEntity = _entitiesService.GetFirstByType(UNITTYPE.PLAYER);
            if (playerEntity.GameObject == null) return DIRECTION.RIGHT;

            return spawnPos.x > playerEntity.GameObject.transform.position.x
                ? DIRECTION.LEFT
                : DIRECTION.RIGHT;
        }

        private GameObject SpawnUnit(UnitDefinitionAsset definition, Vector2 position, DIRECTION direction)
        {
            if (definition == null || definition.Prefab == null)
            {
                Debug.LogWarning("[SpawnerService] Cannot spawn unit: null definition or prefab.");
                return null;
            }

            var instance = UnityEngine.Object.Instantiate(definition.Prefab, position, Quaternion.identity);

            // Set direction
            if (direction == DIRECTION.LEFT)
            {
                instance.transform.localRotation = Quaternion.Euler(0, 180, 0);
            }

            // Apply definition data to UnitSettings if present
            var unitSettings = instance.GetComponent<UnitSettings>();
            if (unitSettings != null)
            {
                if (!string.IsNullOrEmpty(definition.DisplayName))
                {
                    unitSettings.unitName = definition.DisplayName;
                }
            }

            // Apply health from definition if configured
            var healthSystem = instance.GetComponent<HealthSystem>();
            if (healthSystem != null && definition.MaxHealth > 0)
            {
                healthSystem.maxHp = definition.MaxHealth;
                healthSystem.currentHp = definition.MaxHealth;
            }

            return instance;
        }

        private void OnEntityUnregistered(TrackedEntity entity)
        {
            // Track wave enemy deaths
            if (_waveEntityIds.Remove(entity.Id))
            {
                _enemiesAliveInWave = Mathf.Max(0, _enemiesAliveInWave - 1);

                // Check if wave is complete (all spawned and all dead)
                if (_enemiesSpawnedInWave >= _totalEnemiesToSpawnInWave && _enemiesAliveInWave <= 0)
                {
                    Debug.Log($"[SpawnerService] Wave {_currentWaveIndex} completed.");
                    OnWaveCompleted?.Invoke(_currentWaveIndex);
                    AdvanceToNextWave();
                }
            }
        }
    }
}
