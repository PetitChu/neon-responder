using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BrainlessLabs.Neon.SwarmRenderSpike
{
    /// <summary>
    /// Spawns the spike population into the default ECS world.
    /// Throwaway spike driver — intentionally uses no DI services.
    /// </summary>
    public class SpikeBootstrap : MonoBehaviour
    {
        [SerializeField] private int _hotCount = 150;
        [SerializeField] private int _ambientCount = 100;
        [SerializeField] private Vector2 _boundsMin = new(-16f, -4.5f);
        [SerializeField] private Vector2 _boundsMax = new(16f, 4.5f);
        [SerializeField] private float _minSpeed = 0.5f;
        [SerializeField] private float _maxSpeed = 3f;

        private void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                Debug.LogError("[Spike] No default ECS world — automatic Entities bootstrap is disabled?");
                return;
            }

            var entityManager = world.EntityManager;

            var boundsEntity = entityManager.CreateEntity(typeof(SpikeBounds));
            entityManager.SetComponentData(boundsEntity, new SpikeBounds
            {
                Min = new float2(_boundsMin.x, _boundsMin.y),
                Max = new float2(_boundsMax.x, _boundsMax.y)
            });

            var random = new Unity.Mathematics.Random(1234);
            Spawn<HotAgentTag>(entityManager, ref random, _hotCount);
            Spawn<AmbientAgentTag>(entityManager, ref random, _ambientCount);

            Debug.Log($"[Spike] Spawned {_hotCount} hot + {_ambientCount} ambient agents.");
        }

        private void OnDestroy()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                return;
            }

            var entityManager = world.EntityManager;
            using var agents = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpikePosition>());
            entityManager.DestroyEntity(agents);
            using var bounds = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpikeBounds>());
            entityManager.DestroyEntity(bounds);
        }

        private void Spawn<TTag>(EntityManager entityManager, ref Unity.Mathematics.Random random, int count)
            where TTag : struct, IComponentData
        {
            var archetype = entityManager.CreateArchetype(
                typeof(SpikePosition), typeof(SpikeVelocity), typeof(TTag));
            using var entities = entityManager.CreateEntity(archetype, count, Allocator.Temp);

            var min = new float2(_boundsMin.x, _boundsMin.y);
            var max = new float2(_boundsMax.x, _boundsMax.y);

            foreach (var entity in entities)
            {
                entityManager.SetComponentData(entity, new SpikePosition
                {
                    Value = random.NextFloat2(min, max)
                });
                entityManager.SetComponentData(entity, new SpikeVelocity
                {
                    Value = random.NextFloat2Direction() * random.NextFloat(_minSpeed, _maxSpeed)
                });
            }
        }
    }
}
