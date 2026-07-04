using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace BrainlessLabs.Neon.SwarmRenderSpike
{
    /// <summary>
    /// R6 hot-chaff path: a pool of SpriteRenderer proxies whose transforms are
    /// copied from ECS each LateUpdate. Index-based mapping is fine for the spike
    /// (perf question only); the real bridge will need stable entity↔proxy mapping.
    /// </summary>
    public class HotProxyPool : MonoBehaviour
    {
        [SerializeField] private Sprite _sprite;
        [SerializeField] private Color _hotColor = new(1f, 0.25f, 0.6f, 1f);
        [SerializeField] private int _capacity = 150;

        private Transform[] _proxies;
        private EntityQuery _query;
        private bool _ready;

        private void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                Debug.LogError("[Spike] HotProxyPool: no default ECS world.");
                return;
            }
            if (_sprite == null)
            {
                Debug.LogError("[Spike] HotProxyPool: no sprite assigned.");
                return;
            }

            _query = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<SpikePosition>(),
                ComponentType.ReadOnly<HotAgentTag>());

            _proxies = new Transform[_capacity];
            for (int i = 0; i < _capacity; i++)
            {
                var proxy = new GameObject($"HotProxy_{i}");
                proxy.transform.SetParent(transform, worldPositionStays: false);
                var spriteRenderer = proxy.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = _sprite;
                spriteRenderer.color = _hotColor;
                proxy.SetActive(false);
                _proxies[i] = proxy.transform;
            }

            _ready = true;
        }

        private void LateUpdate()
        {
            if (!_ready)
            {
                return;
            }

            using var positions = _query.ToComponentDataArray<SpikePosition>(Allocator.Temp);
            int active = Mathf.Min(positions.Length, _proxies.Length);

            for (int i = 0; i < active; i++)
            {
                var proxy = _proxies[i];
                if (!proxy.gameObject.activeSelf)
                {
                    proxy.gameObject.SetActive(true);
                }
                var p = positions[i].Value;
                proxy.localPosition = new Vector3(p.x, p.y, 0f);
            }

            for (int i = active; i < _proxies.Length; i++)
            {
                if (_proxies[i].gameObject.activeSelf)
                {
                    _proxies[i].gameObject.SetActive(false);
                }
            }
        }
    }
}
