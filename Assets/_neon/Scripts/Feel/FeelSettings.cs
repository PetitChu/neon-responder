using System;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Feel + actives tuning (spec §5.5). Per-verb hitstop/shake profiles, tier-up
    /// flourish, whiff flash, finisher freeze, and the Actives knobs (Siren Pulse +
    /// Overcharge finisher). All playtest starting values.
    /// </summary>
    [Serializable]
    public class FeelSettings : ISettings
    {
        [Header("Per-verb hit profiles (hitstopScale, hitstopSeconds, shakeIntensity, shakeSeconds)")]
        [SerializeField] private HitProfile _punch = new(0.08f, 0.04f, 0.10f, 0.15f);
        [SerializeField] private HitProfile _kick = new(0.06f, 0.06f, 0.16f, 0.20f);
        [SerializeField] private HitProfile _weapon = new(0.05f, 0.07f, 0.20f, 0.22f);
        [SerializeField] private HitProfile _throw = new(0.03f, 0.10f, 0.35f, 0.35f); // biggest hit in the kit
        [SerializeField] private HitProfile _defaultHit = new(0.10f, 0.03f, 0.08f, 0.12f);
        [SerializeField] private HitProfile _finish = new(0.04f, 0.09f, 0.28f, 0.30f);

        [Header("Flourishes")]
        [SerializeField] private HitProfile _tierUp = new(0.15f, 0.08f, 0.22f, 0.30f);
        [SerializeField] private float _finisherFreezeSeconds = 0.35f;
        [SerializeField] private float _whiffFlashSeconds = 0.25f;

        [Header("Actives — Siren Pulse (special-moves doc §2)")]
        [SerializeField] private float _sirenCooldownSeconds = 6f;
        [SerializeField] private int _sirenChargeCost = 20;
        [SerializeField] private float _sirenRadius = 5f;

        [Header("Actives — Overcharge finisher (meter-gated; R4 clears chaff)")]
        [SerializeField] private float _finisherRadius = 12f; // screen-ish

        public HitProfile Punch => _punch;
        public HitProfile Kick => _kick;
        public HitProfile Weapon => _weapon;
        public HitProfile Throw => _throw;
        public HitProfile DefaultHit => _defaultHit;
        public HitProfile Finish => _finish;
        public HitProfile TierUp => _tierUp;
        public float FinisherFreezeSeconds => _finisherFreezeSeconds;
        public float WhiffFlashSeconds => _whiffFlashSeconds;
        public float SirenCooldownSeconds => _sirenCooldownSeconds;
        public int SirenChargeCost => _sirenChargeCost;
        public float SirenRadius => _sirenRadius;
        public float FinisherRadius => _finisherRadius;

#if UNITY_EDITOR
        public void Editor_OnGUI(UnityEngine.Object target)
        {
            var serializedObject = new UnityEditor.SerializedObject(target);
            serializedObject.UpdateIfRequiredOrScript();
            var settingsProperty = serializedObject.FindProperty("_settings");
            UnityEditor.EditorGUILayout.PropertyField(settingsProperty, new GUIContent("Feel Settings"), includeChildren: true);
            serializedObject.ApplyModifiedProperties();
        }
#endif
    }
}
