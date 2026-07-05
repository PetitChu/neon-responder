using UnityEngine;

namespace BrainlessLabs.Neon
{
    public interface IAudioService
    {
        void PlaySFX(string name, Vector3? pos = null, Transform parent = null);
        float GetSFXDuration(string name);
        void PlayMusic(string name);

        /// <summary>Crossfade the music bed to another track over <paramref name="seconds"/>. No-op if already on it.</summary>
        void CrossfadeMusic(string name, float seconds);
    }
}
