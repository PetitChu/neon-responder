using UnityEngine;

namespace BrainlessLabs.Neon {

    //state Machine class
    public class UnitStateMachine : UnitActions {
    
        [SerializeField] private bool showStateInGame; //shows the current state in a textfield below this unit
        [ReadOnlyProperty] public string currentState; //used for displaying the current state in the unity inspector
        private TextMesh stateText; //textfield for showing state in game for debugging
        private UnitState _unitState; //the current state

        void Start(){

            //set to starting state
            if(isPlayer) SetState(new PlayerIdle()); //if unit if a player, go to state PlayerIdle
            else if(isEnemy) SetState(new EnemyIdle()); //if unit if a enemy, go to state EnemyIdle
        }

        public void SetState(UnitState unitState){
        
            //exit current state
            if (this._unitState != null) _unitState.Exit();
       
            //set new state
            _unitState = unitState;
            _unitState.unit = this;

            //set data
            currentState = GetCurrentStateShortName(); //debug info
            _unitState.stateStartTime = Time.time;

            //enter the state
            _unitState.Enter();
        }

        public UnitState GetCurrentState(){
            return _unitState;;
        }

        void Update(){
            _unitState?.Update();
            UpdateStateText();
        }

        void LateUpdate(){
            _unitState?.LateUpdate();
        }

        void FixedUpdate(){
            _unitState?.FixedUpdate();
        }

        void UpdateStateText(){

            //if stateText should not be shown or is not initialized, do nothing
            if(!showStateInGame){
                if (stateText != null) {
                    Destroy(stateText.gameObject);
                    stateText = null;
                }
                return;
            }

            //create stateText if it does not exist
            if(stateText == null){
                GameObject stateTxtGo = Instantiate(Resources.Load("StateText"), transform) as GameObject;
                if (stateTxtGo != null) {
                    stateTxtGo.name = "StateText";
                    stateTxtGo.transform.localPosition = new Vector2(0, -0.2f);
                    stateText = stateTxtGo.GetComponent<TextMesh>();
                }
            }

            //update the state text if it's initialized
            if(stateText != null){
                stateText.text = GetCurrentStateShortName();
                stateText.transform.localRotation = Quaternion.Euler(0, dir == DIRECTION.LEFT ? 180 : 0, 0);
            }
        }

        //returns the name of the current state without the namespace
        string GetCurrentStateShortName(){
            string currentState = UnitStateMachine?.GetCurrentState().GetType().ToString();
            string[] splitStrings = currentState.Split('.');                  
            if(splitStrings.Length >= 2) return splitStrings[1];
            return "";
        }
    }
}