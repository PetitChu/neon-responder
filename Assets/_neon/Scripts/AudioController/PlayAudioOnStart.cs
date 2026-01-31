using UnityEngine;

namespace BrainlessLabs.Neon {

    //class for playing a sfx on Start of a scene
    public class PlayAudioOnStart : MonoBehaviour {

        public string audioItemName = "";
        public Transform parentTransform; //optional

        void Start(){
            if(audioItemName.Length > 0) AudioController.PlaySFX(audioItemName, transform.position, parentTransform? parentTransform : null);
        }
    }
}