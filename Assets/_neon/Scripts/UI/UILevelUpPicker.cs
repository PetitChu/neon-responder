using System;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BrainlessLabs.Neon {

    //The 1-of-3 level-up draft (spec §5.3). Consumes LevelUpChoicesReady and
    //commands IProgressionSystem.Choose. Buttons work at slow-mo (UI ignores timeScale).
    public class UILevelUpPicker : MonoBehaviour {

        [SerializeField] private GameObject panel;
        [SerializeField] private Button[] choiceButtons = new Button[3];
        [SerializeField] private Text[] choiceTitles = new Text[3];
        [SerializeField] private Text[] choiceDescriptions = new Text[3];
        [Inject] private IGameplaySignals _signals;
        [Inject] private IProgressionSystem _progression;
        private IDisposable _subscription;

        void Start(){
            if(_signals == null || _progression == null) return; //scene without DI injection
            if(panel != null) panel.SetActive(false);
            for(int i = 0; i < choiceButtons.Length; i++){
                int index = i; //capture for the closure
                if(choiceButtons[i] != null) choiceButtons[i].onClick.AddListener(() => OnChoice(index));
            }
            _subscription = _signals.On<LevelUpChoicesReady>().Subscribe(Show);
        }

        void OnDestroy(){
            _subscription?.Dispose();
        }

        void Show(LevelUpChoicesReady levelUp){
            if(panel == null) return;
            panel.SetActive(true);
            for(int i = 0; i < choiceButtons.Length; i++){
                bool hasChoice = i < levelUp.Choices.Length && levelUp.Choices[i] != null;
                if(choiceButtons[i] != null) choiceButtons[i].gameObject.SetActive(hasChoice);
                if(!hasChoice) continue;

                var protocol = levelUp.Choices[i];
                if(choiceTitles[i] != null) choiceTitles[i].text = $"{protocol.DisplayName}\n<size=60%>{protocol.Family} · {protocol.Rarity}</size>";
                if(choiceDescriptions[i] != null) choiceDescriptions[i].text = protocol.Description;
            }
        }

        void OnChoice(int index){
            if(panel != null) panel.SetActive(false);
            _progression.Choose(index);
        }
    }
}
