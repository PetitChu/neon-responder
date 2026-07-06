using UnityEngine;
using UnityEngine.Rendering; // Volume (Core.Runtime)

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Fullscreen whiff desaturate. Owns a high-priority global URP Volume whose profile grades
    /// saturation to grey; Pulse() drives its weight 1→0 (WhiffFx curve) on unscaled time.
    /// (URP port of the Plan B PPv2 component — same weight-pulse design.)
    /// Replaces M4's red uGUI flash. Lives in each swarm/combat level scene.
    /// </summary>
    public class WhiffPostFx : MonoBehaviour
    {
        [SerializeField] private Volume _whiffVolume; // isGlobal, priority > base, ColorAdjustments saturation -100, weight 0 at rest

        private float _elapsed, _duration;
        private bool _active;

        public void Pulse(float seconds)
        {
            _duration = Mathf.Max(0.01f, seconds);
            _elapsed = 0f;
            _active = true;
        }

        private void Update()
        {
            if (!_active || _whiffVolume == null) return;
            _elapsed += Time.unscaledDeltaTime; // whiff often coincides with hitstop — unscaled, like the old routine
            _whiffVolume.weight = WhiffFx.WeightAt(_elapsed, _duration);
            if (_elapsed >= _duration) { _whiffVolume.weight = 0f; _active = false; }
        }
    }
}
