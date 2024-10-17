using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DragonController : MonoBehaviour
{
    //FREE
    [SerializeField] private Transform playerTransform; // El jugador al que el drag�n sigue.
    [SerializeField] private float circleHeight = 20f; // Altura a la que vuela el drag�n.
    [SerializeField] private float circleRadius = 15f; // Radio del c�rculo que describe el vuelo.
    [SerializeField] private float circleSpeed = 10f; // Velocidad con la que el drag�n vuela en c�rculos.
    [SerializeField] private float pitchAngle = 0f; // �ngulo de alabeo del drag�n para simular el giro.
    [SerializeField] private float yawAngle = 0f; // �ngulo de alabeo del drag�n para simular el giro.
    [SerializeField] private float rollAngle = 30f; // �ngulo de alabeo del drag�n para simular el giro.
    private Vector3 lastPosition; // Para calcular la direcci�n del movimiento.
    //CALLED
    [SerializeField] private float calledSpeed = 100f; // Velocidad del drag�n cuando es llamado.
    [SerializeField] float spawnDistance = 5f; // Distancia a la que se posiciona el drag�n al llamarlo
    [SerializeField] Transform playerPos; //Position when Mounting

    //FLYING
    [SerializeField] private float pitchSpeed = 10f; // Velocidad de cabeceo (pitch)
    [SerializeField] private float rollSpeed = 10f; // Velocidad de alabeo (roll)
    [SerializeField] private float yawSpeed = 5f; // Velocidad de gui�ada (yaw)
    [SerializeField] private float flyingSpeed = 50f; // Velocidad de vuelo fija por ahora

    //DISMOUNT
    bool isFlyAwayCoroutineCalled = false;

    private Vector2 moveInput; // Input del joystick izquierdo (pitch y roll)
    private float yawInput; // Input de los gatillos para el yaw

    [SerializeField] CinemachineVirtualCamera dragonVcam;

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


    // Input para mover el drag�n (joystick izquierdo)
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    // Input para controlar el yaw (gatillos)
    public void OnYaw(InputAction.CallbackContext context)
    {
        yawInput = context.ReadValue<float>();
    }

    public void OnDismount(InputAction.CallbackContext context)
    {
        if(context.action.triggered && dragonState == DragonStates.Mounted)
        {
           DismountDragon();
        }
    }

    private void Start()
    {
        // Guardamos la posici�n inicial del drag�n para calcular la direcci�n de movimiento
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
                Fly();
                break;
            case DragonStates.Dismounted:
                FlyAway();
                break;
                
        }

        lastPosition = transform.position;
    }

    // M�todo para volar
    private void Fly()
    {
        // Movimiento de pitch (cabeceo) y roll (alabeo)
        float pitch = moveInput.y * pitchSpeed * Time.deltaTime;
        float roll = moveInput.x * rollSpeed * Time.deltaTime;

        // Movimiento de yaw (gui�ada) con los gatillos
        float yaw = yawInput * yawSpeed * Time.deltaTime;

        // Aplicar las rotaciones
        transform.Rotate(pitch, yaw, -roll, Space.Self); // Invertimos el roll para que gire de manera correcta

        // Mover hacia adelante
        transform.position += transform.forward * flyingSpeed * Time.deltaTime;
    }

    //DISMOUNTED
    void FlyAway()
    {
        transform.position += transform.forward * flyingSpeed * Time.deltaTime;
        if(!isFlyAwayCoroutineCalled)
        {
            StartCoroutine(FlyAwayCooldown());
        }
    }
    IEnumerator FlyAwayCooldown()
    {
        isFlyAwayCoroutineCalled = true;        
        yield return new WaitForSecondsRealtime(2);
        SetDragonState(DragonStates.Free);
        yield return null;
    }
    
    //FREE
    private void FlyInCircle()
    {
        float angle = Time.time * circleSpeed; // �ngulo de rotaci�n
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * circleRadius; // Desplazamiento en c�rculo
        Vector3 targetPosition = playerTransform.position + new Vector3(offset.x, circleHeight, offset.z); // Nueva posici�n en c�rculo

        // Mover el drag�n a la posici�n objetivo
        transform.position = targetPosition;

        // Calculamos la direcci�n de movimiento basada en el desplazamiento en el c�rculo.
        Vector3 direction = (transform.position - lastPosition).normalized;

        // Rotamos el drag�n para que mire hacia la direcci�n de su movimiento.
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = targetRotation;
        }

        ApplyRoll();
    }
    private void ApplyRoll()
    {
        // Aplicamos un �ngulo de rotaci�n en el eje Z (roll) para simular el alabeo.
        Quaternion rollRotation = Quaternion.Euler(pitchAngle, yawAngle, rollAngle);
        transform.rotation *= rollRotation;
    }
    //CALLED
    public void CallDragon()
    {
        SetDragonState(DragonStates.Called);        
    }
    private void FlyTowardsPlayer()
    {
        // Posicionar el drag�n justo detr�s del player cuando sea llamado
        Vector3 targetPosition = playerTransform.position - playerTransform.forward * spawnDistance; // Drag�n a 5 unidades detr�s del player

        // Volar hacia el player
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, calledSpeed * Time.deltaTime);

        // Rotar el drag�n hacia la direcci�n en la que se mueve
        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = targetRotation;
        }

    }


    //MOUNT
    void MountDragon()
    {
        SetDragonState(DragonStates.Mounted);        
        playerController.MountDragon();
        //DRAGON CAM ON
        dragonVcam.gameObject.SetActive(true);
        
    }

    //DISMOUNT
    void DismountDragon()
    {
        SetDragonState(DragonStates.Dismounted);        
        playerController.DismountDragon();
       
        //Dragon CAM OFF
        dragonVcam.gameObject.SetActive(false);
    }

    //SET DRAGON STATE
    public void SetDragonState(DragonStates _dragonState)
    {
        dragonState = _dragonState; // M�todo para cambiar el estado del drag�n
    }


    //TRIGGERS
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && dragonState == DragonStates.Called)
        {
            {
                MountDragon();
            }
        }
    }
}
