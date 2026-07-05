using System;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BrainlessLabs.Neon {

    //The M3 shop beat (Heal + Continue). Consumes RunPhaseChanged, commands the
    //economy + run. Specials/ranks/reroll are added here in M4.
    public class UIShopScreen : MonoBehaviour {

        [SerializeField] private GameObject panel;
        [SerializeField] private Button healButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Text chargeLabel;
        [SerializeField] private Text healLabel;
        [Inject] private IGameplaySignals _signals;
        [Inject] private IEconomySystem _economy;
        [Inject] private IRunService _run;
        private RunSettings _runSettings;
        private IDisposable _phaseSub;
        private IDisposable _chargeSub;

        void Start(){
            if(_signals == null || _run == null || _economy == null) return; //scene without DI injection
            _runSettings = RunSettingsAsset.InstanceAsset.Settings;

            if(panel != null) panel.SetActive(false);
            if(healButton != null) healButton.onClick.AddListener(OnHeal);
            if(continueButton != null) continueButton.onClick.AddListener(OnContinue);
            if(healLabel != null) healLabel.text = $"HEAL (+{_runSettings.ShopHealAmount})  ⚡{_runSettings.ShopHealCost}";

            _phaseSub = _signals.On<RunPhaseChanged>().Subscribe(OnPhase);
            _chargeSub = _signals.On<NeonChargeChanged>().Subscribe(_ => RefreshCharge());
        }

        void OnDestroy(){
            _phaseSub?.Dispose();
            _chargeSub?.Dispose();
        }

        void OnPhase(RunPhaseChanged phase){
            bool inShop = phase.Current == RunPhase.Shop;
            if(panel != null) panel.SetActive(inShop);
            if(inShop) RefreshCharge();
        }

        void RefreshCharge(){
            if(chargeLabel != null) chargeLabel.text = $"NEON CHARGE: {_economy.NeonCharge}";
            if(healButton != null) healButton.interactable = _economy.NeonCharge >= _runSettings.ShopHealCost;
        }

        void OnHeal(){
            if(!_economy.TrySpend(_runSettings.ShopHealCost)) return;
            var player = GameObject.FindGameObjectWithTag("Player");
            player?.GetComponent<HealthSystem>()?.AddHealth(_runSettings.ShopHealAmount);
            RefreshCharge();
        }

        void OnContinue(){
            if(panel != null) panel.SetActive(false);
            _run.ContinueFromShop();
        }
    }
}
