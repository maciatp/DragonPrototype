using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TreeEditor;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class DragonController : MonoBehaviour
{
    private float currentYVelocity; //DEBUG
    private float currentDragonSpeed; // Velocidad actual del dragón
    bool isMountable = false;
    bool isGrounded = false;
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
    [SerializeField] SphereCollider calledCollider;

    //FLYING
    [SerializeField] private float pitchSpeed = 10f; // Velocidad de cabeceo (pitch)
    [SerializeField] private float rollSpeed = 10f; // Velocidad de alabeo (roll)
    [SerializeField] private float yawSpeed = 5f; // Velocidad de guiñada (yaw)
    [SerializeField] private float acceleration = 50f; // Tasa de aceleración
    [SerializeField] private float deceleration = 50f; // Tasa de frenado
    [SerializeField] private float maxFlyingSpeed = 100f; // Velocidad máxima
    [SerializeField] private float minFlyingSpeed = 20f; // Velocidad mínima
    [SerializeField] private float idleFlyingSpeed = 50f; // Velocidad en estado idle
    [SerializeField] private float idleForce = 20f; // Velocidad en estado idle

    [SerializeField] float flyAwayBoost = 20f; // velocidad que se añade a la current cuando unmounted
    private bool isAccelerating = false;
    private bool isBraking = false;

    //DISMOUNT MOUNTED JUMP
    bool isFlyAwayCoroutineCalled = false;
    //DISMOUNT LANDED
    [SerializeField] Transform dismountTransform;

    //LANDING MOUNTED
    private bool canLand = false; // Define si es posible aterrizar
    Vector3 landingPoint;
    [SerializeField] float landingForwardOffset = 10;
    [SerializeField] float landMountedDuration = 2f; // Duración del aterrizaje cuando montas en el dragón
    [SerializeField] float landingSpeedThreshold = 40f; //mínima velocidad para poder aterrizar
    [SerializeField] float distanceToLand = 10f;
    //LANDING MOUNTED DEBUG
    [SerializeField] GameObject landingDebug;

    //LANDING UNMOUNTEd
    [SerializeField] float landUnmountedDuration = 2f; // Duración del aterrizaje cuando NO MONTAS

    //LANDED
    [SerializeField] BoxCollider landedCollider;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] float speedOnLand = 14f;
    [SerializeField] Transform orientation;
    [SerializeField] private float rotationSpeed = 20f;
    [SerializeField] Transform dragonObj;

    //JUMP
    [SerializeField] float jumpHeight = 4;

    //MOUNT ON JUMP
    [SerializeField] CapsuleCollider mountCollider;

 

    private Vector2 moveInput; // Input del joystick izquierdo (pitch y roll)
    private float yawInput; // Input de los gatillos para el yaw

    [SerializeField] CinemachineVirtualCamera dragonFlyingVcam;
    [SerializeField] CinemachineFreeLook dragonLandVcam;

    PlayerController playerController;
   [SerializeField] Rigidbody dragonRB;

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
        if (dismountContext.action.triggered && dragonState == DragonStates.MountedLanded)
        {
            DismountDragonOnLand();
        }
    }

    public void OnDismountJump(InputAction.CallbackContext dismountJumpContext)
    {
        if (dismountJumpContext.action.triggered && (dragonState == DragonStates.Mounted || dragonState == DragonStates.MountedLanded))
        {
            DismountJump();
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

    //LAND WHILE MOUNTED
    public void OnLandDragon(InputAction.CallbackContext landDragonContext)
    {
        if (landDragonContext.performed && dragonState == DragonStates.Mounted)
        {            
            PrepareLandingMounted();
        }
    }

    //LANDED MOVEMENT
    public void OnMoveLanded(InputAction.CallbackContext moveLandedContext)
    {
        moveInput = moveLandedContext.ReadValue<Vector2>();
    }
   
    //TAKE OFF
    public void OnTakeOff(InputAction.CallbackContext takeOffContext)
    {
        if(takeOffContext.performed && (dragonState == DragonStates.Landed || dragonState == DragonStates.MountedLanded))
        {
            //TAKE OFF
            TakeOff();
        }
    }

    //JUMP
    public void OnDragonJump(InputAction.CallbackContext jumpContext)
    {
        if (jumpContext.action.triggered) //Sólo se llama una vez, cuando pulsas el botón
        {
            if (isGrounded && dragonState == DragonStates.MountedLanded)
            {
                DragonJump();
            }
        }
    }

    public void DragonJump()
    {
        // Obtener la dirección en la que mira la cámara, sin afectar el eje Y (plano horizontal)
        Vector3 jumpDirection = (Camera.main.transform.right * moveInput.x) + (Camera.main.transform.forward * moveInput.y);
        jumpDirection.y = 0; // Asegurarse de que la dirección sea sólo en el plano XZ


        // Aplicar la fuerza hacia arriba
        dragonRB.AddForce(jumpDirection * speedOnLand + Vector3.up * Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y), ForceMode.VelocityChange);

    }

    private void Awake()
    {
        if(dragonRB == null)
        {
            dragonRB = GetComponent<Rigidbody>(); //en awake para que los otros gameobject en start lo encuentren
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
        //DEBUG
        currentYVelocity = dragonRB.velocity.y;

        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask);
        
        lastPosition = transform.position;

        if (dragonState == DragonStates.Mounted && currentDragonSpeed < landingSpeedThreshold && isBraking)
        {
            // Selecciona un punto en el suelo delante del dragón.
            Vector3 forwardDirection = transform.forward;
            landingPoint = playerTransform.position + forwardDirection * landingForwardOffset; // landingPoint es landingForwardOffset unidades adelante

            // Hacer raycast hacia abajo desde el punto calculado para encontrar el suelo.
            RaycastHit hit;
            if (Physics.Raycast(landingPoint, Vector3.down, out hit, distanceToLand, groundMask))
            {
                if(!landingDebug.activeSelf)
                {
                    landingDebug.SetActive(true);
                }
                // Si el raycast encuentra el suelo, ajusta la altura del punto de aterrizaje.
                landingPoint.y = hit.point.y - dragonObj.transform.GetChild(0).transform.localPosition.y;
                landingDebug.transform.position = landingPoint;
                landingDebug.transform.localRotation = Quaternion.Euler(-transform.rotation.eulerAngles.x,0,-transform.rotation.eulerAngles.z);
                // Ahora puedes aterrizar
                canLand = true;                
            }
            else
            {
                canLand = false;

                //DEBUG
                landingDebug.transform.localPosition = Vector3.zero;
                landingDebug.transform.localRotation = Quaternion.identity;
                landingDebug.SetActive(false);
            }
        }
        else
        {            
            canLand = false;            
            
            //DEBUG
            landingDebug.transform.localPosition = Vector3.zero;
            landingDebug.transform.localRotation = Quaternion.identity;
            landingDebug.SetActive(false);
        }

        if(dragonState == DragonStates.MountedLanded && !dragonRB.useGravity)
        {
            dragonRB.useGravity = true;
        }
        if(dragonState != DragonStates.MountedLanded &&  dragonRB.useGravity)
        {
            dragonRB.useGravity = false;
        }

        //DESPEGUE POR CAÍDA
        if(dragonState == DragonStates.MountedLanded && dragonRB.velocity.y < -15 && !isGrounded)
        {
            TakeOff();
        }
       
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
            currentDragonSpeed += acceleration * Time.deltaTime;            
        }
        else if (isBraking)
        {
            currentDragonSpeed -= deceleration * Time.deltaTime;
        }
        else
        {
            // Si no se está acelerando ni frenando, volver gradualmente a la velocidad idle
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

        // Limitar la velocidad actual entre 0 y la velocidad máxima
        currentDragonSpeed = Mathf.Clamp(currentDragonSpeed, minFlyingSpeed, maxFlyingSpeed);

        // Mover hacia adelante con la velocidad actual
        transform.position += transform.forward * currentDragonSpeed * Time.deltaTime;
        //Probar con DragonRb.MovePosition, puede que así no cruce los colliders.
    }

    //DISMOUNTED
    void FlyAway()
    {
        if(dragonObj.transform.localRotation != Quaternion.Euler(0,0,0))
        {
            dragonObj.localRotation = Quaternion.Euler(0,0,0);
            Debug.Log("Esto de llama?? Rotation dragonOBJ");
        }
        transform.position += transform.forward * (currentDragonSpeed + flyAwayBoost) * Time.deltaTime;
        if (!isFlyAwayCoroutineCalled)
        {
            StartCoroutine(FlyAwayCooldown());
        }
    }
    IEnumerator FlyAwayCooldown()
    {
        isFlyAwayCoroutineCalled = true;
        //SOME CODE CLEANING
        if(isAccelerating)
        {
            isAccelerating = false;
        }
        if(isBraking)
        {
            isBraking = false;
        }
        currentDragonSpeed = idleFlyingSpeed;
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
        // Posicionar el dragón justo detrás del player cuando sea llamado
        dragonObj.localRotation = Quaternion.Euler(0,0,0);
        transform.position = playerTransform.position - playerTransform.forward * spawnDistance; // Dragón a 5 unidades detrás del player
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
        // Rotar el dragón hacia la dirección en la que se mueve
        Vector3 direction = (target - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = targetRotation;
        }
    }

    //LAND
    void LandedMove()
    {
        if (moveInput.sqrMagnitude > 0.01f)
        {
            //Rotación y movimiento respecto a cámara
            Vector3 viewDir = transform.position - new Vector3(dragonLandVcam.transform.position.x, transform.position.y, dragonLandVcam.transform.position.z);
            orientation.forward = viewDir.normalized;
            if(isGrounded)
            {

                Vector3 moveDirection = (orientation.right * moveInput.x) + (orientation.forward * moveInput.y);
                if (moveDirection != Vector3.zero)
                {
                    dragonObj.forward = Vector3.Slerp(dragonObj.forward, moveDirection.normalized, Time.deltaTime * rotationSpeed);
                }

                //MovePosition Method
                dragonRB.MovePosition(dragonRB.position + (moveDirection * speedOnLand) * Time.fixedDeltaTime);
            }
            
        }
    }
    //LANDING UNMOUNTED
    public void CallDragonToLand()
    {

        // Selecciona un punto aleatorio en un cono frente a la cámara
        Vector3 randomPointInView = GetRandomPointInCone(playerTransform);
        
        SetDragonState(DragonStates.Landing);
        calledCollider.enabled = false;
        StartCoroutine(LandAtPointUnmounted(randomPointInView));
    }
    private Vector3 GetRandomPointInCone(Transform player)
    {
        float angle = 45f; // Ángulo del cono
        float distance = 10f; // Distancia máxima del aterrizaje
        Vector3 forward = Camera.main.transform.forward;

        Vector3 randomDirection = Quaternion.Euler(
            Random.Range(-angle / 2, angle / 2),
            Random.Range(-angle / 2, angle / 2),
            0) * forward;

        Vector3 randomPoint = player.position + randomDirection * Random.Range(5f, distance);
        //AÑADIR COMPROBACIÓN DE HIT CON RAYCAST (para que se ponga a la altura del suelo)
        randomPoint.y = player.position.y; // Mantén la altura similar al jugador

        return randomPoint;
    }

    private IEnumerator LandAtPointUnmounted(Vector3 targetPosition)
    {
        RotateTowardsMovement(targetPosition);
        float elapsedTime = 0f;

        Vector3 startPosition = transform.position;
        while (elapsedTime < landUnmountedDuration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / landUnmountedDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition; // Asegura que llegue al punto exacto al final
        transform.eulerAngles = new Vector3(0, transform.rotation.eulerAngles.y, 0); //SETEO rotación al aterrizar

        SetDragonState(DragonStates.Landed); // O cualquier otro estado
        landedCollider.enabled = true;
        dragonRB.isKinematic = true;
    }
    //LANDING MOUNTED
    private void PrepareLandingMounted()
    {
        if (canLand)
        {
            StartCoroutine(LandAtPointWhenMounted(landingPoint));
        }
    }
    private IEnumerator LandAtPointWhenMounted(Vector3 targetPosition)
    {
        SetDragonState(DragonStates.Landing);

        RotateTowardsMovement(targetPosition);

        float elapsedTime = 0f;

        Vector3 startPosition = transform.position;
        while (elapsedTime < landMountedDuration)
        {
            transform.position = Vector3.Slerp(startPosition, targetPosition, elapsedTime / landMountedDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;
        SetDragonState(DragonStates.MountedLanded);
        //Igualo rotación del gameobject padre al interno        
        dragonObj.localRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        transform.rotation = Quaternion.Euler(0, 0, 0);
        landedCollider.enabled = true;
        calledCollider.enabled = false;
        SetDragonCamera();
    }

    //MOUNT FLYING (CATCHED)
    public void MountDragonFlying()
    {
        SetDragonState(DragonStates.Mounted);

        //PHYSICS
        dragonRB.isKinematic = false;

        playerController.MountDragon();
        //DRAGON CAM ON
        SetDragonCamera();

        //DEACTIVATE TRIGGERS
        DeactivateTriggers();

    }

    public void SetDragonCamera()
    {
        if(dragonState == DragonStates.Mounted)
        {
            dragonLandVcam.Priority = 0;
            dragonFlyingVcam.Priority = 10;
        }
        else if(dragonState == DragonStates.MountedLanded)
        {
            dragonFlyingVcam.Priority = 0;
            dragonLandVcam.Priority = 10;
        }
        else //Player cam enabled
        {
            dragonFlyingVcam.Priority = 0;
            dragonLandVcam.Priority = 0;
        }
    }


    public void MountDragonOnLand()
    {
        SetDragonState(DragonStates.MountedLanded);
        playerController.MountDragon();

        //PHYSICS
        dragonRB.useGravity = true;

        //Igualo rotación del gameobject padre al interno        
        dragonObj.localRotation = Quaternion.Euler(0, transform.localEulerAngles.y, 0);
        transform.rotation = Quaternion.Euler(0,0,0);


        //DRAGON CAM ON
        SetDragonCamera();

        //DEACTIVATE TRIGGERS
        DeactivateTriggers();

        isMountable = false;
        dragonRB.isKinematic = false;
    }

    private void TakeOff() //WHEN MOUNTED
    {
        SetDragonState(DragonStates.Mounted);
        
        // Obtener la dirección en la que está mirando la cámara (sin afectar el eje vertical)
        Vector3 cameraForward = Camera.main.transform.forward;

        //cameraForward.y = 0; // Asegurarse de que el dragón no cambie en el eje Y (vertical) // probar más
        Quaternion cameraRotation = Quaternion.LookRotation(cameraForward);

        // Rotar el dragón para que mire en la misma dirección que la cámara
        transform.rotation = cameraRotation;

         
        //Igualo rotación del gameobject padre al interno para que el dragón despegue en la dirección que mira.        
        dragonObj.localRotation = Quaternion.Euler(0, 0, 0);
        SetDragonCamera();
    }

    public void SendTakeOff()
    {
        SetDragonState(DragonController.DragonStates.Free);
        dragonObj.localRotation = Quaternion.Euler(0, 0, 0);
    }



    //DISMOUNT
    void DismountJump()
    {
        switch (dragonState)
        {
            //case DragonStates.Free:
            //    break;
            //case DragonStates.Called:
            //    break;
            case DragonStates.Mounted:
                SetDragonState(DragonStates.Dismounted);
                break;
            //case DragonStates.Dismounted:
            //    break;
            //case DragonStates.Landing:
            //    break;
            //case DragonStates.Landed:
            //    break;
            case DragonStates.MountedLanded:
                //PHYSICS
                dragonRB.isKinematic = true;
                landedCollider.enabled = true;
                SetDragonState(DragonStates.Landed);
                break;
            default:
                break;
        }
        //Igualo rotación del gameobject padre al interno       
        transform.rotation = Quaternion.Euler(0, dragonObj.rotation.eulerAngles.y, 0);
        dragonObj.localRotation = Quaternion.Euler(0, 0, 0);

        StartCoroutine(MountTriggerCoroutine());
        playerController.DismountDragon();
        playerController.Jump(7.5f);

        //Dragon CAM OFF
        SetDragonCamera();
    }
    private IEnumerator MountTriggerCoroutine()
    {
        mountCollider.enabled = false;
        yield return new WaitForSecondsRealtime(0.5f);
        mountCollider.enabled = true;
       yield return null;
    }

    void DismountDragonOnLand()
    {
        SetDragonState(DragonStates.Landed);

        playerController.gameObject.transform.position = dismountTransform.position; // muevo el player a la posición de dismount
        playerController.DismountDragon();
       
        
        //Igualo rotación del gameobject padre al interno       
        transform.rotation = Quaternion.Euler(0, dragonObj.rotation.eulerAngles.y, 0);
        dragonObj.localRotation = Quaternion.Euler(0,0,0);


        //Dragon CAM OFF
        SetDragonCamera();
        landedCollider.enabled = true;
        mountCollider.enabled = true;
        dragonRB.isKinematic = true;
    }

    //SET DRAGON STATE
    public void SetDragonState(DragonStates _dragonState)
    {
        dragonState = _dragonState; // Método para cambiar el estado del dragón
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
        if(mountCollider.enabled)
        {
            mountCollider.enabled = false;
        }
    }
    //TRIGGERS
    private void OnTriggerEnter(Collider other)
    {
        if(other.transform.parent != null)
        {
            if (other.transform.parent.tag == "Player" && dragonState == DragonStates.Called)
            {
                MountDragonFlying();
            }        
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if(other.transform.parent != null)
        {
            if (other.transform.parent.tag == "Player" && dragonState == DragonStates.Landed && !isMountable)
            {
                //CAN MOUNT
                isMountable = true;
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if(other.transform.parent != null)
        {
            if (other.transform.parent.tag == "Player")
            {
                //CAN'T MOUNT
                isMountable = false;
            }
        }
    }

    //UIDEBUG
    

    public bool GetDragonGrounded
    {
        get { return isGrounded; }
    }
    public float GetCurrentFlyingSpeed
    {
        get { return currentDragonSpeed; }
    }
    public string GetDragonAction()
    {
        if(isAccelerating)
        {
            return new string("is Accel"); 
        }
        else if(isBraking)
        {
            return new string("is Braking");
        }
        else
        {
            return new string("is Idle");
        }
    }
    public Vector3 GetCurrentDragonVelocity
    {
        get { return dragonRB.velocity; }
    }

    public Rigidbody GetDragonRB
    {
        get { return dragonRB; }            
    }
}
