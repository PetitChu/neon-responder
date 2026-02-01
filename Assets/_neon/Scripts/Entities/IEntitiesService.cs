using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Represents a tracked entity in the world.
    /// Designed to be DOTS-friendly: uses integer IDs and type categorization.
    /// </summary>
    public struct TrackedEntity
    {
        public int Id;
        public UNITTYPE UnitType;
        public GameObject GameObject;
        public UnitDefinitionAsset Definition;
    }

    /// <summary>
    /// Service that tracks all active entities (players, enemies, NPCs) in the world.
    /// Designed as a DOTS-friendly abstraction that will map to EntityManager/EntityQuery later.
    /// </summary>
    public interface IEntitiesService
    {
        /// <summary>
        /// Registers a new entity and returns its assigned ID.
        /// </summary>
        int Register(GameObject gameObject, UNITTYPE unitType, UnitDefinitionAsset definition = null);

        /// <summary>
        /// Unregisters an entity by its ID.
        /// </summary>
        void Unregister(int entityId);

        /// <summary>
        /// Returns all tracked entities.
        /// </summary>
        IReadOnlyList<TrackedEntity> GetAll();

        /// <summary>
        /// Returns all tracked entities of a specific type.
        /// </summary>
        IReadOnlyList<TrackedEntity> GetByType(UNITTYPE unitType);

        /// <summary>
        /// Returns the count of tracked entities of a specific type.
        /// </summary>
        int GetCount(UNITTYPE unitType);

        /// <summary>
        /// Returns the first entity of a given type, or default if none found.
        /// </summary>
        TrackedEntity GetFirstByType(UNITTYPE unitType);

        /// <summary>
        /// Tries to find a tracked entity by its GameObject.
        /// </summary>
        bool TryGetByGameObject(GameObject gameObject, out TrackedEntity entity);

        /// <summary>
        /// Fired when a new entity is registered.
        /// </summary>
        event Action<TrackedEntity> OnEntityRegistered;

        /// <summary>
        /// Fired when an entity is unregistered.
        /// </summary>
        event Action<TrackedEntity> OnEntityUnregistered;
    }
}
