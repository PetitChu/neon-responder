using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Tracks all active entities (players, enemies, NPCs) in the world.
    /// Replaces the static EnemyManager pattern with a service-based approach.
    /// Designed to be DOTS-friendly: uses integer IDs and type-based queries.
    /// </summary>
    public class EntitiesService : IEntitiesService, IDisposable
    {
        private int _nextEntityId = 1;
        private readonly Dictionary<int, TrackedEntity> _entitiesById = new();
        private readonly Dictionary<UNITTYPE, List<TrackedEntity>> _entitiesByType = new();
        private readonly Dictionary<GameObject, int> _gameObjectToId = new();

        public event Action<TrackedEntity> OnEntityRegistered;
        public event Action<TrackedEntity> OnEntityUnregistered;

        public EntitiesService()
        {
            // Pre-populate type lists for the known unit types
            _entitiesByType[UNITTYPE.PLAYER] = new List<TrackedEntity>();
            _entitiesByType[UNITTYPE.ENEMY] = new List<TrackedEntity>();
            _entitiesByType[UNITTYPE.NPC] = new List<TrackedEntity>();
        }

        public int Register(GameObject gameObject, UNITTYPE unitType, UnitDefinitionAsset definition = null)
        {
            if (gameObject == null)
            {
                Debug.LogWarning("[EntitiesService] Cannot register null GameObject.");
                return -1;
            }

            // If already registered, return existing ID
            if (_gameObjectToId.TryGetValue(gameObject, out int existingId))
            {
                return existingId;
            }

            int entityId = _nextEntityId++;
            var entity = new TrackedEntity
            {
                Id = entityId,
                UnitType = unitType,
                GameObject = gameObject,
                Definition = definition
            };

            _entitiesById[entityId] = entity;
            _gameObjectToId[gameObject] = entityId;

            if (!_entitiesByType.ContainsKey(unitType))
            {
                _entitiesByType[unitType] = new List<TrackedEntity>();
            }

            _entitiesByType[unitType].Add(entity);

            OnEntityRegistered?.Invoke(entity);
            return entityId;
        }

        public void Unregister(int entityId)
        {
            if (!_entitiesById.TryGetValue(entityId, out TrackedEntity entity))
            {
                return;
            }

            _entitiesById.Remove(entityId);

            if (entity.GameObject != null)
            {
                _gameObjectToId.Remove(entity.GameObject);
            }

            if (_entitiesByType.TryGetValue(entity.UnitType, out var list))
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i].Id == entityId)
                    {
                        list.RemoveAt(i);
                        break;
                    }
                }
            }

            OnEntityUnregistered?.Invoke(entity);
        }

        public IReadOnlyList<TrackedEntity> GetAll()
        {
            var all = new List<TrackedEntity>(_entitiesById.Count);
            foreach (var kvp in _entitiesById)
            {
                all.Add(kvp.Value);
            }

            return all;
        }

        public IReadOnlyList<TrackedEntity> GetByType(UNITTYPE unitType)
        {
            if (_entitiesByType.TryGetValue(unitType, out var list))
            {
                return list;
            }

            return Array.Empty<TrackedEntity>();
        }

        public int GetCount(UNITTYPE unitType)
        {
            if (_entitiesByType.TryGetValue(unitType, out var list))
            {
                return list.Count;
            }

            return 0;
        }

        public TrackedEntity GetFirstByType(UNITTYPE unitType)
        {
            if (_entitiesByType.TryGetValue(unitType, out var list) && list.Count > 0)
            {
                return list[0];
            }

            return default;
        }

        public bool TryGetByGameObject(GameObject gameObject, out TrackedEntity entity)
        {
            entity = default;

            if (gameObject == null || !_gameObjectToId.TryGetValue(gameObject, out int entityId))
            {
                return false;
            }

            if (_entitiesById.TryGetValue(entityId, out entity))
            {
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            _entitiesById.Clear();
            _entitiesByType.Clear();
            _gameObjectToId.Clear();
            OnEntityRegistered = null;
            OnEntityUnregistered = null;
        }
    }
}
