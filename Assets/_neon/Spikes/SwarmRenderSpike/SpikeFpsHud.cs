using UnityEngine;

namespace BrainlessLabs.Neon.SwarmRenderSpike
{
    public class SpikeFpsHud : MonoBehaviour
    {
        private float _smoothedDelta;

        private void Update()
        {
            _smoothedDelta = _smoothedDelta <= 0f
                ? Time.unscaledDeltaTime
                : Mathf.Lerp(_smoothedDelta, Time.unscaledDeltaTime, 0.05f);
        }

        private void OnGUI()
        {
            float fps = _smoothedDelta > 0f ? 1f / _smoothedDelta : 0f;
            var style = new GUIStyle(GUI.skin.label) { fontSize = 22 };
            GUI.Label(new Rect(10, 10, 500, 30), $"FPS (smoothed): {fps:F1}", style);
        }
    }
}
