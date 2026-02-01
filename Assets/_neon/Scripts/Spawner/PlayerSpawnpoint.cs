using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Marker component placed in level scenes to designate where players spawn.
    /// For co-op support, each spawnpoint has a player index.
    /// </summary>
    public class PlayerSpawnpoint : MonoBehaviour
    {
        [Tooltip("Player index for co-op. Player 1 = 0, Player 2 = 1, etc.")]
        [SerializeField]
        private int _playerIndex;

        [Tooltip("Optional override for the player unit definition. If null, uses the level's default.")]
        [SerializeField]
        private UnitDefinitionAsset _playerDefinitionOverride;

        [Tooltip("The direction the player faces when spawning.")]
        [SerializeField]
        private DIRECTION _spawnDirection = DIRECTION.RIGHT;

        /// <summary>
        /// Player index for co-op support (0-based).
        /// </summary>
        public int PlayerIndex => _playerIndex;

        /// <summary>
        /// Optional override for the player unit definition.
        /// If null, the level's default player definition is used.
        /// </summary>
        public UnitDefinitionAsset PlayerDefinitionOverride => _playerDefinitionOverride;

        /// <summary>
        /// The direction the player faces when spawning.
        /// </summary>
        public DIRECTION SpawnDirection => _spawnDirection;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.right * (int)_spawnDirection * 0.8f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.7f,
                $"Player {_playerIndex + 1}");
#endif
        }
    }
}
