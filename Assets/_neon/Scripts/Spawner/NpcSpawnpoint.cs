using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Marker component placed in level scenes to designate where an NPC spawns.
    /// Each spawnpoint references a UnitDefinitionAsset to identify which NPC to spawn.
    /// </summary>
    public class NpcSpawnpoint : MonoBehaviour
    {
        [Tooltip("The NPC unit definition that determines which NPC to spawn here.")]
        [SerializeField]
        private UnitDefinitionAsset _npcDefinition;

        [Tooltip("The direction the NPC faces when spawning.")]
        [SerializeField]
        private DIRECTION _spawnDirection = DIRECTION.RIGHT;

        [Tooltip("If true, the NPC spawns immediately when the level loads. If false, it must be triggered.")]
        [SerializeField]
        private bool _spawnOnLevelLoad = true;

        /// <summary>
        /// The NPC unit definition that determines which NPC to spawn here.
        /// </summary>
        public UnitDefinitionAsset NpcDefinition => _npcDefinition;

        /// <summary>
        /// The direction the NPC faces when spawning.
        /// </summary>
        public DIRECTION SpawnDirection => _spawnDirection;

        /// <summary>
        /// Whether the NPC spawns immediately when the level loads.
        /// </summary>
        public bool SpawnOnLevelLoad => _spawnOnLevelLoad;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.right * (int)_spawnDirection * 0.8f);

#if UNITY_EDITOR
            string label = _npcDefinition != null ? _npcDefinition.DisplayName : "No NPC Assigned";
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.7f,
                label);
#endif
        }
    }
}
