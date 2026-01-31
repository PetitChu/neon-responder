using UnityEngine;

namespace BrainlessLabs.Neon {

    //state for player jump
    public class PlayerJump : UnitState {

        private string animationName = "Jump";
        private string sfxName = "JumpUp";
        private bool hasLanded;
        private int playerId => unit.settings.playerId;
        
        public override void Enter(){
            unit.StopMoving(true);
            unit.animator.Play(animationName);
            unit.groundPos = unit.transform.position.y;
            unit.isGrounded = false;
            unit.yForce = unit.settings.jumpHeight; //the upwards force of the jump
            AudioService.PlaySFX(sfxName, unit.transform.position);
         }

        public override void Update(){

            //perform jump punch attack
            if(InputService.PunchKeyDown(playerId)){ unit.UnitStateMachine.SetState(new PlayerJumpAttack(unit.settings.jumpPunch)); return; }

            //perform jump kick attack
            if(InputService.KickKeyDown(playerId)){ unit.UnitStateMachine.SetState(new PlayerJumpAttack(unit.settings.jumpKick)); return; }

            //go to landed state
            if(hasLanded) unit.UnitStateMachine.SetState(new PlayerLand());
        }

        public override void FixedUpdate(){

            //preform jump
            unit.JumpSequence();

            //check is the jump is finished when the unit has reached the ground position
            bool JumpFinished = (unit.transform.position.y < unit.groundPos);

            //end of jump
            if(JumpFinished){
                unit.GetComponent<Collider2D>().offset = Vector2.zero;
                unit.transform.position = new Vector3(unit.transform.position.x, unit.groundPos, 0);
                unit.isGrounded = true;
                hasLanded = true;
            }
        }
    }
}