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
        private bool hasTarget;
        private Vector2 targetPosition;

        void Start(){
            if(_signals == null) return; //scene without DI injection
            if(promptRoot != null) promptRoot.gameObject.SetActive(false);
            _subscription = _signals.On<FinishReadyPromptChanged>().Subscribe(Apply);
        }

        void OnDestroy(){
            _subscription?.Dispose();
        }

        void LateUpdate(){
            if(!hasTarget || promptRoot == null) return;
            var cam = Camera.main;
            if(cam == null) return;
            promptRoot.position = (Vector2)cam.WorldToScreenPoint(targetPosition) + screenOffset;
        }

        void Apply(FinishReadyPromptChanged prompt){
            hasTarget = prompt.HasTarget;
            targetPosition = prompt.TargetPosition;
            if(promptRoot != null) promptRoot.gameObject.SetActive(prompt.HasTarget);
            if(verbLabel != null) verbLabel.text = prompt.SuggestedVerb.ToString(); //"PUNCH" glyph art comes with M4 polish
            if(countLabel != null) countLabel.text = prompt.ReadyCount > 1 ? $"+{prompt.ReadyCount - 1} ready" : "";
        }
    }
}
