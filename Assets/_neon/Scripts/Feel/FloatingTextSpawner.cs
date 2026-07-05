using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using VContainer;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Spec §5.5 signs: +N XP/Charge popups + callouts. Pure consumer. Pools
    /// world-space TextMesh labels that rise + fade on UNSCALED time (readable
    /// during hitstop/freeze). ObjectiveCompleted → "NODE RESTORED"; Callout → its text.
    /// </summary>
    public class FloatingTextSpawner : MonoBehaviour
    {
        [SerializeField] private TextMesh labelPrefab;   // a world-space 3D Text prefab
        [SerializeField] private int poolSize = 24;
        [SerializeField] private float riseSpeed = 1.5f;
        [SerializeField] private float lifeSeconds = 0.9f;
        [SerializeField] private Color xpColor = new(0.6f, 0.9f, 1f);
        [SerializeField] private Color chargeColor = new(1f, 0.85f, 0.2f);
        [SerializeField] private Color calloutColor = new(0.4f, 1f, 0.6f);

        [Inject] private IGameplaySignals _signals;

        private readonly List<TextMesh> _pool = new();
        private readonly List<float> _spawnedAt = new();
        private int _next;
        private readonly List<IDisposable> _subs = new();

        void Start()
        {
            if (_signals == null || labelPrefab == null) { enabled = false; return; }

            for (int i = 0; i < poolSize; i++)
            {
                var label = Instantiate(labelPrefab, transform);
                label.gameObject.SetActive(false);
                _pool.Add(label);
                _spawnedAt.Add(0f);
            }

            _subs.Add(_signals.On<XpGained>().Subscribe(e => Spawn($"+{e.Amount} XP", PlayerPos(), xpColor)));
            _subs.Add(_signals.On<NeonChargeChanged>().Subscribe(OnCharge));
            _subs.Add(_signals.On<Callout>().Subscribe(e => Spawn(e.Text, e.Position, calloutColor)));
            _subs.Add(_signals.On<ObjectiveCompleted>().Subscribe(_ => Spawn("NODE RESTORED", PlayerPos(), calloutColor)));
        }

        void OnDestroy()
        {
            foreach (var sub in _subs) sub.Dispose();
            _subs.Clear();
        }

        private int _lastCharge;

        private void OnCharge(NeonChargeChanged e)
        {
            int delta = e.Total - _lastCharge;
            _lastCharge = e.Total;
            if (delta > 0) Spawn($"+{delta} ⚡", PlayerPos(), chargeColor); // gains only; spends are silent
        }

        void LateUpdate()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                var label = _pool[i];
                if (!label.gameObject.activeSelf) continue;

                float age = Time.unscaledTime - _spawnedAt[i];
                if (age >= lifeSeconds) { label.gameObject.SetActive(false); continue; }

                label.transform.position += Vector3.up * (riseSpeed * Time.unscaledDeltaTime);
                var c = label.color;
                c.a = Mathf.Lerp(1f, 0f, age / lifeSeconds);
                label.color = c;
            }
        }

        private void Spawn(string text, Vector2 worldPos, Color color)
        {
            int slot = _next;
            _next = (_next + 1) % _pool.Count;

            var label = _pool[slot];
            label.text = text;
            label.color = color;
            label.transform.position = new Vector3(worldPos.x, worldPos.y + 1f, 0f);
            label.gameObject.SetActive(true);
            _spawnedAt[slot] = Time.unscaledTime;
        }

        private Vector2 PlayerPos()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            return player != null ? (Vector2)player.transform.position : Vector2.zero;
        }
    }
}
