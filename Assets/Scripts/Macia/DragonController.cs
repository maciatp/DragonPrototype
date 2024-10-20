using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class DragonController : MonoBehaviour
{
    private float currentDragonSpeed; // Velocidad actual del drag�n
    bool isMountable = false;
    bool isGrounded = false;
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
    [SerializeField] SphereCollider calledCollider;

    //FLYING
    [SerializeField] private float pitchSpeed = 10f; // Velocidad de cabeceo (pitch)
    [SerializeField] private float rollSpeed = 10f; // Velocidad de alabeo (roll)
    [SerializeField] private float yawSpeed = 5f; // Velocidad de gui�ada (yaw)
    [SerializeField] private float acceleration = 50f; // Tasa de aceleraci�n
    [SerializeField] private float deceleration = 50f; // Tasa de frenado
    [SerializeField] private float maxFlyingSpeed = 100f; // Velocidad m�xima
    [SerializeField] private float minFlyingSpeed = 20f; // Velocidad m�nima
    [SerializeField] private float idleFlyingSpeed = 50f; // Velocidad en estado idle
    [SerializeField] private float idleForce = 20f; // Velocidad en estado idle

    [SerializeField] float flyAwayBoost = 20f; // velocidad que se a�ade a la current cuando unmounted
    private bool isAccelerating = false;
    private bool isBraking = false;

    //DISMOUNT
    bool isFlyAwayCoroutineCalled = false;

    //LANDED
    [SerializeField] BoxCollider landedCollider;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] float speedOnLand = 14f;
    [SerializeField] Transform orientation;
    [SerializeField] private float rotationSpeed = 20f;
    [SerializeField] Transform dragonObj;

    private Vector2 moveInput; // Input del joystick izquierdo (pitch y roll)
    private float yawInput; // Input de los gatillos para el yaw

    [SerializeField] CinemachineVirtualCamera dragonVcam;

    PlayerController playerController;
    Rigidbody dragonRB;

    [SerializeField] DragonStates dragonState;

    public enum DragonStates
    {
        Free,
        Called,
        Mounted,
        Dismounted,
        Landing,
        Landed,
        MountedLanded
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
    public bool IsMountable
    {
        get { return isMountable; }
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

    public void OnDismount(InputAction.CallbackContext dismountContext)
    {
        if (dismountContext.action.triggered && dragonState == DragonStates.Mounted)
        {
            DismountDragon();
        }
        if (dismountContext.action.triggered && dragonState == DragonStates.MountedLanded)
        {
            DismountDragonOnLand();
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

    //LANDED MOVEMENT
    public void OnMoveLanded(InputAction.CallbackContext moveLandedContext)
    {
        moveInput = moveLandedContext.ReadValue<Vector2>();
    }

    public void OnTakeOff(InputAction.CallbackContext takeOffContext)
    {
        if(takeOffContext.performed)
        {
            SetDragonState(DragonStates.Mounted);
            dragonObj.transform.localRotation = Quaternion.Euler(0, 0, 0); //CAMBIAR A CALCULAR Forward con la c�mara y rotar el drag�n hacia donde apunte la c�mara.
        }
    }

    private void Start()
    {
        // Guardamos la posici�n inicial del drag�n para calcular la direcci�n de movimiento
        lastPosition = transform.position;

        playerController = playerTransform.GetComponent<PlayerController>();
        dragonRB = GetComponent<Rigidbody>();
                       
    }

    private void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask);
        
        lastPosition = transform.position;
    }
    private void FixedUpdate()
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
            case DragonStates.Landing:
                //LANDING
                break;
            case DragonStates.Landed:
                //LANDED
                break;
            case DragonStates.MountedLanded:
                LandedMove();
                break;

        }

    }


    void LandedMove()
    {
        if (moveInput.sqrMagnitude > 0.01f)
        {
            //Rotaci�n y movimiento respecto a c�mara
            Vector3 viewDir = transform.position - new Vector3(dragonVcam.transform.position.x, transform.position.y, dragonVcam.transform.position.z);
            orientation.forward = viewDir.normalized;
            Vector3 moveDirection = (orientation.right * moveInput.x) + (orientation.forward * moveInput.y);
            if (moveDirection != Vector3.zero)
            {
                dragonObj.forward = Vector3.Slerp(dragonObj.forward, moveDirection.normalized, Time.deltaTime * rotationSpeed);
            }

            //MovePosition Method
            dragonRB.MovePosition(dragonRB.position + (moveDirection * speedOnLand) * Time.fixedDeltaTime);
            
        }
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

        // Actualizar la velocidad actual
        if (isAccelerating)
        {
            currentDragonSpeed += acceleration * Time.deltaTime;            
        }
        else if (isBraking)
        {
            currentDragonSpeed -= deceleration * Time.deltaTime;
        }
        else
        {
            // Si no se est� acelerando ni frenando, volver gradualmente a la velocidad idle
            if (currentDragonSpeed > idleFlyingSpeed)
            {
                currentDragonSpeed -= idleForce * Time.deltaTime;
                if (currentDragonSpeed < idleFlyingSpeed)
                    currentDragonSpeed = idleFlyingSpeed;
            }
            else if (currentDragonSpeed < idleFlyingSpeed)
            {
                currentDragonSpeed += idleForce * Time.deltaTime;
                if (currentDragonSpeed > idleFlyingSpeed)
                    currentDragonSpeed = idleFlyingSpeed;
            }
        }

        // Limitar la velocidad actual entre 0 y la velocidad m�xima
        currentDragonSpeed = Mathf.Clamp(currentDragonSpeed, minFlyingSpeed, maxFlyingSpeed);

        // Mover hacia adelante con la velocidad actual
        transform.position += transform.forward * currentDragonSpeed * Time.deltaTime;
    }

    //DISMOUNTED
    void FlyAway()
    {
        transform.position += transform.forward * (currentDragonSpeed + flyAwayBoost) * Time.deltaTime;
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
        // Posicionar el drag�n justo detr�s del player cuando sea llamado
        transform.position = playerTransform.position - playerTransform.forward * spawnDistance; // Drag�n a 5 unidades detr�s del player
        calledCollider.enabled = true;
    }
    private void FlyTowardsPlayer()
    {

        // Volar hacia el player
        transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, calledSpeed * Time.deltaTime);
        RotateTowardsMovement(playerTransform.position);

    }

    private void RotateTowardsMovement(Vector3 target)
    {
        // Rotar el drag�n hacia la direcci�n en la que se mueve
        Vector3 direction = (target - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = targetRotation;
        }
    }

    //LAND
    public void CallDragonToLand()
    {

        // Selecciona un punto aleatorio en un cono frente al player
        Vector3 randomPointInView = GetRandomPointInCone(playerTransform);
        
        SetDragonState(DragonStates.Landing);
        calledCollider.enabled = false;
        StartCoroutine(LandAtPoint(randomPointInView));
    }

    private IEnumerator LandAtPoint(Vector3 targetPosition)
    {
        RotateTowardsMovement(targetPosition);
        float landDuration = 2f; // Duraci�n del aterrizaje
        float elapsedTime = 0f;

        Vector3 startPosition = transform.position;
        while (elapsedTime < landDuration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / landDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition; // Asegura que llegue al punto exacto al final
        transform.eulerAngles = new Vector3(0, transform.rotation.eulerAngles.y, 0); //SETEO rotaci�n al aterrizar

        SetDragonState(DragonStates.Landed); // O cualquier otro estado
        landedCollider.enabled = true;
    }

    private Vector3 GetRandomPointInCone(Transform player)
    {
        float angle = 45f; // �ngulo del cono
        float distance = 10f; // Distancia m�xima del aterrizaje
        Vector3 forward = player.forward;

        Vector3 randomDirection = Quaternion.Euler(
            Random.Range(-angle / 2, angle / 2),
            Random.Range(-angle / 2, angle / 2),
            0) * forward;

        Vector3 randomPoint = player.position + randomDirection * Random.Range(5f, distance);
        randomPoint.y = player.position.y; // Mant�n la altura similar al jugador

        return randomPoint;
    }

    //MOUNT
    public void MountDragon()
    {
        SetDragonState(DragonStates.Mounted);


        playerController.MountDragon();
        //DRAGON CAM ON
        dragonVcam.gameObject.SetActive(true);

        //DEACTIVATE TRIGGERS
        DeactivateTriggers();

    }


    public void MountDragonOnLand()
    {
        SetDragonState(DragonStates.MountedLanded);
        playerController.MountDragon();



        //Igualo rotaci�n del gameobject padre al interno        
        dragonObj.localRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        transform.rotation = Quaternion.Euler(0,0,0);


        //DRAGON CAM ON
        dragonVcam.gameObject.SetActive(true);

        //DEACTIVATE TRIGGERS
        DeactivateTriggers();

        isMountable = false;
    }

    private void DeactivateTriggers()
    {
        if (landedCollider.enabled)
        {
            landedCollider.enabled = false;
        }
        if (calledCollider.enabled)
        {
            calledCollider.enabled = false;
        }
    }

    //DISMOUNT
    void DismountDragon()
    {
        SetDragonState(DragonStates.Dismounted);
        playerController.DismountDragon();

        //Dragon CAM OFF
        dragonVcam.gameObject.SetActive(false);
    }
    void DismountDragonOnLand()
    {
        SetDragonState(DragonStates.Landed);
        playerController.DismountDragon();
       
        
        //Igualo rotaci�n del gameobject padre al interno       
        transform.rotation = Quaternion.Euler(0, dragonObj.rotation.eulerAngles.y, 0);
        dragonObj.localRotation = Quaternion.Euler(0,0,0);
        

        //Dragon CAM OFF
        dragonVcam.gameObject.SetActive(false);
        landedCollider.enabled = true;
    }

    //SET DRAGON STATE
    public void SetDragonState(DragonStates _dragonState)
    {
        dragonState = _dragonState; // M�todo para cambiar el estado del drag�n
    }

    //TRIGGERS
    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.parent.tag == "Player" && dragonState == DragonStates.Called)
        {
            MountDragon();
        }        
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.transform.parent.tag == "Player" && dragonState == DragonStates.Landed && !isMountable)
        {
            //CAN MOUNT
            isMountable = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.transform.parent.tag == "Player")
        {
            //CAN'T MOUNT
            isMountable = false;
        }
    }

}
