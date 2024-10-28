using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecificActionUI : MonoBehaviour
{
    [SerializeField] PlayerController playerController;
    [SerializeField] DragonController dragonController;

    Transform uiSpecific;

    // Start is called before the first frame update
    void Start()
    {
        uiSpecific = transform.GetChild(0).transform;
    }

    // Update is called once per frame
    void Update()
    {
        if(dragonController.IsMountable && !uiSpecific.gameObject.activeSelf)
        {
            uiSpecific.gameObject.SetActive(true);
            //SET TEXT?
        }
        else if(!dragonController.IsMountable && uiSpecific.gameObject.activeSelf)
        {
            uiSpecific.gameObject.SetActive(false);
        }
    }
}
