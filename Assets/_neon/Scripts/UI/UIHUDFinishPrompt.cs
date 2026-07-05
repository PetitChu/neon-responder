using System;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BrainlessLabs.Neon {

    //The single verb-glyph prompt (R7) + "+N ready" counter. Follows the prompted
    //target on screen (Screen Space - Overlay canvas). Pure signals consumer.
    public class UIHUDFinishPrompt : MonoBehaviour {

        [SerializeField] private RectTransform promptRoot;
        [SerializeField] private Text verbLabel;
        [SerializeField] private Text countLabel;
        [SerializeField] private Vector2 screenOffset = new(0f, 60f);
        [Inject] private IGameplaySignals _signals;
        private IDisposable _subscription;
        private IDisposable _challengeSubscription;
        private bool hasTarget;
        private bool challengeActive;
        private Vector2 targetPosition;

        void Start(){
            if(_signals == null) return; //scene without DI injection
            if(promptRoot != null) promptRoot.gameObject.SetActive(false);
            _subscription = _signals.On<FinishReadyPromptChanged>().Subscribe(Apply);
            _challengeSubscription = _signals.On<FinishChallengeChanged>().Subscribe(ApplyChallenge);
        }

        void OnDestroy(){
            _subscription?.Dispose();
            _challengeSubscription?.Dispose();
        }

        void LateUpdate(){
            if(!hasTarget || promptRoot == null) return;
            var cam = Camera.main;
            if(cam == null) return;
            promptRoot.position = (Vector2)cam.WorldToScreenPoint(targetPosition) + screenOffset;
        }

        void Apply(FinishReadyPromptChanged prompt){
            if(challengeActive) return; //an active hero challenge owns the prompt
            hasTarget = prompt.HasTarget;
            targetPosition = prompt.TargetPosition;
            if(promptRoot != null) promptRoot.gameObject.SetActive(prompt.HasTarget);
            if(verbLabel != null) verbLabel.text = prompt.SuggestedVerb.ToString(); //"PUNCH" glyph art comes with M4 polish
            if(countLabel != null) countLabel.text = prompt.ReadyCount > 1 ? $"+{prompt.ReadyCount - 1} ready" : "";
        }

        void ApplyChallenge(FinishChallengeChanged challenge){
            challengeActive = challenge.Active;
            if(!challenge.Active) return; //the next selector publish reasserts the normal prompt

            hasTarget = true;
            targetPosition = challenge.TargetPosition;
            if(promptRoot != null) promptRoot.gameObject.SetActive(true);
            if(verbLabel != null) verbLabel.text = $"{challenge.ExpectedVerb} {challenge.Progress}/{challenge.Total}";
            if(countLabel != null) countLabel.text = "FINISH!";
        }
    }
}
