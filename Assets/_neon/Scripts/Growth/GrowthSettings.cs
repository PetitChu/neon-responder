using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// M2 growth tuning. Economy/progression numbers from protocol doc §8.3;
    /// challenge shape per decision P2. All are playtest starting knobs.
    /// </summary>
    [Serializable]
    public class GrowthSettings : ISettings
    {
        [Header("Economy (doc §8.3 — gains are ×Run.GainMultiplier)")]
        [SerializeField] private int _xpPerKill = 1;
        [SerializeField] private int _chargePerFinish = 2;
        [SerializeField] private int _overchargePerFinish = 8;
        [SerializeField] private int _overchargeCap = 100;

        [Header("Progression (XP cost to clear level N = ceil(base × N^exponent))")]
        [SerializeField] private float _xpCostBase = 10f;
        [SerializeField] private float _xpCostExponent = 1.35f;
        [SerializeField, Range(0.01f, 1f)] private float _levelUpSlowMoScale = 0.1f;

        [Header("Protocol catalog (the draft pool)")]
        [SerializeField] private List<ProtocolDefinitionAsset> _protocolCatalog = new();

        [Header("Hero finish challenge (P2 — chaff stay single-verb)")]
        [SerializeField] private ATTACKTYPE[] _challengeSequenceBase = { ATTACKTYPE.PUNCH, ATTACKTYPE.KICK };
        [SerializeField] private ATTACKTYPE[] _challengeSequenceHot = { ATTACKTYPE.PUNCH, ATTACKTYPE.KICK, ATTACKTYPE.PUNCH };
        [SerializeField] private float _challengeInputWindowSeconds = 0.9f;
        [SerializeField] private float _challengeWindowTightenPerTier = 0.1f;

        [Header("Protocol effect hooks")]
        [SerializeField] private int _finishAoeDamage = 6;

        public int XpPerKill => _xpPerKill;
        public int ChargePerFinish => _chargePerFinish;
        public int OverchargePerFinish => _overchargePerFinish;
        public int OverchargeCap => _overchargeCap;
        public float XpCostBase => _xpCostBase;
        public float XpCostExponent => _xpCostExponent;
        public float LevelUpSlowMoScale => _levelUpSlowMoScale;
        public List<ProtocolDefinitionAsset> ProtocolCatalog => _protocolCatalog;
        public ATTACKTYPE[] ChallengeSequenceBase => _challengeSequenceBase;
        public ATTACKTYPE[] ChallengeSequenceHot => _challengeSequenceHot;
        public float ChallengeInputWindowSeconds => _challengeInputWindowSeconds;
        public float ChallengeWindowTightenPerTier => _challengeWindowTightenPerTier;
        public int FinishAoeDamage => _finishAoeDamage;

#if UNITY_EDITOR
        public void Editor_OnGUI(UnityEngine.Object target)
        {
            var serializedObject = new UnityEditor.SerializedObject(target);
            serializedObject.UpdateIfRequiredOrScript();
            var settingsProperty = serializedObject.FindProperty("_settings");
            UnityEditor.EditorGUILayout.PropertyField(settingsProperty, new GUIContent("Growth Settings"), includeChildren: true);
            serializedObject.ApplyModifiedProperties();
        }
#endif
    }
}
