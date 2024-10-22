using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MountTrigger : MonoBehaviour
{
    DragonController dragonController;

    private void Start()
    {
        dragonController = GetComponentInParent<DragonController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.parent.tag == "Player")
        {            
            if (dragonController.GetDragonState == DragonController.DragonStates.Free)
            {
                dragonController.MountDragonFlying();
            }
            else if(dragonController.GetDragonState == DragonController.DragonStates.Landed)
            {
                dragonController.MountDragonOnLand();
            }
        }
       
    }
}
