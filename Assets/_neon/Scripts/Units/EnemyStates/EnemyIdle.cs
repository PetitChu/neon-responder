namespace BrainlessLabs.Neon {

    public class EnemyIdle : UnitState {

        private string animationName = "Idle";
        public override void Enter(){

            unit.StopMoving();
            unit.animator.Play(animationName);

            //find a player target
            if(!unit.target) unit.target = unit.findClosestPlayer();

            //if the target was spotted, turn towards the target
            if(unit.targetSpotted) unit.TurnToTarget();
        }
    }
}