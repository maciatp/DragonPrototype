using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIDebug : MonoBehaviour
{

   [SerializeField] TMPro.TextMeshProUGUI playerState;
   [SerializeField] TMPro.TextMeshProUGUI playerGrounded;
   [SerializeField] TMPro.TextMeshProUGUI playerVelocity;
   [SerializeField] TMPro.TextMeshProUGUI dragonState;
   [SerializeField] TMPro.TextMeshProUGUI dragonGrounded;
   [SerializeField] TMPro.TextMeshProUGUI dragonVelocity;
   [SerializeField] TMPro.TextMeshProUGUI dragonAction;

    PlayerController playerController;
    DragonController dragonController;

    // Start is called before the first frame update
    void Start()
    {
        playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        dragonController = GameObject.FindGameObjectWithTag("Dragon").GetComponent<DragonController>();
    }

    // Update is called once per frame
    void Update()
    {
        //PLAYER
        playerState.text = playerController.GetPlayerState.ToString();
        if(playerController.GetPlayerState == PlayerController.PlayerStates.Normal)
        {
            if(playerController.IsPlayerGrounded)
            {
                playerGrounded.text = "Grounded";
                playerGrounded.color = Color.white;

            }
            else
            {
                playerGrounded.text = "Not Grounded";
                playerGrounded.color = Color.red; 
            }
        }
        playerVelocity.text = playerController.GetPlayerVelocity.ToString();

        //DRAGON
        dragonState.text = dragonController.GetDragonState.ToString();
        if(dragonController.GetDragonState == DragonController.DragonStates.Landed || dragonController.GetDragonState == DragonController.DragonStates.MountedLanded)
        {
            if(dragonController.GetDragonGrounded)
            {
                dragonGrounded.text = "Grounded";
                dragonGrounded.color = Color.white;
            }
            else
            {
                dragonGrounded.text = "Not Grounded";
                dragonGrounded.color = Color.red;
                
            }
                dragonVelocity.text = dragonController.GetCurrentDragonVelocity.ToString();
                dragonAction.text = "";
        }
        else if(dragonController.GetDragonState != DragonController.DragonStates.Landed || dragonController.GetDragonState != DragonController.DragonStates.MountedLanded)
        {
            
            dragonGrounded.text = "Air";
            dragonVelocity.text = dragonController.GetCurrentFlyingSpeed.ToString();
            dragonAction.text = dragonController.GetDragonAction();
        }
        

    }
}
