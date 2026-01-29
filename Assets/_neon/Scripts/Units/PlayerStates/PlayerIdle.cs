namespace BeatEmUpTemplate2D {

    public class PlayerIdle : State {

        private string animationName = "Idle";
        private int playerId => unit.settings.playerId;
    
        public override void Enter(){
            unit.animator.Play(animationName);
        }

        public override void Update(){
        
            //stop moving
            unit.StopMoving(false);

            //defend
            if(InputManager.DefendKeyDown(playerId)){ unit.stateMachine.SetState(new UnitDefend()); return; }

            //jump
            if(InputManager.JumpKeyDown(playerId)){ unit.stateMachine.SetState(new PlayerJump()); return; }

            //use weapon
            if(unit.weapon && InputManager.PunchKeyDown(playerId)){ unit.stateMachine.SetState(new PlayerWeaponAttack()); return; }

            //check for nearby enemy to ground Punch
            if(InputManager.PunchKeyDown(playerId) && unit.NearbyEnemyDown()){ unit.stateMachine.SetState(new PlayerGroundPunch()); return; }

            //check for nearby enemy to ground kick
            if(InputManager.KickKeyDown(playerId) && unit.NearbyEnemyDown()){ unit.stateMachine.SetState(new PlayerGroundKick()); return; }

            //punch Key pressed
            if(InputManager.PunchKeyDown(playerId)){ unit.stateMachine.SetState(new PlayerAttack(ATTACKTYPE.PUNCH)); return; }

            //kick Key pressed
            if(InputManager.KickKeyDown(playerId)){ unit.stateMachine.SetState(new PlayerAttack(ATTACKTYPE.KICK)); return; }

            //grab something (enemy or item)
            if(InputManager.GrabKeyDown(playerId) && !unit.weapon){ unit.stateMachine.SetState(new PlayerTryGrab()); return; }

            //drop current weapon
            if(InputManager.GrabKeyDown(playerId) && unit.weapon){ unit.stateMachine.SetState(new UnitDropWeapon()); return; }
                
            //move
            if(InputManager.GetInputVector(playerId).magnitude > 0) unit.stateMachine.SetState(new PlayerMove());
        }
    }
}