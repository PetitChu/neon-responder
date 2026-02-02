using UnityEngine;
using VContainer;

namespace BrainlessLabs.Neon {

    //class for Health Pickup Items
    public class HealthPickup : Item {

        [Header("Health Setting")]
        public int healthRecover = 1;
        public GameObject showEffect; //effect to show when picked up
        [Inject] private IAudioService _audioService;

        //item has been picked up
        public override void OnPickUpItem(GameObject target){

            //heal target if this is a health pickup
            target?.GetComponent<HealthSystem>()?.AddHealth(healthRecover);

            //show effect
            if(showEffect) GameObject.Instantiate(showEffect, transform.position, Quaternion.identity);

            //play sfx
            _audioService.PlaySFX(pickupSFX, transform.position);
            Destroy(gameObject);
        }
    }
}
