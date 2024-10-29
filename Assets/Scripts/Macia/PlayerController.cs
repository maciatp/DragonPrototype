using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using JetBrains.Annotations;
using UnityEditor;
using Unity.VisualScripting;


public class PlayerController : MonoBehaviour
{
    public float Yspeed;
    [SerializeField] PlayerStates playerState = PlayerStates.Normal;
    private Vector2 moveInput;
    private bool isGrounded;
    private bool isDiving = false;
    private bool isRunning = false;

    [SerializeField] private float moveSpeed = 12f;
    private float runSpeed; // que valga moveSpeed * 1.5;
    [SerializeField] private float bigFallMoveSpeed = 6f; // Velocidad de movimiento en BigFall
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float bigFallRotationSpeed = 5f; // Velocidad de rotación gradual en BigFall
    [SerializeField] private float jumpHeight = 3f;
    [SerializeField] private float fallThreshold = -10f; // Umbral para activar BigFall
    [SerializeField] private float terminalVelocity = -20f;
    [SerializeField] private float terminalDiveVelocity = -50f;
    [SerializeField] private float diveAcceleration = 10f;  // Rapidez para alcanzar terminalDiveVelocity
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private Transform playerObj; // El modelo que rotará en BigFall. Es hijo del gameobject que lleva este script
    [SerializeField] Transform orientation; //transform que se usa para determinar la orientación. es hijo del gameobject que lleva este script.
    [SerializeField] CinemachineFreeLook freeLookPlayerCamera;
    float playerCameraOriginalPriority;
    private Rigidbody playerRb;
    [SerializeField] ParticleSystem particles;

    //Paravela
    [SerializeField] float paravelaMovementSpeed = 6f;
    [SerializeField] float paravelaFallingSpeed = -2f;
    [SerializeField] float paravelaRotationSpeed = 10f;
    float currentParavelaStamina = 0f;
    [SerializeField] float totalParavelaStamina = 2f;

    [SerializeField] GameObject paravelaGO;

    //WINGSUIT
    float wingsuitSpeed;
    [SerializeField] float wingsuitAcceleration = 20f;
    [SerializeField] float wingsuitDeceleration = 8f;
    float minWingsuitSpeed = 5;
    float maxWingsuitSpeed = 65f;
    [SerializeField] float wingsuitTurnSpeed  = 1f;
    [SerializeField] float stableWingsuitSpeed  = 20f;
    [SerializeField] float descentThreshold = -50f;


    [Header("Dragon")]
    DragonController dragonController;

    //INPUT
    PlayerInput playerInput;

    //Debug DIVE
    TrailRenderer trail;
    Color normalColor;
    [SerializeField] Color diveColor;


    public enum PlayerStates
    {
        Normal,
        BigFall,
        OnDragon,
        Paravela,
        Wingsuit
    }
    public PlayerStates GetPlayerState
    {
        get { return playerState; }
    }

    public float GetCurrentStamina
    {
        get { return currentParavelaStamina; }
    }
    public float GetMaxStamina
    {
        get { return totalParavelaStamina; }
    }

    private void Awake()
    {
        playerRb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        //INPUT
        playerInput = GetComponent<PlayerInput>();
        runSpeed = moveSpeed * 1.5f; //ESTABLEZCO RUN SPEED

        //CAMERA
        playerCameraOriginalPriority = freeLookPlayerCamera.Priority;

        if (dragonController == null)
        {
            dragonController = GameObject.FindGameObjectWithTag("Dragon").GetComponent<DragonController>();
        }

        //PARAVELA
        paravelaGO.SetActive(false);
        currentParavelaStamina = totalParavelaStamina;

        //Dive Debug
        trail = GetComponentInChildren<TrailRenderer>();
        normalColor = trail.startColor;
        trail.enabled = false;

    }
    //CONTROLS
    public void OnMove(InputAction.CallbackContext movementContext) //AÑADIDO A PLAYER INPUT MEDIANTE EVENTOS
    {
        if(playerState != PlayerStates.Wingsuit)
        {
            moveInput = movementContext.ReadValue<Vector2>();
        }
    }

    public void OnJump(InputAction.CallbackContext jumpContext)
    {
        if (jumpContext.action.triggered) //Sólo se llama una vez, cuando pulsas el botón
        {
            if (isGrounded && playerState == PlayerStates.Normal)
            {                
                Jump();
            }
        }
    }
    public void OnRun(InputAction.CallbackContext runContext)
    {
        if (runContext.action.IsPressed())
        {
            isRunning = true;
        }
        if (runContext.action.WasReleasedThisFrame())
        {
            isRunning = false;
        }
    }

    public void OnDive(InputAction.CallbackContext diveContext) //sólo true cuando el botón está pulsado (no hold)
    {
        ParticleSystem.TrailModule particleTrail = particles.trails;
        if (diveContext.action.IsPressed()) //mira si está presionado
        {
            isDiving = true;
            particleTrail.ratio = 0.8f;
        }
        if (diveContext.action.WasReleasedThisFrame()) //mira si se ha soltado el botón
        {
            isDiving = false;
            particleTrail.ratio = 0.4f;
        }
    }

    public void OnCallDragon(InputAction.CallbackContext callContext)
    {
        if (callContext.action.triggered )
        {            
            if (playerState == PlayerStates.BigFall || playerState == PlayerStates.Paravela)
            {
                dragonController.CallDragon();
            }
            if(playerState == PlayerStates.Normal && dragonController.IsMountable)
            {             
                dragonController.MountDragonOnLand();
            }
        }
        
    }

    public void OnLandDragon(InputAction.CallbackContext landDragonContext)
    { 
        if (landDragonContext.performed)
        {
            if(playerState == PlayerStates.Normal && dragonController.GetDragonState == DragonController.DragonStates.Free)
            {
                dragonController.CallDragonToLand();
            }
            if(dragonController.GetDragonState == DragonController.DragonStates.Landed)
            {
                dragonController.SendTakeOff();
                
            }
        }
        
    }
    public void OnLookDragon(InputAction.CallbackContext lookDragonContext)
    {
        if (lookDragonContext.action.IsPressed() && playerState == PlayerStates.Normal)
        {
            freeLookPlayerCamera.LookAt = dragonController.transform;
        }
        if (lookDragonContext.action.WasReleasedThisFrame())
        {
            freeLookPlayerCamera.LookAt = transform;
        }
    }
    public void OnParavela(InputAction.CallbackContext paravelaContext)
    {
        //PARAVELA
        if (paravelaContext.action.triggered)
        {           
            switch (playerState)
            {
                case PlayerStates.Normal:
                    if (!isGrounded && currentParavelaStamina > 0)
                    {
                        ParavelaEnable();
                    }
                    break;
                case PlayerStates.BigFall:
                    if (currentParavelaStamina > 0)
                    {
                        ParavelaEnable();
                        if(isDiving)
                        {
                            isDiving = false;
                        }
                    }
                    break;
                case PlayerStates.OnDragon:
                    break;
                case PlayerStates.Paravela:
                    ParavelaDisable();
                    break;
                default:
                    break;
            }

        }
    }

    public void OnPetDragon(InputAction.CallbackContext petDragonContext)
    {
        if(petDragonContext.action.triggered && dragonController.IsPetable && dragonController.IsMountable)
        {
            StartCoroutine(dragonController.PetDragon());
        }
    }

    //WINGSUIT
    public void OnWingsuit(InputAction.CallbackContext wingsuitContext)
    {
        if(wingsuitContext.action.triggered && playerState == PlayerStates.BigFall)
        {
            EnableWingsuit();
        }
    }

    public void OnWingsuitMove(InputAction.CallbackContext wingsuitMoveContext)
    {
        if(playerState == PlayerStates.Wingsuit)
        {
            moveInput = wingsuitMoveContext.ReadValue<Vector2>();
        }
    }

    void EnableWingsuit()
    {
        Debug.Log("Wingsuit");
        SetPlayerState(PlayerStates.Wingsuit);
    }

    //UPDATE
    private void Update()
    {
        if(playerState != PlayerStates.OnDragon)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask);
        }
        else
        {
            isGrounded = false; //para todo lo demás
        }

        //CHARGE STAMINA
        if ((isGrounded && playerState == PlayerStates.Normal) || playerState == PlayerStates.OnDragon)
        {
            if (currentParavelaStamina != totalParavelaStamina)
            {
                ChargeStamina();
            }
        }
        if(isGrounded && playerState == PlayerStates.Normal && playerRb.velocity.y < 0)
        {
            playerRb.velocity = Vector3.zero;
        }
        if (!isGrounded && playerState == PlayerStates.Normal && playerRb.velocity.y <= fallThreshold)
        {
            SetBigFall();
        }

        if (isGrounded && playerState != PlayerStates.Normal) //&& playerState != PlayerStates.OnDragon //probar método de seguridad para volver a grounded
        {
            RestorePlayerNormalState();           
        }

        
        if (playerState == PlayerStates.Paravela)
        {
            currentParavelaStamina -= Time.deltaTime;
            if (currentParavelaStamina < 0)
            {
                ParavelaDisable();
            }
        }        
    }


    private void FixedUpdate()
    {
        Yspeed = playerRb.velocity.y; //DEBUG

     

        

        


        switch (playerState)
        {
            case PlayerStates.Normal:
                Move();
                break;
            case PlayerStates.BigFall:
                if(!isGrounded)
                {  
                    BigFallMovement(); 
                }
                break;
            case PlayerStates.OnDragon:
                break;
            case PlayerStates.Paravela:
                ParavelaMovement();
                break;
            case PlayerStates.Wingsuit:
                WingsuitMovement();
                break;
            default:
                break;
        }
    }

    

    private void Move()
    {
        if (moveInput.sqrMagnitude > 0.01f)
        {
            //Rotación y movimiento respecto a cámara
            Vector3 viewDir = transform.position - new Vector3(freeLookPlayerCamera.transform.position.x, transform.position.y, freeLookPlayerCamera.transform.position.z);
            orientation.forward = viewDir.normalized;
            Vector3 moveDirection = (orientation.right * moveInput.x) + (orientation.forward * moveInput.y);
            if(isGrounded) //sólo se gira y mueve si está grounded
            {
                if (moveDirection != Vector3.zero)
                {
                    playerObj.forward = Vector3.Slerp(playerObj.forward, moveDirection, Time.deltaTime * rotationSpeed);               
                }

                //MovePosition Method
                if(!isRunning)
                {
                    playerRb.MovePosition(playerRb.position + moveDirection * moveSpeed * Time.fixedDeltaTime);               
                }
                else
                {
                    playerRb.MovePosition(playerRb.position + moveDirection * runSpeed * Time.fixedDeltaTime);               
                }
            }
        }
    }

    //WINGSUIT    
    private void WingsuitMovement()
    {
        // Factor de descenso para controlar aceleración/desaceleración
        float descentFactor = Mathf.Clamp01(Vector3.Dot(transform.forward, Vector3.down));

        // Crear la dirección de movimiento en base a la cámara y el input del jugador, con un efecto de inercia
        Vector3 moveDirection = (orientation.right * moveInput.x) + (orientation.up * moveInput.y);
        moveDirection = Vector3.Slerp(transform.forward, moveDirection, Time.fixedDeltaTime * wingsuitTurnSpeed * 0.5f); // Añade inercia al giro
        transform.forward = Vector3.Slerp(transform.forward, moveDirection, Time.fixedDeltaTime * wingsuitTurnSpeed);

        // Aplicar el movimiento con inercia en la velocidad
        transform.position += transform.forward * wingsuitSpeed * Time.fixedDeltaTime;

        // Controlar la velocidad en función del ángulo de descenso
        if (descentFactor < descentThreshold)
        {
            wingsuitSpeed = Mathf.Lerp(wingsuitSpeed, minWingsuitSpeed, Time.fixedDeltaTime * wingsuitDeceleration * 0.5f);
        }
        else if (descentFactor > descentThreshold)
        {
            wingsuitSpeed = Mathf.Lerp(wingsuitSpeed, maxWingsuitSpeed, Time.fixedDeltaTime * wingsuitAcceleration * 0.7f);
        }

        // Ajustar la rotación hacia abajo en bajas velocidades
        if (wingsuitSpeed < minWingsuitSpeed + 1)
        {
            transform.forward = Vector3.Slerp(transform.forward, Vector3.down, Time.fixedDeltaTime * wingsuitTurnSpeed * 0.5f);
        }

        wingsuitSpeed = Mathf.Clamp(wingsuitSpeed, minWingsuitSpeed, maxWingsuitSpeed);
    }



    //JUMP
    public void Jump()
    {
        // Obtener la dirección en la que mira la cámara, sin afectar el eje Y (plano horizontal)
        Vector3 jumpDirection = (Camera.main.transform.right * moveInput.x) + (Camera.main.transform.forward * moveInput.y);
        jumpDirection.y = 0; // Asegurarse de que la dirección sea sólo en el plano XZ
        

        // Aplicar la fuerza hacia arriba
        playerRb.AddForce(jumpDirection * moveSpeed + Vector3.up * Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y), ForceMode.VelocityChange);

        

    }
    public void Jump(float forwardForce)
    {
        // Obtener la dirección en la que mira la cámara, sin afectar el eje Y (plano horizontal)
        Vector3 jumpDirection = playerObj.transform.forward;
        


        // Aplicar la fuerza hacia arriba y hacia adelante
        playerRb.AddForce(jumpDirection * forwardForce + Vector3.up * Mathf.Sqrt(jumpHeight*1.5f * -2f * Physics.gravity.y), ForceMode.VelocityChange);

       
    }


    //BIG FALL
    private void SetBigFall()
    {
       
        transform.rotation = Quaternion.Euler(0, playerObj.transform.eulerAngles.y, 0); //igualo la rotación del Player Padre a PlayerObj para el salto
        playerObj.localRotation = Quaternion.Euler(90f, 0f, 0f); //seteo rotación del playerObj a paralelo al suelo. TO DO: hacer que sea gradual. borrar cuando gire bien
        
        SetPlayerState(PlayerStates.BigFall);
        playerRb.useGravity = false;

        //PARTICLES  TRAILS ON
        particles.gameObject.SetActive(true);

        //DIVE DEBUG
        trail.enabled = true;
    }

    private void BigFallMovement()
    {
        
        //Rotación para ponerse horizontal al suelo // WIP
        //playerObj.localRotation = Quaternion.Slerp(playerObj.rotation, Quaternion.Euler(90,0,0),  rotationSpeed);

        // Rotación gradual hacia la cámara
        Vector3 viewDir = transform.position - new Vector3(freeLookPlayerCamera.transform.position.x, transform.position.y, freeLookPlayerCamera.transform.position.z);
        orientation.forward = viewDir.normalized;
        Vector3 targetDirection = orientation.forward;

        // Rotación gradual
        transform.forward = Vector3.Slerp(transform.forward, targetDirection, Time.fixedDeltaTime * bigFallRotationSpeed);

        // Movimiento paralelo al suelo
        Vector3 moveDirection = (orientation.right * moveInput.x) + (orientation.forward * moveInput.y);
        playerRb.MovePosition(playerRb.position + moveDirection * bigFallMoveSpeed * Time.fixedDeltaTime);


        //FALL VELOCITY
        float targetVelocity = isDiving ? terminalDiveVelocity : terminalVelocity;
        trail.startColor = isDiving ? diveColor : normalColor;
        float currentYVelocity = playerRb.velocity.y;

        if (currentYVelocity != targetVelocity)
        {
            float newFallSpeed = Mathf.MoveTowards(currentYVelocity, targetVelocity, diveAcceleration * Time.fixedDeltaTime);
            playerRb.velocity = new Vector3(playerRb.velocity.x, newFallSpeed, playerRb.velocity.z);
        }
    }
    
    // Return to NORMAL STATE -> dismount, grounded, ParavelaDisable
    private void RestorePlayerNormalState()
    { 
        if (playerObj != null && playerState != PlayerStates.Paravela)
        {
            playerObj.transform.localRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);  // Devuelvo la rotación de Y de Player a PlayerObj    
            transform.rotation = Quaternion.identity;
            SetPlayerState(PlayerStates.Normal);
            playerRb.useGravity = true;
        }

        if(paravelaGO != null && paravelaGO.activeSelf)
        {
            ParavelaDisable();
        }

        if (trail != null) //DIVE TESTING
        {
            trail.enabled = false;
        }

        //PARTICLES  TRAILS OFF
        particles.gameObject.SetActive(false);
    }

    //DRAGON
    public void MountDragon()
    {
        SetPlayerState(PlayerStates.OnDragon); //SET PLAYER STATE
        //Paravela Disable
        if(paravelaGO.gameObject.activeSelf)
        {
            ParavelaDisable();
        }

        //MOVE POSITION PLAYER ON DRAGON
        Transform playerPosOnDragon = dragonController.GetPlayerPos;
        transform.SetParent(playerPosOnDragon);
        transform.localPosition = Vector3.zero;
        playerObj.localRotation = Quaternion.identity;
        transform.localRotation = playerPosOnDragon.localRotation;

        //PHYSICS and colliders
        playerRb.isKinematic = true;
        playerRb.useGravity = false;
        CapsuleCollider playerCollider = GetComponentInChildren<CapsuleCollider>();       
        playerCollider.enabled = false;
        
        //INPUT MAP CHANGE
        playerInput.SwitchCurrentActionMap("Dragon");

        //CAMERA CHANGE
        freeLookPlayerCamera.Priority = 0;
        dragonController.SetDragonCamera();

        //PARTICLES OFF
        particles.gameObject.SetActive(false);

        //DIVE TESTING
        trail.enabled = false;
    }

    public void DismountDragon()
    {
        //RESTORE PLAYER ROTATION and set parent null
        transform.SetParent(null); //va Primero para que el player se salga de la posición de Mounted
        RestorePlayerNormalState();       

        //PHYSICS and colliders
        playerRb.isKinematic = false;
        playerRb.useGravity = true;
        CapsuleCollider playerCollider = GetComponentInChildren<CapsuleCollider>();
        playerCollider.enabled = true;

        //Input map change
        playerInput.SwitchCurrentActionMap("Foot");

        //Camera
        //priority returns to original
        freeLookPlayerCamera.Priority = (int)playerCameraOriginalPriority;
        dragonController.SetDragonCamera();


       
    }

    //PARAVELA
    private void ParavelaEnable()
    {
        
        if(playerState != PlayerStates.BigFall) //por el tema de cómo funciona la orientación del player
        {
            //ROTATION
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, playerObj.transform.localEulerAngles.y, transform.rotation.eulerAngles.z);        
            playerObj.localRotation = Quaternion.identity;
        }

        //PARTICLES  TRAILS OFF
        particles.gameObject.SetActive(false);

        SetPlayerState(PlayerStates.Paravela);
        playerRb.useGravity = true; // para que caiga
        playerRb.velocity = Vector3.zero;

        paravelaGO.SetActive(true);
        if(trail.enabled)
        {
            trail.enabled = false;
        }
        //DEBUG
       // rb.isKinematic = true;

    }

    private void ParavelaDisable()
    {
        if(playerState != PlayerStates.OnDragon)
        {
            SetPlayerState(PlayerStates.Normal);
            playerRb.useGravity = true;
        }
        paravelaGO.SetActive(false);


        playerObj.transform.localRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);  // Devuelvo la rotación de Y de Player a PlayerObj    
        transform.rotation = Quaternion.identity;
        

        //DEBUG
        //rb.isKinematic = false;
    }

    private void ChargeStamina() //WHEN GROUNDED
    {
        currentParavelaStamina += (Time.deltaTime*1.75f);
        if (currentParavelaStamina > totalParavelaStamina)
        {
            currentParavelaStamina = totalParavelaStamina;
        }
    }
    private void ParavelaMovement()
    {
        // Rotación para ponerse derecho
        playerObj.forward = Vector3.Slerp(playerObj.forward, transform.forward, Time.fixedDeltaTime * rotationSpeed);

        // Dirección del joystick con respecto a la orientación de la cámara
        Vector3 viewDir = transform.position - new Vector3(freeLookPlayerCamera.transform.position.x, transform.position.y, freeLookPlayerCamera.transform.position.z);
        orientation.forward = viewDir.normalized;
        Vector3 moveDirection = (orientation.right * moveInput.x) + (orientation.forward * moveInput.y);

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            // Rotación gradual hacia la dirección de movimiento
            transform.forward = Vector3.Slerp(transform.forward, moveDirection, Time.fixedDeltaTime * paravelaRotationSpeed);
        }

        // Movimiento paralelo al suelo en la dirección del joystick
        playerRb.MovePosition(playerRb.position + moveDirection * paravelaMovementSpeed * Time.fixedDeltaTime);

        // Limitar la velocidad de caída para simular que está usando la paravela
        playerRb.velocity = new Vector3(playerRb.velocity.x, Mathf.Max(playerRb.velocity.y, paravelaFallingSpeed), playerRb.velocity.z); // Limita la caída para que no baje rápido
    }



    private void SetPlayerState(PlayerStates newPlayerState)
    {
        playerState = newPlayerState; 
    }

    //UI DEBUG

    public bool IsPlayerGrounded
    {
        get { return isGrounded; }
    }
    public Vector3 GetPlayerVelocity
    {
        get { return playerRb.velocity; }
    }
    public Rigidbody GetPlayerRB
    {
        get { return playerRb; }
    }

  
}
