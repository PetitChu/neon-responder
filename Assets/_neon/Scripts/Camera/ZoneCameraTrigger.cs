using UnityEngine;
using Unity.Cinemachine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// When the player enters this 2D trigger, promote the assigned zone vcam above the
    /// base vcam so the CinemachineBrain blends to the zone's framing/zoom. On exit, demote.
    /// Level 01 places one per corridor/plaza zone (authored in Plan C).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ZoneCameraTrigger : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera _zoneVcam;
        [SerializeField] private int _activePriority = 20;
        [SerializeField] private int _inactivePriority = 5;
        [SerializeField] private string _playerTag = "Player";

        void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        void Start()
        {
            if (_zoneVcam != null) _zoneVcam.Priority = _inactivePriority;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_zoneVcam != null && other.CompareTag(_playerTag))
                _zoneVcam.Priority = _activePriority;
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (_zoneVcam != null && other.CompareTag(_playerTag))
                _zoneVcam.Priority = _inactivePriority;
        }
    }
}
