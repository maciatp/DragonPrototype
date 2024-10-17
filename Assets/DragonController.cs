using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DragonController : MonoBehaviour
{
    [SerializeField] private Transform playerTransform; // El jugador al que el dragón sigue.
    [SerializeField] private float circleHeight = 20f; // Altura a la que vuela el dragón.
    [SerializeField] private float circleRadius = 15f; // Radio del círculo que describe el vuelo.
    [SerializeField] private float circleSpeed = 10f; // Velocidad con la que el dragón vuela en círculos.
    [SerializeField] private float pitchAngle = 0f; // Ángulo de alabeo del dragón para simular el giro.
    [SerializeField] private float yawAngle = 0f; // Ángulo de alabeo del dragón para simular el giro.
    [SerializeField] private float rollAngle = 30f; // Ángulo de alabeo del dragón para simular el giro.
    [SerializeField] private float calledSpeed = 100f; // Velocidad del dragón cuando es llamado.
    [SerializeField] float spawnDistance = 5f; // Distancia a la que se posiciona el dragón al llamarlo
  
    [SerializeField] Transform playerPos; //Position when Mounting

    private Vector3 lastPosition; // Para calcular la dirección del movimiento.

    PlayerController playerController;

    [SerializeField] DragonStates dragonState;

    public enum DragonStates
    {
        Free,
        Called,
        Mounted,
        Dismounted
    }
    public DragonStates GetDragonState
    {
        get { return dragonState; }
        set { dragonState = value; }
    }

    public Transform GetPlayerPos
    {
        get { return playerPos; }
    }
       


    public void OnDismount(InputAction.CallbackContext context)
    {
        if(context.action.triggered && dragonState == DragonStates.Mounted)
        {
            Debug.Log("Pulso Botón desmontar");
            DismountDragon();
        }
    }

    private void Start()
    {
        // Guardamos la posición inicial del dragón para calcular la dirección de movimiento
        lastPosition = transform.position;

        playerController = playerTransform.GetComponent<PlayerController>();
    }

    private void Update()
    {
        switch (dragonState)
        {
            case DragonStates.Free:
                FlyInCircle();
                break;
            case DragonStates.Called:
                FlyTowardsPlayer();
                break;
            case DragonStates.Mounted:
                Debug.Log("FLYING");
                break;
            case DragonStates.Dismounted:
                //FLY AWAY
                break;
                
        }

        lastPosition = transform.position;
    }

    private void FlyInCircle()
    {
        float angle = Time.time * circleSpeed; // Ángulo de rotación
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * circleRadius; // Desplazamiento en círculo
        Vector3 targetPosition = playerTransform.position + new Vector3(offset.x, circleHeight, offset.z); // Nueva posición en círculo

        // Mover el dragón a la posición objetivo
        transform.position = targetPosition;

        // Calculamos la dirección de movimiento basada en el desplazamiento en el círculo.
        Vector3 direction = (transform.position - lastPosition).normalized;

        // Rotamos el dragón para que mire hacia la dirección de su movimiento.
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = targetRotation;
        }

        ApplyRoll();
    }

    private void FlyTowardsPlayer()
    {
        // Posicionar el dragón justo detrás del player cuando sea llamado
        Vector3 targetPosition = playerTransform.position - playerTransform.forward * spawnDistance; // Dragón a 5 unidades detrás del player

        // Volar hacia el player
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, calledSpeed * Time.deltaTime);

        // Rotar el dragón hacia la dirección en la que se mueve
        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = targetRotation;
        }

    }

    private void ApplyRoll()
    {
        // Aplicamos un ángulo de rotación en el eje Z (roll) para simular el alabeo.
        Quaternion rollRotation = Quaternion.Euler(pitchAngle, yawAngle, rollAngle);
        transform.rotation *= rollRotation;
    }

    public void CallDragon()
    {
        SetDragonState(DragonStates.Called);        
    }

    void MountDragon()
    {
        SetDragonState(DragonStates.Mounted);
        Debug.Log("MOUNTED from Dragon");
        playerController.MountDragon();
        
    }

    void DismountDragon()
    {
        SetDragonState(DragonStates.Dismounted);        
        Debug.Log("Dismount from Dragon");
        playerController.DismountDragon();
    }

    //SET DRAGON STATE
    public void SetDragonState(DragonStates _dragonState)
    {
        dragonState = _dragonState; // Método para cambiar el estado del dragón
    }


    //TRIGGERS
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && dragonState == DragonStates.Called)
        {
            {
                MountDragon();
                Debug.Log("Dragon Trigger Entered");
            }
        }
    }
}
