using UnityEngine;

namespace BrainlessLabs.Neon
{
    public class AudioService : IAudioService, System.IDisposable
    {
        public static AudioService Instance { get; private set; }

        private readonly AudioConfigurationAsset _sfxConfiguration;
        private readonly AudioConfigurationAsset _musicConfiguration;

        private GameObject _musicObject;
        private AudioSource _musicSource;

        public AudioService()
        {
            var settings = AudioSettingsAsset.InstanceAsset.Settings;
            _sfxConfiguration = settings.SfxConfiguration;
            _musicConfiguration = settings.MusicConfiguration;
            Instance = this;
        }

        #region Static API (backward compatibility)

        public static void PlaySFX(string name = "", Vector3? pos = null, Transform parent = null)
        {
            Instance?.PlaySFXInternal(name, pos, parent);
        }

        public static float GetSFXDuration(string name)
        {
            if (Instance == null) return 0;
            return Instance.GetSFXDurationInternal(name);
        }

        public static void PlayMusic(string name)
        {
            Instance?.PlayMusicInternal(name);
        }

        #endregion

        #region IAudioService

        void IAudioService.PlaySFX(string name, Vector3? pos, Transform parent)
        {
            PlaySFXInternal(name, pos, parent);
        }

        float IAudioService.GetSFXDuration(string name)
        {
            return GetSFXDurationInternal(name);
        }

        void IAudioService.PlayMusic(string name)
        {
            PlayMusicInternal(name);
        }

        #endregion

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
            var found = false;

            foreach (var audioItem in audioItems)
            {
                if (audioItem.name != name) continue;

                if (audioItem.clip.Length == 0)
                {
                    Debug.LogWarning($"AudioClip '{name}' has no clips assigned in the SFX configuration.");
                    return;
                }

                if (Time.time - audioItem.lastTimePlayed < audioItem.minTimeBetweenCall) return;
                audioItem.lastTimePlayed = Time.time;

                var audioObj = new GameObject($"AudioSFX_{name}");
                audioObj.transform.parent = Mathf.Abs(audioItem.range) <= Mathf.Epsilon ? cameraTransform : finalParent;
                audioObj.transform.position = finalPos;

                var source = audioObj.AddComponent<AudioSource>();
                var rand = Random.Range(0, audioItem.clip.Length);
                source.clip = audioItem.clip[rand];
                source.spatialBlend = 1.0f;
                source.pitch = 1f + Random.Range(-audioItem.randomPitch, audioItem.randomPitch);
                source.volume = audioItem.volume + Random.Range(-audioItem.randomVolume, audioItem.randomVolume);
                source.outputAudioMixerGroup = _sfxConfiguration.MixerGroup;
                source.rolloffMode = AudioRolloffMode.Custom;
                source.loop = audioItem.loop;

                if (audioItem.range > 0) source.maxDistance = audioItem.range;
                if (audioItem.range > 3) source.minDistance = source.maxDistance - 3;

                source.Play();

                if (!audioItem.loop && source.clip != null)
                    Object.Destroy(audioObj, source.clip.length + Time.deltaTime);

                found = true;
            }

            if (!found) Debug.LogWarning($"No audio item found with name: {name}");
        }

        private float GetSFXDurationInternal(string name)
        {
            if (_sfxConfiguration == null) return 0;

            foreach (var audioItem in _sfxConfiguration.AudioItems)
            {
                if (audioItem.name != name) continue;

                if (audioItem.clip == null || audioItem.clip.Length == 0)
                {
                    Debug.LogWarning($"AudioClip '{name}' has no clips assigned in the sfx configuration.");
                    return 0;
                }

                float maxDuration = 0f;
                for (int i = 0; i < audioItem.clip.Length; i++)
                {
                    var clip = audioItem.clip[i];
                    if (clip == null) continue;
                    if (clip.length > maxDuration)
                    {
                        maxDuration = clip.length;
                    }
                }

                return maxDuration;
            }

            Debug.LogWarning($"No audio item found with name: {name}");
            return 0;
        }

        private void PlayMusicInternal(string name)
        {
            if (string.IsNullOrEmpty(name) || _musicConfiguration == null) return;

            foreach (var audioItem in _musicConfiguration.AudioItems)
            {
                if (audioItem.name != name) continue;

                if (audioItem.clip.Length == 0)
                {
                    Debug.LogWarning($"AudioClip '{name}' has no clips assigned in the music configuration.");
                    return;
                }

                EnsureMusicSource();

                var rand = Random.Range(0, audioItem.clip.Length);
                _musicSource.clip = audioItem.clip[rand];
                _musicSource.volume = audioItem.volume;
                _musicSource.loop = audioItem.loop;
                _musicSource.Play();
                return;
            }

            Debug.LogWarning($"No music item found with name: {name}");
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
            if (Instance == this) Instance = null;
        }
    }
}
