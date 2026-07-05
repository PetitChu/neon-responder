using UnityEngine;
using Unity.Cinemachine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Binds a CinemachineCamera's Follow target to the runtime-spawned Player.
    /// Retries until the Player tag exists (Level spawns the player after boot).
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera))]
    public class CinemachineTargetBinder : MonoBehaviour
    {
        [SerializeField] private string _targetTag = "Player";
        private CinemachineCamera _vcam;

        void Awake() => _vcam = GetComponent<CinemachineCamera>();

        void Update()
        {
            if (_vcam.Follow != null) return;
            var target = GameObject.FindGameObjectWithTag(_targetTag);
            if (target != null) _vcam.Follow = target.transform;
        }
    }
}
