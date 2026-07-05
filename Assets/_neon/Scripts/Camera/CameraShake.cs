using UnityEngine;
using Unity.Cinemachine;

namespace BrainlessLabs.Neon {

    /// <summary>
    /// Camera shake, now backed by a Cinemachine impulse (was: CameraFollow.additionalYOffset bob).
    /// Public API (ShowCamShake) is unchanged so FeedbackSystem / UnitActions / DoCamShake are untouched.
    /// Lives on the Main Camera; a CinemachineImpulseListener on the base vcam consumes the impulse.
    /// </summary>
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class CameraShake : MonoBehaviour {

        // Serialized fields kept for scene/inspector compatibility (curve now unused by the impulse path).
        public AnimationCurve CameraShakeAnimation;
        public float intensity = .15f;
        public float duration = .3f;

        private CinemachineImpulseSource _source;

        void Awake() {
            _source = GetComponent<CinemachineImpulseSource>();
        }

        //use default settings
        public void ShowCamShake() {
            ShowCamShake(intensity, duration);
        }

        //use custom settings
        public void ShowCamShake(float _intensity, float _duration) {
            if (_source == null) return;
            _source.ImpulseDefinition.ImpulseDuration = Mathf.Max(0.01f, _duration);
            _source.GenerateImpulseWithForce(_intensity);
        }
    }
}
