using UnityEngine;
using VContainer;

namespace BrainlessLabs.Neon {

    //class for playing a sfx on Start of a scene
    public class PlayAudioOnStart : MonoBehaviour {

        public string audioItemName = "";
        public Transform parentTransform; //optional
        [Inject] private IAudioService _audioService;

        void Start(){
            if(audioItemName.Length > 0) _audioService.PlaySFX(audioItemName, transform.position, parentTransform? parentTransform : null);
        }
    }
}