using System.Linq;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    public class AudioService : IAudioService, System.IDisposable
    {
        private readonly AudioConfigurationAsset _sfxConfiguration;
        private readonly AudioConfigurationAsset _musicConfiguration;

        private GameObject _musicObject;
        private AudioSource _musicSource;

        public AudioService()
        {
            var settings = AudioSettingsAsset.InstanceAsset.Settings;
            _sfxConfiguration = settings.SfxConfiguration;
            _musicConfiguration = settings.MusicConfiguration;
        }

        public void PlaySFX(string name = "", Vector3? pos = null, Transform parent = null)
        {
            PlaySFXInternal(name, pos, parent);
        }

        public float GetSFXDuration(string name)
        {
            return GetSFXDurationInternal(name);
        }

        public void PlayMusic(string name)
        {
            PlayMusicInternal(name);
        }

        private void PlaySFXInternal(string name, Vector3? pos, Transform parent)
        {
            if (string.IsNullOrEmpty(name) || _sfxConfiguration == null) return;

            Transform cameraTransform = null;

            // Determine final position without touching Camera.main unless needed
            Vector3 finalPos;
            if (pos.HasValue)
            {
                finalPos = pos.Value;
            }
            else
            {
                var mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    cameraTransform = mainCamera.transform;
                    finalPos = cameraTransform.position;
                }
                else
                {
                    // Fallback position when no MainCamera exists
                    finalPos = Vector3.zero;
                }
            }

            // Determine final parent without touching Camera.main unless needed
            Transform finalParent;
            if (parent != null)
            {
                finalParent = parent;
            }
            else
            {
                if (cameraTransform == null)
                {
                    var mainCamera = Camera.main;
                    cameraTransform = mainCamera != null ? mainCamera.transform : null;
                }

                finalParent = cameraTransform;
            }
            var audioItems = _sfxConfiguration.AudioItems;
            var matchingItem = audioItems.FirstOrDefault(item => item.name == name);

            if (matchingItem == null)
            {
                Debug.LogWarning($"No audio item found with name: {name}");
                return;
            }

            if (matchingItem.clip.Length == 0)
            {
                Debug.LogWarning($"AudioClip '{name}' has no clips assigned in the SFX configuration.");
                return;
            }

            if (Time.time - matchingItem.lastTimePlayed < matchingItem.minTimeBetweenCall) return;
            matchingItem.lastTimePlayed = Time.time;

            var audioObj = new GameObject($"AudioSFX_{name}");
            audioObj.transform.parent = Mathf.Abs(matchingItem.range) <= Mathf.Epsilon ? cameraTransform : finalParent;
            audioObj.transform.position = finalPos;

            var source = audioObj.AddComponent<AudioSource>();
            var rand = Random.Range(0, matchingItem.clip.Length);
            source.clip = matchingItem.clip[rand];
            source.spatialBlend = 1.0f;
            source.pitch = 1f + Random.Range(-matchingItem.randomPitch, matchingItem.randomPitch);
            source.volume = matchingItem.volume + Random.Range(-matchingItem.randomVolume, matchingItem.randomVolume);
            source.outputAudioMixerGroup = _sfxConfiguration.MixerGroup;
            source.rolloffMode = AudioRolloffMode.Custom;
            source.loop = matchingItem.loop;

            if (matchingItem.range > 0) source.maxDistance = matchingItem.range;
            if (matchingItem.range > 3) source.minDistance = source.maxDistance - 3;

            source.Play();

            if (!matchingItem.loop && source.clip != null)
                Object.Destroy(audioObj, source.clip.length + Time.deltaTime);
        }

        private float GetSFXDurationInternal(string name)
        {
            if (_sfxConfiguration == null) return 0;

            var matchingItem = _sfxConfiguration.AudioItems.FirstOrDefault(item => item.name == name);

            if (matchingItem == null)
            {
                Debug.LogWarning($"No audio item found with name: {name}");
                return 0;
            }

            if (matchingItem.clip == null || matchingItem.clip.Length == 0)
            {
                Debug.LogWarning($"AudioClip '{name}' has no clips assigned in the sfx configuration.");
                return 0;
            }

            float maxDuration = 0f;
            for (int i = 0; i < matchingItem.clip.Length; i++)
            {
                var clip = matchingItem.clip[i];
                if (clip == null) continue;
                if (clip.length > maxDuration)
                {
                    maxDuration = clip.length;
                }
            }

            return maxDuration;
        }

        private void PlayMusicInternal(string name)
        {
            if (string.IsNullOrEmpty(name) || _musicConfiguration == null) return;

            var matchingItem = _musicConfiguration.AudioItems.FirstOrDefault(item => item.name == name);

            if (matchingItem == null)
            {
                Debug.LogWarning($"No music item found with name: {name}");
                return;
            }

            if (matchingItem.clip.Length == 0)
            {
                Debug.LogWarning($"AudioClip '{name}' has no clips assigned in the music configuration.");
                return;
            }

            EnsureMusicSource();

            var rand = Random.Range(0, matchingItem.clip.Length);
            _musicSource.clip = matchingItem.clip[rand];
            _musicSource.volume = matchingItem.volume;
            _musicSource.loop = matchingItem.loop;
            _musicSource.Play();
        }

        private void EnsureMusicSource()
        {
            if (_musicObject != null) return;

            _musicObject = new GameObject("[AudioService] Music");
            Object.DontDestroyOnLoad(_musicObject);

            _musicSource = _musicObject.AddComponent<AudioSource>();
            _musicSource.spatialBlend = 0f;
            _musicSource.outputAudioMixerGroup = _musicConfiguration.MixerGroup;
        }

        public void Dispose()
        {
            if (_musicObject != null) Object.Destroy(_musicObject);
        }
    }
}
