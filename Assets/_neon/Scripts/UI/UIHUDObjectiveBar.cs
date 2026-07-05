using System;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BrainlessLabs.Neon {

    //Objective fill bar + a giant arrow to the node + the dawn/Signal bar (spec §5.5).
    //Pure consumer of ObjectiveProgress + SignalChanged.
    public class UIHUDObjectiveBar : MonoBehaviour {

        [Header("Objective")]
        [SerializeField] private GameObject objectiveRoot;
        [SerializeField] private Image objectiveFill;
        [SerializeField] private Text objectiveLabel;
        [SerializeField] private RectTransform nodeArrow; // points from screen center toward the node

        [Header("Signal (dawn)")]
        [SerializeField] private Image dawnFill;

        [Inject] private IGameplaySignals _signals;
        private IDisposable _objectiveSub;
        private IDisposable _signalSub;
        private bool objectiveActive;
        private Vector2 nodeWorldPos;

        void Start(){
            if(_signals == null) return; //scene without DI injection
            if(objectiveRoot != null) objectiveRoot.SetActive(false);
            if(dawnFill != null) dawnFill.fillAmount = 0f;
            _objectiveSub = _signals.On<ObjectiveProgress>().Subscribe(ApplyObjective);
            _signalSub = _signals.On<SignalChanged>().Subscribe(ApplySignal);
        }

        void OnDestroy(){
            _objectiveSub?.Dispose();
            _signalSub?.Dispose();
        }

        void LateUpdate(){
            if(!objectiveActive || nodeArrow == null) return;
            var cam = Camera.main;
            if(cam == null) return;
            Vector2 screenNode = cam.WorldToScreenPoint(nodeWorldPos);
            Vector2 center = new Vector2(Screen.width, Screen.height) * 0.5f;
            Vector2 dir = (screenNode - center);
            nodeArrow.gameObject.SetActive(dir.magnitude > 40f); //hide when basically on the node
            if(dir.sqrMagnitude > 0.001f) nodeArrow.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        }

        void ApplyObjective(ObjectiveProgress p){
            objectiveActive = p.Normalized < 1f;
            nodeWorldPos = p.Position;
            if(objectiveRoot != null) objectiveRoot.SetActive(objectiveActive);
            if(objectiveFill != null) objectiveFill.fillAmount = p.Normalized;
            if(objectiveLabel != null) objectiveLabel.text = p.PlayerInZone ? "REBOOTING…" : "REACH THE NODE";
        }

        void ApplySignal(SignalChanged s){
            if(dawnFill != null && s.Dawn > 0f) dawnFill.fillAmount = Mathf.Clamp01(s.Value / s.Dawn);
        }
    }
}
