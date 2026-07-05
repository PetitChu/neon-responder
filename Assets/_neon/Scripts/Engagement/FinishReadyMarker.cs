using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Hero-tier Finish-Ready flag + glow sign (spec §5.1). Added at runtime by
    /// FinishReadySystem; FinishResolver reads IsReady at the hit seam.
    /// </summary>
    public class FinishReadyMarker : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;
        private Color _originalColor;
        private bool _hasRenderer;

        public bool IsReady { get; private set; }

        private void Awake()
        {
            var settings = GetComponent<UnitSettings>();
            _spriteRenderer = settings != null && settings.spriteRenderer != null
                ? settings.spriteRenderer
                : GetComponent<SpriteRenderer>();
            _hasRenderer = _spriteRenderer != null;
            if (_hasRenderer) _originalColor = _spriteRenderer.color;
        }

        public void SetReady(bool ready, Color glowColor)
        {
            if (IsReady == ready) return;
            IsReady = ready;
            if (_hasRenderer) _spriteRenderer.color = ready ? glowColor : _originalColor;
        }
    }
}
