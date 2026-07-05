using UnityEngine;

namespace BrainlessLabs.Neon {

    //brief self-stagger after a whiffed verb (spec §5.1 whiff cost: Momentum reset
    //+ 0.5s vulnerability window). Entered by FinishResolver, not by player input.
    public class PlayerWhiffStagger : UnitState {

        private readonly float duration;

        public PlayerWhiffStagger(float duration){
            this.duration = duration;
        }

        public override void Enter(){
            unit.StopMoving();
            unit.animator.Play("Hit", 0, 0); //reuse the hit-reaction anim as the stagger tell
        }

        public override void Update(){
            if(Time.time - stateStartTime > duration) unit.UnitStateMachine.SetState(new PlayerIdle());
        }
    }
}
