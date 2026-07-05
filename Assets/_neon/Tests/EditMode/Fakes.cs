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

        public readonly List<(Vector2 Center, float Radius)> MassFinishReadyCalls = new();
        public readonly List<(Vector2 Center, float Radius)> FinishAllChaffCalls = new();
        public int MassFinishReadyReturn;
        public int FinishAllChaffReturn;

        public int MassFinishReady(Vector2 center, float radius)
        {
            MassFinishReadyCalls.Add((center, radius));
            return MassFinishReadyReturn;
        }

        public int FinishAllChaff(Vector2 center, float radius)
        {
            FinishAllChaffCalls.Add((center, radius));
            return FinishAllChaffReturn;
        }
    }

    internal sealed class FakeMomentumSystem : IMomentumSystem
    {
        public MomentumTier Tier { get; set; } = MomentumTier.Cool;
    }

    internal sealed class FakeEconomy : IEconomySystem
    {
        public int XpValue;
        public int NeonChargeValue;
        public int OverchargeValue;
        public bool OverchargeFull;

        public int Xp => XpValue;
        public int NeonCharge => NeonChargeValue;
        public int Overcharge => OverchargeValue;
        public bool IsOverchargeFull => OverchargeFull;

        public bool TrySpend(int amount)
        {
            if (amount <= 0) return true;
            if (NeonChargeValue < amount) return false;
            NeonChargeValue -= amount;
            return true;
        }

        public bool TryConsumeOvercharge()
        {
            if (!OverchargeFull) return false;
            OverchargeValue = 0;
            OverchargeFull = false;
            return true;
        }
    }

    internal sealed class FakeInputService : IInputService
    {
        public bool Special;
        public bool Finisher;

        public bool PunchKeyDown(int playerId) => false;
        public bool KickKeyDown(int playerId) => false;
        public bool DefendKeyDown(int playerId) => false;
        public bool GrabKeyDown(int playerId) => false;
        public bool JumpKeyDown(int playerId) => false;
        public UnityEngine.Vector2 GetInputVector(int playerId) => UnityEngine.Vector2.zero;
        public bool JoypadDirInputDetected(int playerId) => false;

        // Consumed-on-read, mirroring WasPressedThisFrame edge semantics.
        public bool SpecialKeyDown(int playerId) { bool v = Special; Special = false; return v; }
        public bool FinisherKeyDown(int playerId) { bool v = Finisher; Finisher = false; return v; }
    }
}
