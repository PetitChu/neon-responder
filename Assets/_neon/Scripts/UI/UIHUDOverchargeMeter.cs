using System;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BrainlessLabs.Neon {

    //Overcharge fill (spending arrives with the M4 finisher). Pure signals consumer.
    public class UIHUDOverchargeMeter : MonoBehaviour {

        [SerializeField] private Image fillImage;
        [Inject] private IGameplaySignals _signals;
        private IDisposable _subscription;

        void Start(){
            if(_signals == null) return;
            if(fillImage != null) fillImage.fillAmount = 0f;
            _subscription = _signals.On<OverchargeChanged>().Subscribe(Apply);
        }

        void OnDestroy(){
            _subscription?.Dispose();
        }

        void Apply(OverchargeChanged overcharge){
            if(fillImage != null && overcharge.Cap > 0){
                fillImage.fillAmount = Mathf.Clamp01((float)overcharge.Value / overcharge.Cap);
            }
        }
    }
}
