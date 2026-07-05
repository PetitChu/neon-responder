using UnityEngine;
using Unity.Cinemachine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Owns the PolygonCollider2D used by a CinemachineConfiner2D and rebuilds it
    /// from a right-edge arena bound (replaces the old CameraFollow.levelBound clamp).
    /// The left edge never retreats past where the run started. Must live on a STATIC
    /// GameObject (SetPath is local-space and Confiner2D tracks the shape's transform,
    /// so a moving holder would drag the confinement region along); the Confiner2D
    /// extension on the vcam references this collider.
    /// </summary>
    [RequireComponent(typeof(PolygonCollider2D))]
    public class NeonCameraConfinerDriver : MonoBehaviour
    {
        [SerializeField] private CinemachineConfiner2D _confiner;
        [SerializeField] private CinemachineCamera _vcam;

        private PolygonCollider2D _shape;
        private float _leftX;
        private bool _initialized;

        void Awake()
        {
            _shape = GetComponent<PolygonCollider2D>();
            if (_confiner == null) _confiner = GetComponent<CinemachineConfiner2D>();
            if (_vcam == null) _vcam = GetComponent<CinemachineCamera>();
        }

        /// <summary>Set the arena right-edge world X. Call from Level per wave.</summary>
        public void SetRightBound(float rightBoundX)
        {
            float halfHeight = _vcam != null ? _vcam.Lens.OrthographicSize : 5f;
            float halfWidth = halfHeight * ((float)Screen.width / Screen.height);
            float centerY = transform.position.y;
            if (!_initialized) { _leftX = rightBoundX - halfWidth * 2f; _initialized = true; }

            Rect r = NeonCameraBounds.ConfinerRect(rightBoundX, halfWidth, halfHeight, centerY, _leftX);
            _leftX = r.xMin;

            _shape.pathCount = 1;
            _shape.SetPath(0, new[]
            {
                new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin),
                new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax),
            });
            if (_confiner != null) _confiner.InvalidateBoundingShapeCache();
        }
    }
}
