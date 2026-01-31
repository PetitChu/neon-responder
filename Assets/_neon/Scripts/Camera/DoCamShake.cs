using UnityEngine;

namespace BrainlessLabs.Neon {

    //preform a camera shake on start
    public class DoCamShake : MonoBehaviour {

        void Start() {
            CameraShake cs = Camera.main.GetComponent<CameraShake>();
            if(cs != null) cs.ShowCamShake();
        }
    }
}