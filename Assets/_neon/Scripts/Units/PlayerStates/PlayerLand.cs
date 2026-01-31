using UnityEngine;

namespace BrainlessLabs.Neon {

    //state for landing after a jump
    public class PlayerLand : UnitState {

        private string animationName = "Land";
        private float animDuration => unit.GetAnimDuration(animationName);
    
        public override void Enter(){
            unit.animator.Play(animationName);
            unit.StopMoving();
            unit.Footstep(); //footstep sfx
        }

        public override void Update(){
            if(Time.time - stateStartTime > animDuration)  unit.UnitStateMachine.SetState(new PlayerIdle()); //go to idle state
        }
    }
}
