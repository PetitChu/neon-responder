using System;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Tuning for the M1 engagement spine. Numbers from spec §5.1/§9 and the
    /// protocol doc's guardrails (auto-rate hard cap 6/s, arc cap 180°,
    /// Momentum the only multiplier at ×1.0/1.3/1.7/2.5).
    /// </summary>
    [Serializable]
    public class EngagementSettings : ISettings
    {
        [Header("Auto-engage")]
        [SerializeField] private float _autoEngageRatePerSecond = 1.5f;
        [SerializeField] private int _autoEngageChipDamage = 8;
        [SerializeField] private float _autoEngageArcDegrees = 120f;
        [SerializeField] private float _autoEngageRange = 4f;

        [Header("Finish-Ready")]
        [SerializeField, Range(0f, 1f)] private float _finishReadyHealthThreshold = 0.25f;
        [SerializeField] private Color _finishReadyGlow = new(1f, 0.85f, 0.2f, 1f);

        [Header("Momentum")]
        [SerializeField] private int _momentumStepsPerTier = 3;
        [SerializeField] private float _momentumDecaySeconds = 2.5f;
        [SerializeField] private float[] _momentumTierMultipliers = { 1f, 1.3f, 1.7f, 2.5f };
        [SerializeField] private float _whiffStaggerSeconds = 0.5f;

        [Header("Chaff")]
        [SerializeField] private int _chaffMaxHealth = 24;

        public float AutoEngageRatePerSecond => _autoEngageRatePerSecond;
        public int AutoEngageChipDamage => _autoEngageChipDamage;
        public float AutoEngageArcDegrees => _autoEngageArcDegrees;
        public float AutoEngageRange => _autoEngageRange;
        public float FinishReadyHealthThreshold => _finishReadyHealthThreshold;
        public Color FinishReadyGlow => _finishReadyGlow;
        public int MomentumStepsPerTier => _momentumStepsPerTier;
        public float MomentumDecaySeconds => _momentumDecaySeconds;
        public float[] MomentumTierMultipliers => _momentumTierMultipliers;
        public float WhiffStaggerSeconds => _whiffStaggerSeconds;
        public int ChaffMaxHealth => _chaffMaxHealth;

#if UNITY_EDITOR
        public void Editor_OnGUI(UnityEngine.Object target)
        {
            var serializedObject = new UnityEditor.SerializedObject(target);
            serializedObject.UpdateIfRequiredOrScript();
            var settingsProperty = serializedObject.FindProperty("_settings");
            UnityEditor.EditorGUILayout.PropertyField(settingsProperty, new GUIContent("Engagement Settings"), includeChildren: true);
            serializedObject.ApplyModifiedProperties();
        }
#endif
    }
}
