using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using BrainlessLabs.Neon.Simulation;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Pure render projection of the swarm sim (no DI, no gameplay):
    /// hot chaff → pooled SpriteRenderer proxies with STABLE entity↔proxy mapping
    /// (spike verdict) + Finish-Ready glow tint; ambient → DrawMeshInstanced quads.
    /// Place one in each swarm-enabled level scene.
    /// </summary>
    public class SwarmRenderRig : MonoBehaviour
    {
        [Header("Hot chaff proxies")]
        [SerializeField] private Sprite _chaffSprite;
        [SerializeField] private Color _hotColor = new(1f, 0.35f, 0.65f, 1f);
        [SerializeField] private Color _finishReadyColor = new(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private int _proxyCapacity = 150;
        [SerializeField] private Material _chaffMaterial; // assign Sprite-Lit-Default so 2D lights reach chaff later

        [Header("Ambient instancing (URP: DrawMeshInstanced + Neon/InstancedUnlit)")]
        [SerializeField] private Material _ambientMaterial;
        [SerializeField] private float _ambientSize = 0.8f;

        private Transform[] _proxies;
        private SpriteRenderer[] _proxyRenderers;
        private readonly Dictionary<Entity, int> _entityToProxy = new();
        private readonly Stack<int> _freeProxies = new();
        private readonly HashSet<Entity> _seenThisFrame = new();
        private readonly HashSet<Entity> _readySet = new();
        private readonly List<Entity> _releaseScratch = new();

        private EntityQuery _chaffQuery;
        private EntityQuery _readyQuery;
        private EntityQuery _ambientQuery;
        private Mesh _quad;
        private Matrix4x4[] _ambientMatrices;
        private bool _ready;

        private void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || _chaffSprite == null)
            {
                Debug.LogError("[Swarm] SwarmRenderRig: missing ECS world or chaff sprite.");
                enabled = false;
                return;
            }
            if (_ambientMaterial != null && !_ambientMaterial.enableInstancing)
            {
                Debug.LogError("[Swarm] SwarmRenderRig: ambient material must have GPU instancing enabled.");
            }

            var entityManager = world.EntityManager;
            _chaffQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<BeltPosition>(), ComponentType.ReadOnly<SwarmHealth>());
            _readyQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<BeltPosition>(), ComponentType.ReadOnly<FinishReadyTag>());
            _ambientQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SwarmAgent, BeltPosition>()
                .WithNone<SwarmHealth>()
                .Build(entityManager);

            _proxies = new Transform[_proxyCapacity];
            _proxyRenderers = new SpriteRenderer[_proxyCapacity];
            for (int i = 0; i < _proxyCapacity; i++)
            {
                var proxy = new GameObject($"ChaffProxy_{i}");
                proxy.transform.SetParent(transform, worldPositionStays: false);
                var spriteRenderer = proxy.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = _chaffSprite;
                if (_chaffMaterial != null) spriteRenderer.material = _chaffMaterial;
                spriteRenderer.color = _hotColor;
                proxy.SetActive(false);
                _proxies[i] = proxy.transform;
                _proxyRenderers[i] = spriteRenderer;
                _freeProxies.Push(i);
            }

            _quad = BuildQuad();
            _ready = true;
        }

        private void LateUpdate()
        {
            if (!_ready) return;
            SyncProxies();
            DrawAmbient();
        }

        private void SyncProxies()
        {
            using var entities = _chaffQuery.ToEntityArray(Allocator.Temp);
            using var positions = _chaffQuery.ToComponentDataArray<BeltPosition>(Allocator.Temp);

            _readySet.Clear();
            using (var readyEntities = _readyQuery.ToEntityArray(Allocator.Temp))
            {
                for (int i = 0; i < readyEntities.Length; i++) _readySet.Add(readyEntities[i]);
            }

            _seenThisFrame.Clear();
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                _seenThisFrame.Add(entity);

                if (!_entityToProxy.TryGetValue(entity, out int index))
                {
                    if (_freeProxies.Count == 0) continue; // cap misconfigured above capacity
                    index = _freeProxies.Pop();
                    _entityToProxy[entity] = index;
                    _proxies[index].gameObject.SetActive(true);
                }

                var p = positions[i].Value;
                _proxies[index].position = new Vector3(p.x, p.y, 0f);
                _proxyRenderers[index].color = _readySet.Contains(entity) ? _finishReadyColor : _hotColor;
            }

            _releaseScratch.Clear();
            foreach (var pair in _entityToProxy)
            {
                if (!_seenThisFrame.Contains(pair.Key)) _releaseScratch.Add(pair.Key);
            }
            foreach (var dead in _releaseScratch)
            {
                int index = _entityToProxy[dead];
                _entityToProxy.Remove(dead);
                _proxies[index].gameObject.SetActive(false);
                _freeProxies.Push(index);
            }
        }

        private void DrawAmbient()
        {
            if (_ambientMaterial == null) return;

            using var positions = _ambientQuery.ToComponentDataArray<BeltPosition>(Allocator.Temp);
            if (positions.Length == 0) return;

            if (_ambientMatrices == null || _ambientMatrices.Length < positions.Length)
            {
                _ambientMatrices = new Matrix4x4[positions.Length];
            }

            var scale = new Vector3(_ambientSize, _ambientSize, 1f);
            for (int i = 0; i < positions.Length; i++)
            {
                var p = positions[i].Value;
                // z = 1: ambient behind the chaff proxies (z = 0).
                _ambientMatrices[i] = Matrix4x4.TRS(new Vector3(p.x, p.y, 1f), Quaternion.identity, scale);
            }

            Graphics.DrawMeshInstanced(_quad, 0, _ambientMaterial, _ambientMatrices, positions.Length);
        }

        private static Mesh BuildQuad()
        {
            var mesh = new Mesh
            {
                vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, 0f),
                    new Vector3(0.5f, -0.5f, 0f),
                    new Vector3(-0.5f, 0.5f, 0f),
                    new Vector3(0.5f, 0.5f, 0f)
                },
                uv = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f)
                },
                triangles = new[] { 0, 2, 1, 2, 3, 1 }
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
