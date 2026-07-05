using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrainlessLabs.Neon.Tests
{
    internal sealed class FakeEntitiesService : IEntitiesService
    {
        private readonly List<TrackedEntity> _entities = new();
        private int _nextId = 1;

        public event Action<TrackedEntity> OnEntityRegistered;
        public event Action<TrackedEntity> OnEntityUnregistered;

        public int Register(GameObject gameObject, UNITTYPE unitType, UnitDefinitionAsset definition = null)
        {
            var entity = new TrackedEntity { Id = _nextId++, UnitType = unitType, GameObject = gameObject, Definition = definition };
            _entities.Add(entity);
            OnEntityRegistered?.Invoke(entity);
            return entity.Id;
        }

        public void Unregister(int entityId)
        {
            int index = _entities.FindIndex(e => e.Id == entityId);
            if (index < 0) return;
            var entity = _entities[index];
            _entities.RemoveAt(index);
            OnEntityUnregistered?.Invoke(entity);
        }

        public IReadOnlyList<TrackedEntity> GetAll() => _entities;
        public IReadOnlyList<TrackedEntity> GetByType(UNITTYPE unitType) => _entities.FindAll(e => e.UnitType == unitType);
        public int GetCount(UNITTYPE unitType) => GetByType(unitType).Count;

        public TrackedEntity GetFirstByType(UNITTYPE unitType)
        {
            foreach (var entity in _entities)
            {
                if (entity.UnitType == unitType) return entity;
            }
            return default;
        }

        public bool TryGetByGameObject(GameObject gameObject, out TrackedEntity entity)
        {
            foreach (var candidate in _entities)
            {
                if (candidate.GameObject == gameObject)
                {
                    entity = candidate;
                    return true;
                }
            }
            entity = default;
            return false;
        }
    }

    internal sealed class FakeSwarmBridge : ISwarmBridge
    {
        public TargetRef? NearestHot;
        public TargetRef? NearestFinishReady;
        public int HotCount;
        public int FinishReadyCount;
        public readonly List<(TargetRef Target, int Damage)> ChipCalls = new();

        public bool TryGetNearestHot(Vector2 origin, float facingSign, float arcDegrees, float range, out TargetRef target)
        {
            if (NearestHot.HasValue)
            {
                target = NearestHot.Value;
                return true;
            }
            target = default;
            return false;
        }

        public bool TryGetNearestFinishReady(Vector2 origin, out TargetRef target)
        {
            if (NearestFinishReady.HasValue)
            {
                target = NearestFinishReady.Value;
                return true;
            }
            target = default;
            return false;
        }

        public int CountHot() => HotCount;
        public int CountFinishReady() => FinishReadyCount;

        public void ApplyChip(in TargetRef target, int damage) => ChipCalls.Add((target, damage));

        public bool ApplyVerbHit(Bounds hitBounds, AttackData attackData) => false;

        public readonly List<(Vector2 Center, float Radius, int Damage)> AreaDamageCalls = new();

        public void ApplyAreaDamage(Vector2 center, float radius, int damage) => AreaDamageCalls.Add((center, radius, damage));
    }
}
