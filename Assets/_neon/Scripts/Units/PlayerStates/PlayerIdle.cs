namespace BrainlessLabs.Neon {

    public class PlayerIdle : UnitState {

        private string animationName = "Idle";
        private int playerId => unit.settings.playerId;
    
        public override void Enter(){
            unit.animator.Play(animationName);
        }

        public override void Update(){
        
            //stop moving
            unit.StopMoving(false);

            //defend
            if(unit.InputService.DefendKeyDown(playerId)){ unit.UnitStateMachine.SetState(new UnitDefend()); return; }

            //jump
            if(unit.InputService.JumpKeyDown(playerId)){ unit.UnitStateMachine.SetState(new PlayerJump()); return; }

            //use weapon
            if(unit.weapon && unit.InputService.PunchKeyDown(playerId)){ unit.UnitStateMachine.SetState(new PlayerWeaponAttack()); return; }

            //check for nearby enemy to ground Punch
            if(unit.InputService.PunchKeyDown(playerId) && unit.NearbyEnemyDown()){ unit.UnitStateMachine.SetState(new PlayerGroundPunch()); return; }

            //check for nearby enemy to ground kick
            if(unit.InputService.KickKeyDown(playerId) && unit.NearbyEnemyDown()){ unit.UnitStateMachine.SetState(new PlayerGroundKick()); return; }

            //punch Key pressed
            if(unit.InputService.PunchKeyDown(playerId)){ unit.UnitStateMachine.SetState(new PlayerAttack(ATTACKTYPE.PUNCH)); return; }

            //kick Key pressed
            if(unit.InputService.KickKeyDown(playerId)){ unit.UnitStateMachine.SetState(new PlayerAttack(ATTACKTYPE.KICK)); return; }

            //grab something (enemy or item)
            if(unit.InputService.GrabKeyDown(playerId) && !unit.weapon){ unit.UnitStateMachine.SetState(new PlayerTryGrab()); return; }

            //drop current weapon
            if(unit.InputService.GrabKeyDown(playerId) && unit.weapon){ unit.UnitStateMachine.SetState(new UnitDropWeapon()); return; }
                
            //move
            if(unit.InputService.GetInputVector(playerId).magnitude > 0) unit.UnitStateMachine.SetState(new PlayerMove());
        }
    }
}