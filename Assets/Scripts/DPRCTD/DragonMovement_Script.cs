using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DragonMovement_Script : MonoBehaviour
{
    [SerializeField] Transform playerTransform;
    
    public Transform GetDragonTransform() { return gameObject.transform; }

    [SerializeField] float rollRate = 10;
    [SerializeField] float pitchRate = 10;
    [SerializeField] float yawRate = 10;
    [SerializeField] float acceleration = 5f;
    [SerializeField] float deceleration = 5f;
    [SerializeField] float idleDeceleration = 5f;
    [SerializeField] float currentFlyingSpeed = 20;
    [SerializeField] float minFlyingSpeed = 10f;
    [SerializeField] float idleFlyingSpeed = 20;
    [SerializeField] float maxFlyingSpeed = 50f;
    [SerializeField] float calledFlyingSpeed = 30;
     float flyOffFlySpeed;


    [SerializeField] private float circleRadius = 50f;              // Radio del círculo
    [SerializeField] private float flyHeight = 10f;                  // Altura a la que vuela el dragón en círculo
    [SerializeField] private float circleSpeed = 10f;               // Velocidad de desplazamiento en círculo

    [SerializeField] DragonStates dragonState;
   public DragonStates GetDragonState
    {
        get { return dragonState; }
        set { dragonState = value; }
    }

    public enum DragonStates
    {
        Free,
        Called,
        Mounted,
        Dismounted
    }

    // Start is called before the first frame update
    void Start()
    {
        //PARA TESTING en la escena dragon
        if(SceneManager.GetActiveScene().name != "DragonTest_Scene")
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        }
        
    }

    // Update is called once per frame
    void Update()
    {
        //VUELO EN CÍRCULO
        if(dragonState == DragonStates.Free)
        {
            FlyInCircle();
        }

        //LO LLAMO
        if(dragonState == DragonStates.Called)
        {
            Vector3 moveVector = new Vector3(playerTransform.position.x - gameObject.transform.position.x, playerTransform.position.y - gameObject.transform.position.y, playerTransform.position.z - gameObject.transform.position.z);
            gameObject.transform.position += moveVector * calledFlyingSpeed * Time.deltaTime;
            
        }

        //VOLANDO
        if(dragonState == DragonStates.Mounted)
        {
            // Control de aceleración
            if (Input.GetButton("Fire1")) // Botón A para acelerar
            {
                currentFlyingSpeed += acceleration * Time.deltaTime; // Aumentar la velocidad
                currentFlyingSpeed = Mathf.Clamp(currentFlyingSpeed, minFlyingSpeed, maxFlyingSpeed); // Limitar a la velocidad máxima
            }
            else if (Input.GetButton("Fire3")) // Botón X para desacelerar
            {
                currentFlyingSpeed -= deceleration * Time.deltaTime; // Disminuir la velocidad
                currentFlyingSpeed = Mathf.Clamp(currentFlyingSpeed, minFlyingSpeed, maxFlyingSpeed); // Asegurar que no sea negativo
            }
            else
            {
                // Si no se presionan los botones, se aplica una desaceleración suave
                currentFlyingSpeed = Mathf.MoveTowards(currentFlyingSpeed, idleFlyingSpeed, idleDeceleration * Time.deltaTime);
            }

            transform.position += transform.forward * Time.deltaTime * currentFlyingSpeed;
            transform.Rotate(Input.GetAxis("Vertical")*pitchRate, 0.0f*yawRate, -Input.GetAxis("Horizontal")*rollRate); //TODO: implement YAW
        }


        //DESMONTO
        if(dragonState == DragonStates.Dismounted)
        {
            flyOffFlySpeed = maxFlyingSpeed * 1.25f;
            transform.position += transform.forward * flyOffFlySpeed * Time.deltaTime;
        }        
    }

    private void OnTriggerEnter(Collider other)
    {
        if((other.tag == "Player") && dragonState != DragonStates.Mounted)
        {
           MountDragon(other);
        }
    }

    public void MountDragon(Collider other)
    {
        //ROTO EL DRAGON PARA QUE ESTÉ CENTRADO
        gameObject.transform.rotation = Quaternion.identity;
        //COLOCO AL JUGADOR EN LA SILLA del dragon
        other.transform.position = gameObject.transform.GetChild(0).transform.position;
        //seteo el dragon como padre para que siga el movimiento
        other.transform.SetParent(gameObject.transform.GetChild(0).transform);

       // other.GetComponent<ThirdPersonMovement_Script>().IsOnDragon(gameObject);


        SetDragonState(DragonStates.Mounted);
    }

    public void DismountDragon(GameObject player)
    {
        player.transform.SetParent(null);

        //Camera.main.GetComponent<CinemachineBrain>().enabled = true;

        SetDragonState(DragonStates.Dismounted);

        StartCoroutine("DragonCooldownAfterJumpingOff");
    }

    IEnumerator DragonCooldownAfterJumpingOff()
    {
        
        gameObject.GetComponent<SphereCollider>().enabled = false;
        yield return new WaitForSeconds(2);
        gameObject.GetComponent<SphereCollider>().enabled = true;
        SetDragonState(DragonStates.Free);
    }

    private void FlyInCircle()
    {
        // Calcular la posición circular alrededor del jugador
        if (playerTransform)
        {
            float angle = Time.time * circleSpeed; // Ángulo de rotación
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * circleRadius; // Cálculo del desplazamiento en círculo
            Vector3 targetPosition = playerTransform.position + new Vector3(offset.x, flyHeight, offset.z); // Nueva posición en círculo


            // Mover el dragón a la posición objetivo
            transform.position = targetPosition;


            // Hacer que el dragón mire hacia el interior del círculo
            Vector3 tangentDirection = new Vector3(-Mathf.Sin(angle),0, Mathf.Cos(angle)); // Dirección tangente
            Quaternion lookRotation = Quaternion.LookRotation(tangentDirection); // Crear la rotación que mira hacia el jugador
            lookRotation *= Quaternion.Euler(-5, -15, 20); // Añadir rotación al dragón
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * yawRate); // Suavizar la rotación
        }

    }

    public void SetDragonState(DragonStates _dragonState)
    {
        dragonState = _dragonState; // Método para cambiar el estado del dragón
    }

    public void CallDragon()
    {
        SetDragonState(DragonStates.Called);
    }
}
