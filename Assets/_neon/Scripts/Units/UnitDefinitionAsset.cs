using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Configuration asset that defines a unit type.
    /// Used by the spawning system to know what to instantiate and how to configure it.
    /// </summary>
    [CreateAssetMenu(fileName = "UnitDefinition", menuName = "Neon/Units/Unit Definition")]
    public class UnitDefinitionAsset : ScriptableObject
    {
        [SerializeField]
        private string _unitId;

        [SerializeField]
        private string _displayName;

        [SerializeField]
        private UNITTYPE _unitType;

        [SerializeField]
        private GameObject _prefab;

        [SerializeField]
        private Sprite _portrait;

        [SerializeField]
        private int _maxHealth = 1;

        /// <summary>
        /// Unique identifier for this unit definition.
        /// </summary>
        public string UnitId => _unitId;

        /// <summary>
        /// Display name shown in UI.
        /// </summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// The type of unit (Player, Enemy, NPC).
        /// </summary>
        public UNITTYPE UnitType => _unitType;

        /// <summary>
        /// The prefab to instantiate for this unit.
        /// </summary>
        public GameObject Prefab => _prefab;

        /// <summary>
        /// Portrait sprite for UI display.
        /// </summary>
        public Sprite Portrait => _portrait;

        /// <summary>
        /// Maximum health for this unit.
        /// </summary>
        public int MaxHealth => _maxHealth;
    }
}
