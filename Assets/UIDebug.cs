using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIDebug : MonoBehaviour
{

   [SerializeField] TMPro.TextMeshProUGUI playerState;
   [SerializeField] TMPro.TextMeshProUGUI playerGrounded;
   [SerializeField] TMPro.TextMeshProUGUI playerVelocity;
   [SerializeField] TMPro.TextMeshProUGUI playerKinematic;
   [SerializeField] TMPro.TextMeshProUGUI playerGravity;
   [SerializeField] TMPro.TextMeshProUGUI dragonState;
   [SerializeField] TMPro.TextMeshProUGUI dragonGrounded;
   [SerializeField] TMPro.TextMeshProUGUI dragonVelocity;
   [SerializeField] TMPro.TextMeshProUGUI dragonAction;
    [SerializeField] TMPro.TextMeshProUGUI dragonKinematic;
    [SerializeField] TMPro.TextMeshProUGUI dragonGravity;

    PlayerController playerController;
    Rigidbody playerRb;
    DragonController dragonController;
    Rigidbody dragonRb;

    // Start is called before the first frame update
    void Start()
    {
        playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        playerRb = playerController.GetPlayerRB;
        dragonController = GameObject.FindGameObjectWithTag("Dragon").GetComponent<DragonController>();
        dragonRb = dragonController.GetDragonRB;
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

        //PLAYER RIGIDBODY
        if(playerRb.isKinematic)
        {
            playerKinematic.text = "Kinematic";
            playerKinematic.color = Color.white;
        }
        else
        {
            playerKinematic.text = "NOT Kinematic";
            playerKinematic.color = Color.red;
        }

        if(playerRb.useGravity)
        {
            playerGravity.text = "Gravity";
            playerGravity.color = Color.white;
        }
        else
        {
            playerGravity.text = "NOT Gravity";
            playerGravity.color = Color.red;
        }

        //DRAGON
        dragonState.text = dragonController.GetDragonState.ToString();
        if(dragonController.GetDragonState == DragonController.DragonStates.Landed || dragonController.GetDragonState == DragonController.DragonStates.MountedLanded)
        {
            if(dragonController.IsDragonGrounded)
            {
                dragonGrounded.text = "Grounded";
                dragonGrounded.color = Color.white;
            }
            else
            {
                dragonGrounded.text = "Not Grounded";
                dragonGrounded.color = Color.red;
                
            }
                dragonVelocity.text = dragonController.GetCurrentDragonVelocity.magnitude.ToString();
                dragonAction.text = "";
        }
        else if(dragonController.GetDragonState != DragonController.DragonStates.Landed || dragonController.GetDragonState != DragonController.DragonStates.MountedLanded)
        {
            
            dragonGrounded.text = "Air";
            dragonVelocity.text = dragonController.GetCurrentFlyingSpeed.ToString("F0");
            dragonAction.text = dragonController.GetDragonAction();
        }

        //DRAGON RIGIDBODY
        if (dragonRb.isKinematic)
        {
            dragonKinematic.text = "Kinematic";
            dragonKinematic.color = Color.white;
        }
        else
        {
            dragonKinematic.text = "NOT Kinematic";
            dragonKinematic.color = Color.red;
        }

        if (dragonRb.useGravity)
        {
            dragonGravity.text = "Gravity";
            dragonGravity.color = Color.white;
        }
        else
        {
            dragonGravity.text = "NOT Gravity";
            dragonGravity.color = Color.red;
        }

    }
}
