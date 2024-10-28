using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//HOLA XISCO NO ME PEGUES ES UN PROTOTIPO VALE?
public class GeneralButtonUI : MonoBehaviour
{
    [SerializeField] PlayerController playerController;
    [SerializeField] DragonController dragonController;

    [SerializeField] TMPro.TextMeshProUGUI northButtonText;
    
    [SerializeField] TMPro.TextMeshProUGUI eastButtonText;
    [SerializeField] TMPro.TextMeshProUGUI eastHoldText;
    [SerializeField] TMPro.TextMeshProUGUI southButtonText;
    
    [SerializeField] TMPro.TextMeshProUGUI westButtonText;

    [SerializeField] TMPro.TextMeshProUGUI rightTriggerText;
    [SerializeField] TMPro.TextMeshProUGUI leftTriggerText;
    [SerializeField] TMPro.TextMeshProUGUI leftTopButtonText;
    [SerializeField] TMPro.TextMeshProUGUI rightTopButtonText;

    Color availableColor;
    Color unavailableColor = Color.gray;
    // Start is called before the first frame update
    void Start()
    {
        availableColor = northButtonText.color;
        northButtonText.text = " ";
        eastButtonText.text = " ";
        eastHoldText.text = " ";
        southButtonText.text = " ";
        westButtonText.text = "  ";
        rightTriggerText.text = "  ";
        leftTriggerText.text = "  ";
        rightTopButtonText.text = " ";
        leftTopButtonText.text = " ";
    }

    // Update is called once per frame
    void Update()
    {
        switch (playerController.GetPlayerState)
        {
            case PlayerController.PlayerStates.Normal:
                rightTopButtonText.text = "Look Dragon";
                leftTopButtonText.text = " ";
                if (playerController.IsPlayerGrounded)
                {
                    northButtonText.text = "Jump";
                    rightTriggerText.text = "Run";
                    rightTriggerText.color = availableColor;
                }
                else
                {
                    northButtonText.text = "Parasail";
                    rightTriggerText.color = unavailableColor;
                }

                leftTriggerText.color = unavailableColor;
                leftTriggerText.text = " ";
                westButtonText.text = "Pet";
                westButtonText.color = unavailableColor;

                eastButtonText.text = "Mount";
                if(dragonController.IsMountable)
                {
                    eastButtonText.color = availableColor;
                }
                else
                {
                    eastButtonText.color = unavailableColor;
                }
                southButtonText.text = " ";
                eastHoldText.color = availableColor;
                switch (dragonController.GetDragonState)
                {
                    case DragonController.DragonStates.Free:
                        eastHoldText.text = "Land (Hold)";
                        break;
                   
                 
                    case DragonController.DragonStates.Landed:

                        if(dragonController.IsMountable)
                        {   
                            eastHoldText.text = " ";
                        }
                        else
                        {                            
                            eastHoldText.text = "Take-Off (Hold)";
                        }

                        if (dragonController.IsPetable && dragonController.IsMountable)
                        {
                            westButtonText.color = availableColor;
                        }
                        else
                        {
                            westButtonText.color = unavailableColor;
                        }

                        break;
                    case DragonController.DragonStates.MountedLanded:
                        eastButtonText.text = " ";
                        break;
                    default:
                        break;
                }


                break;
            case PlayerController.PlayerStates.BigFall:
                northButtonText.text = "Parasail";
                eastButtonText.text = "Call Dragon";
                eastButtonText.color = availableColor;
                eastHoldText.text = " ";
                eastHoldText.color = unavailableColor;
                rightTriggerText.text = "Dive";
                rightTriggerText.color = availableColor;
                southButtonText.text = " ";
                leftTriggerText.text = " ";
                leftTopButtonText.text = " ";
                rightTopButtonText.text = " ";


                break;
            case PlayerController.PlayerStates.OnDragon:                
                northButtonText.text = "Dismount Jump";
                switch (dragonController.GetDragonState)
                {

                    case DragonController.DragonStates.Mounted:

                        westButtonText.text = "Pet";
                        if(dragonController.IsPetable)
                        {
                            westButtonText.color = availableColor;
                        }
                        else
                        {
                            westButtonText.color = unavailableColor;
                        }
                        eastButtonText.text = " ";
                        eastHoldText.text = "Land (Hold)";
                        if(dragonController.CanDragonLand)
                        {
                            eastHoldText.color = availableColor;
                        }
                        else
                        {
                            eastHoldText.color = unavailableColor;
                        }

                        rightTriggerText.text = "Accel";
                        rightTriggerText.color = availableColor;
                        leftTriggerText.text = "Brake";
                        leftTriggerText.color = availableColor;
                        southButtonText.color = unavailableColor;
                        southButtonText.text = " ";

                        rightTopButtonText.text = "Yaw Right";
                        leftTopButtonText.text = "Yaw Left";
                        break;
                   
                   
                    case DragonController.DragonStates.MountedLanded:
                        eastButtonText.color = availableColor;
                        eastButtonText.text = " ";
                        eastHoldText.text = "Dismount (Hold)";
                        
                        westButtonText.text = "Pet";
                        if(dragonController.IsPetable)
                        {
                            westButtonText.color = availableColor;
                        }
                        else
                        {
                            westButtonText.color = unavailableColor;
                        }


                        if(dragonController.IsDragonGrounded)
                        {
                            southButtonText.text = "Jump";
                        }
                        else
                        {
                            southButtonText.text = "Take-Off (Smash)";
                            southButtonText.color = availableColor;
                        }
                        leftTriggerText.text = " ";
                        rightTriggerText.text = " ";
                        rightTopButtonText.text = " ";
                        leftTopButtonText.text = " ";
                        break;
                    default:
                        break;
                }
                
                break;
            case PlayerController.PlayerStates.Paravela:
                northButtonText.text = "Freefall";
                eastButtonText.color = availableColor;
                eastButtonText.text = "Call Dragon";
                eastHoldText.text = " ";
                southButtonText.color = unavailableColor;
                rightTriggerText.color = unavailableColor;
                leftTriggerText.text = " ";
                rightTopButtonText.text = " ";
                leftTopButtonText.text = " ";

                break;
            default:
                break;
        }

        
    }

    
}
