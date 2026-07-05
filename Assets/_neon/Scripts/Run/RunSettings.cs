using System;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Global run tuning (spec §5.4 / §9). Per-level encounter geometry lives on
    /// LevelConfigurationAsset.Run; these are the run-wide knobs.
    /// </summary>
    [Serializable]
    public class RunSettings : ISettings
    {
        [Header("Cadence")]
        [SerializeField] private float _encounterIntroSeconds = 1.5f;
        [SerializeField] private float _rebootDurationSeconds = 50f; // §9: 45–60s

        [Header("Signal (dawn = win)")]
        [SerializeField] private float _dawnValue = 1f;
        [Tooltip("Extra chaff density at full dawn, as a fraction (1 = ×2 at dawn).")]
        [SerializeField] private float _maxSpawnNastinessBonus = 1f;

        [Header("Shop (Heal + Continue; Specials arrive M4)")]
        [SerializeField] private int _shopHealCost = 25;   // special-moves doc §3
        [SerializeField] private int _shopHealAmount = 40;
        [SerializeField, Range(0f, 1f)] private float _shopPauseScale = 0f;

        [Header("Darkness lerp")]
        [SerializeField] private Color _nightColor = new(0.04f, 0.05f, 0.10f, 1f);
        [SerializeField] private Color _dawnColor = new(0.45f, 0.35f, 0.30f, 1f);

        public float EncounterIntroSeconds => _encounterIntroSeconds;
        public float RebootDurationSeconds => _rebootDurationSeconds;
        public float DawnValue => _dawnValue;
        public float MaxSpawnNastinessBonus => _maxSpawnNastinessBonus;
        public int ShopHealCost => _shopHealCost;
        public int ShopHealAmount => _shopHealAmount;
        public float ShopPauseScale => _shopPauseScale;
        public Color NightColor => _nightColor;
        public Color DawnColor => _dawnColor;

#if UNITY_EDITOR
        public void Editor_OnGUI(UnityEngine.Object target)
        {
            var serializedObject = new UnityEditor.SerializedObject(target);
            serializedObject.UpdateIfRequiredOrScript();
            var settingsProperty = serializedObject.FindProperty("_settings");
            UnityEditor.EditorGUILayout.PropertyField(settingsProperty, new GUIContent("Run Settings"), includeChildren: true);
            serializedObject.ApplyModifiedProperties();
        }
#endif
    }
}
