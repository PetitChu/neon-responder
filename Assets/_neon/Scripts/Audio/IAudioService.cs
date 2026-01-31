using UnityEngine;

namespace BrainlessLabs.Neon
{
    public interface IAudioService
    {
        void PlaySFX(string name, Vector3? pos = null, Transform parent = null);
        float GetSFXDuration(string name);
        void PlayMusic(string name);
    }
}
