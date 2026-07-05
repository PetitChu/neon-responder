using System;
using R3;
using UnityEngine;
using VContainer;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Scene visual for the active Reboot Node (spec §5.5 "objective bar + zone glow").
    /// Pure consumer of ObjectiveProgress: moves a world marker to the node, tints it
    /// by fill, hides it when no objective is active. No gameplay logic.
    /// </summary>
    public class RebootNodeVisual : MonoBehaviour
    {
        [SerializeField] private Transform marker;          // a world-space sprite at the node
        [SerializeField] private SpriteRenderer glow;       // tinted by fill
        [SerializeField] private Color emptyColor = new(0.2f, 0.6f, 1f, 0.35f);
        [SerializeField] private Color fullColor = new(0.3f, 1f, 0.5f, 0.85f);
        [Inject] private IGameplaySignals _signals;
        private IDisposable _subscription;

        void Start()
        {
            if (_signals == null) return; // scene without DI injection
            if (marker != null) marker.gameObject.SetActive(false);
            _subscription = _signals.On<ObjectiveProgress>().Subscribe(Apply);
        }

        void OnDestroy() => _subscription?.Dispose();

        void Apply(ObjectiveProgress p)
        {
            if (marker == null) return;
            // Normalized == 1 means completed this frame; the run advances past the
            // objective, so a fresh ObjectiveProgress with a new position re-shows it.
            marker.gameObject.SetActive(p.Normalized < 1f);
            marker.position = new Vector3(p.Position.x, p.Position.y, marker.position.z);
            if (glow != null) glow.color = Color.Lerp(emptyColor, fullColor, p.Normalized);
        }
    }
}
