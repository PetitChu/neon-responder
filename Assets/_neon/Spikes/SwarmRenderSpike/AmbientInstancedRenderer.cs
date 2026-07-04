using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace BrainlessLabs.Neon.SwarmRenderSpike
{
    /// <summary>
    /// R6 ambient path: draws every ambient agent as an instanced textured quad.
    /// The assigned material must have "Enable GPU Instancing" ticked.
    /// </summary>
    public class AmbientInstancedRenderer : MonoBehaviour
    {
        [SerializeField] private Material _material;
        [SerializeField] private float _agentSize = 1f;

        private Mesh _quad;
        private EntityQuery _query;
        private Matrix4x4[] _matrices;
        private bool _ready;

        private void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                Debug.LogError("[Spike] AmbientInstancedRenderer: no default ECS world.");
                return;
            }
            if (_material == null)
            {
                Debug.LogError("[Spike] AmbientInstancedRenderer: no material assigned.");
                return;
            }
            if (!_material.enableInstancing)
            {
                Debug.LogError("[Spike] AmbientInstancedRenderer: material must have GPU instancing enabled.");
                return;
            }

            _query = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<SpikePosition>(),
                ComponentType.ReadOnly<AmbientAgentTag>());
            _quad = BuildQuad();
            _ready = true;
        }

        private void Update()
        {
            if (!_ready)
            {
                return;
            }

            using var positions = _query.ToComponentDataArray<SpikePosition>(Allocator.Temp);
            if (positions.Length == 0)
            {
                return;
            }

            if (_matrices == null || _matrices.Length < positions.Length)
            {
                _matrices = new Matrix4x4[positions.Length];
            }

            var scale = new Vector3(_agentSize, _agentSize, 1f);
            for (int i = 0; i < positions.Length; i++)
            {
                var p = positions[i].Value;
                // z = 1 puts ambient behind the hot proxies (z = 0).
                _matrices[i] = Matrix4x4.TRS(new Vector3(p.x, p.y, 1f), Quaternion.identity, scale);
            }

            // Explicit worldBounds: the default zero-size bounds can get the whole
            // instanced draw frustum-culled.
            var renderParams = new RenderParams(_material)
            {
                worldBounds = new Bounds(Vector3.zero, new Vector3(200f, 50f, 10f))
            };
            Graphics.RenderMeshInstanced(renderParams, _quad, 0, _matrices, positions.Length);
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
