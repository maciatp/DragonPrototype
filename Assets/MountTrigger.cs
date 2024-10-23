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
            PlayerController playerController = other.transform.parent.GetComponent<PlayerController>();
            //if (dragonController.GetDragonState == DragonController.DragonStates.Free) //montar al dragon en el aire, de un salto
            //{
            //    dragonController.MountDragonFlying();
            //}
            if(dragonController.GetDragonState == DragonController.DragonStates.Landed && !playerController.IsPlayerGrounded)
            {
                dragonController.MountDragonOnLand();
            }
        }
       
    }
}
