using System;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BrainlessLabs.Neon {

    //Functional readouts for the two M4 actives: Siren Pulse cooldown fill + a
    //"FINISHER READY" flag when Overcharge is full. Pure signals consumer; cosmetic
    //polish (icons/glyphs/animation) is the Feel & Level pass.
    public class UIHUDSpecialMeter : MonoBehaviour {

        [SerializeField] private Image specialCooldownFill; // 0 (cooling) → 1 (ready)
        [SerializeField] private Text specialReadyLabel;    // "SIREN READY"/""
        [SerializeField] private GameObject finisherReadyRoot; // shown only when Overcharge full

        [Inject] private IGameplaySignals _signals;
        private IDisposable _specialSub;
        private IDisposable _finisherSub;

        void Start(){
            if(_signals == null) return; //scene without DI injection
            if(specialCooldownFill != null) specialCooldownFill.fillAmount = 1f;
            if(specialReadyLabel != null) specialReadyLabel.text = "SIREN READY";
            if(finisherReadyRoot != null) finisherReadyRoot.SetActive(false);

            _specialSub = _signals.On<SpecialStateChanged>().Subscribe(ApplySpecial);
            _finisherSub = _signals.On<OverchargeReadyChanged>().Subscribe(ApplyFinisher);
        }

        void OnDestroy(){
            _specialSub?.Dispose();
            _finisherSub?.Dispose();
        }

        void ApplySpecial(SpecialStateChanged s){
            if(specialCooldownFill != null) specialCooldownFill.fillAmount = s.CooldownNormalized;
            if(specialReadyLabel != null) specialReadyLabel.text = s.Ready ? "SIREN READY" : "";
        }

        void ApplyFinisher(OverchargeReadyChanged f){
            if(finisherReadyRoot != null) finisherReadyRoot.SetActive(f.Ready);
        }
    }
}
