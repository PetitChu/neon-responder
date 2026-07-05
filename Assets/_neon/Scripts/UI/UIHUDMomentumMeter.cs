using System;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BrainlessLabs.Neon {

    //Momentum tier meter (M1-minimal): fill + color per tier. Pure signals consumer.
    public class UIHUDMomentumMeter : MonoBehaviour {

        [SerializeField] private Image fillImage;
        [SerializeField] private Text tierLabel;
        [SerializeField] private Color[] tierColors = {
            new(0.5f, 0.5f, 0.5f, 1f),   //Cool
            new(1f, 0.8f, 0.3f, 1f),     //Warm
            new(1f, 0.5f, 0.15f, 1f),    //Hot
            new(1f, 0.15f, 0.3f, 1f)     //Overdrive
        };
        [Inject] private IGameplaySignals _signals;
        private IDisposable _subscription;

        void Start(){
            if(_signals == null) return; //scene without DI injection
            Apply(MomentumTier.Cool);
            _subscription = _signals.On<MomentumTierChanged>().Subscribe(e => Apply(e.Current));
        }

        void OnDestroy(){
            _subscription?.Dispose();
        }

        void Apply(MomentumTier tier){
            int index = Mathf.Clamp((int)tier, 0, tierColors.Length - 1);
            if(fillImage != null){
                fillImage.fillAmount = ((int)tier + 1) / 4f;
                fillImage.color = tierColors[index];
            }
            if(tierLabel != null) tierLabel.text = tier.ToString().ToUpper();
        }
    }
}
