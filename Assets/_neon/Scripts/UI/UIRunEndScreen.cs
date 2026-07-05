using System;
using R3;
using UnityEngine;
using VContainer;

namespace BrainlessLabs.Neon {

    //Shows the win (dawn) menu on RunEnded(true). Loss presentation stays on Level's
    //existing OnPlayerDeath→GameOverMenu (documented split), so this only acts on the win.
    public class UIRunEndScreen : MonoBehaviour {

        [SerializeField] private UIManager uiManager;
        [SerializeField] private string dawnMenuName = "RunWon";
        [Inject] private IGameplaySignals _signals;
        private IDisposable _subscription;

        void Start(){
            if(_signals == null) return; //scene without DI injection
            if(uiManager == null) uiManager = FindObjectOfType<UIManager>();
            _subscription = _signals.On<RunEnded>().Subscribe(OnRunEnded);
        }

        void OnDestroy() => _subscription?.Dispose();

        void OnRunEnded(RunEnded ended){
            if(!ended.Won) return; //loss is presented by Level.OnPlayerDeath (GameOverMenu)
            if(uiManager != null) uiManager.ShowMenu(dawnMenuName);
            else Debug.Log("[Run] Dawn reached — RunWon (no UIManager menu wired).");
        }
    }
}
