using System;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BrainlessLabs.Neon {

    //XP progress within the current level + level label. Pure signals consumer.
    public class UIHUDXpBar : MonoBehaviour {

        [SerializeField] private Image fillImage;
        [SerializeField] private Text levelLabel;
        [Inject] private IGameplaySignals _signals;
        private IDisposable _subscription;

        void Start(){
            if(_signals == null) return; //scene without DI injection
            if(fillImage != null) fillImage.fillAmount = 0f;
            if(levelLabel != null) levelLabel.text = "LV 1";
            _subscription = _signals.On<XpProgressChanged>().Subscribe(Apply);
        }

        void OnDestroy(){
            _subscription?.Dispose();
        }

        void Apply(XpProgressChanged progress){
            if(fillImage != null && progress.XpForNextLevel > 0){
                fillImage.fillAmount = Mathf.Clamp01((float)progress.XpIntoLevel / progress.XpForNextLevel);
            }
            if(levelLabel != null) levelLabel.text = $"LV {progress.Level}";
        }
    }
}
