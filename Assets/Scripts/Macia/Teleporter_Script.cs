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
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player")
        {
            Debug.Log("Me muevo");
            other.GetComponent<CharacterController>().Move(teleporter_B.position - other.gameObject.transform.position);
        }
    }
}
