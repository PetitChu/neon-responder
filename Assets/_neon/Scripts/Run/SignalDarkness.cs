using System;
using R3;
using UnityEngine;
using VContainer;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Night→dawn background lerp driven by the Signal (spec §5.4/§5.5). Pure consumer:
    /// lerps the main camera's background color from RunSettings.NightColor to DawnColor
    /// by Signal fraction, so "reaching dawn" is legible. Music aggression is the OTHER
    /// Signal consumer — deferred to M4 (spec §7 M4).
    /// </summary>
    public class SignalDarkness : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera; // defaults to Camera.main
        [Inject] private IGameplaySignals _signals;
        private RunSettings _settings;
        private IDisposable _subscription;

        void Start()
        {
            if (_signals == null) return; // scene without DI injection
            _settings = RunSettingsAsset.InstanceAsset.Settings;
            if (targetCamera == null) targetCamera = Camera.main;
            Apply(new SignalChanged(0f, 1f));
            _subscription = _signals.On<SignalChanged>().Subscribe(Apply);
        }

        void OnDestroy() => _subscription?.Dispose();

        void Apply(SignalChanged signal)
        {
            if (targetCamera == null || _settings == null) return;
            float t = signal.Dawn > 0f ? Mathf.Clamp01(signal.Value / signal.Dawn) : 0f;
            targetCamera.backgroundColor = Color.Lerp(_settings.NightColor, _settings.DawnColor, t);
        }
    }
}
