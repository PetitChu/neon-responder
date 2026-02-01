using UnityEngine;

namespace BrainlessLabs.Neon {

    public class PlayerMove : UnitState {

        private string animationName = "Run";
        private int playerId => unit.settings.playerId;

        public override void Update(){

            //defend
            if(Services.Input.DefendKeyDown(playerId)){ unit.UnitStateMachine.SetState(new UnitDefend()); return; }

            //jump
            if(Services.Input.JumpKeyDown(playerId)){ unit.UnitStateMachine.SetState(new PlayerJump()); return; }

            //use weapon
            if(unit.weapon && Services.Input.PunchKeyDown(playerId)){ unit.UnitStateMachine.SetState(new PlayerWeaponAttack()); return; }

            //check for nearby enemy to ground pound
            if(Services.Input.PunchKeyDown(playerId) && unit.NearbyEnemyDown()){ unit.UnitStateMachine.SetState(new PlayerGroundPunch()); return; }

            //check for nearby enemy to ground kick
            if(Services.Input.KickKeyDown(playerId) && unit.NearbyEnemyDown()){ unit.UnitStateMachine.SetState(new PlayerGroundKick()); return; }

            //punch Key pressed
            if(Services.Input.PunchKeyDown(playerId)){ unit.UnitStateMachine.SetState(new PlayerAttack(ATTACKTYPE.PUNCH)); return; }

            //kick Key pressed
            if(Services.Input.KickKeyDown(playerId)){ unit.UnitStateMachine.SetState(new PlayerAttack(ATTACKTYPE.KICK)); return; }

            //grab something (enemy or item)
            if(Services.Input.GrabKeyDown(playerId) && !unit.weapon){ unit.UnitStateMachine.SetState(new PlayerTryGrab()); return; }

            //drop current weapon
            if(Services.Input.GrabKeyDown(playerId) && unit.weapon){ unit.UnitStateMachine.SetState(new UnitDropWeapon()); return; }
        }

        public override void FixedUpdate(){

            //get input
            Vector2 inputVector = Services.Input.GetInputVector(playerId).normalized;

            //go to idle, if there is no input
            if(inputVector.magnitude == 0) {
                unit.UnitStateMachine.SetState(new PlayerIdle()); 
                return;
            }

            //go to idle if there is a wall in front of us
            Vector2 wallDistanceCheck = unit.col2D? (unit.col2D.size/1.6f) * 1.1f : Vector2.one * .3f; //dividing by 1.6f because the distance check needs to be a bit larger than the collider (otherwise we never encounter a wall)
            if(unit.WallDetected(inputVector * wallDistanceCheck)){
                unit.UnitStateMachine.SetState(new PlayerIdle()); //go to idle
                return;
            }

            //adjust input to move slower in the y position to create a sense of depth
            inputVector.y *= .8f; 
                
            //move
            unit.MoveToVector(inputVector, unit.settings.moveSpeed);

            //play run anim
            unit.animator.Play(animationName);
        }
    }
}
