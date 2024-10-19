using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleporter_Script : MonoBehaviour
{

    [SerializeField]
    Transform teleporter_B;

    private void Awake()
    {
        if(teleporter_B == null)
        {
            teleporter_B = GameObject.Find("Teleporter_B").transform;
            
        }
    } 

    private void OnTriggerEnter(Collider other)
    {
        if(other.transform.parent.tag == "Player")
        {
            Debug.Log("Me muevo");
            other.GetComponentInParent<Rigidbody>().MovePosition(teleporter_B.position);
        }
    }
}
