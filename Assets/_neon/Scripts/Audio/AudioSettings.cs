using System;

namespace BrainlessLabs.Neon
{
    [Serializable]
    public class AudioSettings : IAudioSettings
    {

#if UNITY_EDITOR
        public void Editor_OnGUI(UnityEngine.Object target)
        {
        }
    }
#endif
}
