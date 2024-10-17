using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DragonController : MonoBehaviour
{
    //FREE
    [SerializeField] private Transform playerTransform; // El jugador al que el dragón sigue.
    [SerializeField] private float circleHeight = 20f; // Altura a la que vuela el dragón.
    [SerializeField] private float circleRadius = 15f; // Radio del círculo que describe el vuelo.
    [SerializeField] private float circleSpeed = 10f; // Velocidad con la que el dragón vuela en círculos.
    [SerializeField] private float pitchAngle = 0f; // Ángulo de alabeo del dragón para simular el giro.
    [SerializeField] private float yawAngle = 0f; // Ángulo de alabeo del dragón para simular el giro.
    [SerializeField] private float rollAngle = 30f; // Ángulo de alabeo del dragón para simular el giro.
    private Vector3 lastPosition; // Para calcular la dirección del movimiento.
    //CALLED
    [SerializeField] private float calledSpeed = 100f; // Velocidad del dragón cuando es llamado.
    [SerializeField] float spawnDistance = 5f; // Distancia a la que se posiciona el dragón al llamarlo
    [SerializeField] Transform playerPos; //Position when Mounting

    //FLYING
    [SerializeField] private float pitchSpeed = 10f; // Velocidad de cabeceo (pitch)
    [SerializeField] private float rollSpeed = 10f; // Velocidad de alabeo (roll)
    [SerializeField] private float yawSpeed = 5f; // Velocidad de guiñada (yaw)
    [SerializeField] private float acceleration = 50f; // Tasa de aceleración
    [SerializeField] private float deceleration = 50f; // Tasa de frenado
    [SerializeField] private float maxFlyingSpeed = 100f; // Velocidad máxima
    [SerializeField] private float idleFlyingSpeed = 50f; // Velocidad en estado idle
    [SerializeField] private float idleForce = 20f; // Velocidad en estado idle

    [SerializeField] float flyAwaySpeed = 70f; // velocidad de escape cuando unmounted
    private float currentSpeed; // Velocidad actual del dragón
    private bool isAccelerating = false;
    private bool isBraking = false;

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

    // Input para mover el dragón (joystick izquierdo)
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    // Input para controlar el yaw (gatillos)
    public void OnYaw(InputAction.CallbackContext context)
    {
        yawInput = context.ReadValue<float>();
    }

    public void OnDismount(InputAction.CallbackContext dismountContext)
    {
        if (dismountContext.action.triggered && dragonState == DragonStates.Mounted)
        {
            DismountDragon();
        }
    }

    // Input para acelerar
    public void OnAccelerate(InputAction.CallbackContext accelerateContext)
    {
        if (accelerateContext.action.IsPressed())
        {
            isAccelerating = true;
        }
        if (accelerateContext.action.WasReleasedThisFrame())
        {
            isAccelerating = false;
        }
    }

    // Input para frenar
    public void OnBrake(InputAction.CallbackContext brakeContext)
    {
        if (brakeContext.action.IsPressed())
        {
            isBraking = true;
        }
        if (brakeContext.action.WasReleasedThisFrame())
        {
            isBraking = false;
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
                Fly();
                break;
            case DragonStates.Dismounted:
                FlyAway();
                break;

        }

        lastPosition = transform.position;
    }

    // Método para volar
    private void Fly()
    {
        // Movimiento de pitch (cabeceo) y roll (alabeo)
        float pitch = moveInput.y * pitchSpeed * Time.deltaTime;
        float roll = moveInput.x * rollSpeed * Time.deltaTime;

        // Movimiento de yaw (guiñada) con los gatillos
        float yaw = yawInput * yawSpeed * Time.deltaTime;

        // Aplicar las rotaciones
        transform.Rotate(pitch, yaw, -roll, Space.Self); // Invertimos el roll para que gire de manera correcta

        // Actualizar la velocidad actual
        if (isAccelerating)
        {
            currentSpeed += acceleration * Time.deltaTime;            
        }
        else if (isBraking)
        {
            currentSpeed -= deceleration * Time.deltaTime;
        }
        else
        {
            // Si no se está acelerando ni frenando, volver gradualmente a la velocidad idle
            if (currentSpeed > idleFlyingSpeed)
            {
                currentSpeed -= idleForce * Time.deltaTime;
                if (currentSpeed < idleFlyingSpeed)
                    currentSpeed = idleFlyingSpeed;
            }
            else if (currentSpeed < idleFlyingSpeed)
            {
                currentSpeed += idleForce * Time.deltaTime;
                if (currentSpeed > idleFlyingSpeed)
                    currentSpeed = idleFlyingSpeed;
            }
        }

        // Limitar la velocidad actual entre 0 y la velocidad máxima
        currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxFlyingSpeed);

        // Mover hacia adelante con la velocidad actual
        transform.position += transform.forward * currentSpeed * Time.deltaTime;
    }

    //DISMOUNTED
    void FlyAway()
    {
        transform.position += transform.forward * flyAwaySpeed * Time.deltaTime;
        if (!isFlyAwayCoroutineCalled)
        {
            StartCoroutine(FlyAwayCooldown());
        }
    }
    IEnumerator FlyAwayCooldown()
    {
        isFlyAwayCoroutineCalled = true;
        yield return new WaitForSecondsRealtime(2);
        SetDragonState(DragonStates.Free);
        isFlyAwayCoroutineCalled = false;
        yield return null;
    }

    //FREE
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
    private void ApplyRoll()
    {
        // Aplicamos un ángulo de rotación en el eje Z (roll) para simular el alabeo.
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
        dragonState = _dragonState; // Método para cambiar el estado del dragón
    }

    //TRIGGERS
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && dragonState == DragonStates.Called)
        {
            MountDragon();
        }
    }
}
