using UnityEngine;
using UnityEngine.Audio;

namespace BrainlessLabs.Neon
{
    [CreateAssetMenu(fileName = "AudioConfiguration", menuName = "Neon/Audio/Audio Configuration")]
    public class AudioConfigurationAsset : ScriptableObject
    {
        [SerializeField]
        private AudioMixerGroup _mixerGroup;

        [SerializeField]
        private AudioItem[] _audioItems = System.Array.Empty<AudioItem>();

        public AudioMixerGroup MixerGroup => _mixerGroup;
        public AudioItem[] AudioItems => _audioItems;
    }
}
