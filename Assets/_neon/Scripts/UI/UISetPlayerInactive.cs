using System.Collections;
using UnityEngine;

namespace BrainlessLabs.Neon {

    public class UISetPlayerInactive : MonoBehaviour {

        public float startDelay = 3f;

         void OnEnable() {
                StartCoroutine(SetPlayerInactive(startDelay));
            }

        //Set all player(s) to Inactive state
        IEnumerator SetPlayerInactive(float delay){
            yield return new WaitForSeconds(startDelay);

            foreach(UnitStateMachine unitStateMachine in GameObject.FindObjectsOfType<UnitStateMachine>()){
                if(unitStateMachine.settings.unitType == UNITTYPE.PLAYER){
                    unitStateMachine.SetState(new PlayerInActive());
                }
            }
        }
    }
}