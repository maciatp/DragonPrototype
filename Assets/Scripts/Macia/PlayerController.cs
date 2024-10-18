using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using JetBrains.Annotations;

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
    private Rigidbody rb;


    //Paravela
    float currentParavelaStamina = 0f;
    [SerializeField] float totalParavelaStamina = 2f;
    [SerializeField] GameObject paravelaGO;

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
        Paravela
    }
    public PlayerStates GetPlayerState
    {
        get { return playerState; }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
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

        //Dive Debug
        trail = GetComponentInChildren<TrailRenderer>();
        normalColor = trail.startColor;
        trail.enabled = false;

    }

    public void OnMove(InputAction.CallbackContext movementContext) //AÑADIDO A PLAYER INPUT MEDIANTE EVENTOS
    {
        moveInput = movementContext.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext jumpContext)
    {
        if (jumpContext.action.triggered) //Sólo se llama una vez, cuando pulsas el botón
        {
            if (isGrounded)
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
        if (diveContext.action.IsPressed()) //mira si está presionado
        {
            isDiving = true;
        }
        if (diveContext.action.WasReleasedThisFrame()) //mira si se ha soltado el botón
        {
            isDiving = false;
        }
    }

    public void OnCallDragon(InputAction.CallbackContext callContext)
    {
        if (callContext.action.triggered && playerState == PlayerStates.BigFall)
        {
            dragonController.CallDragon();
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


    private void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask);

        if (isGrounded && playerState == PlayerStates.Normal)
        {
            if (currentParavelaStamina != totalParavelaStamina)
            {
                ChargeStamina();
            }
        }

        if (!isGrounded && playerState == PlayerStates.Normal && rb.velocity.y <= fallThreshold)
        {
            SetBigFall();
        }

        if (isGrounded && playerState != PlayerStates.Normal)
        {
            RestorePlayerNormalState();           
        }

        
        if (playerState == PlayerStates.Paravela)
        {
            Debug.Log("Consumiendo Stamina Paravela");
            currentParavelaStamina -= Time.deltaTime;
            if (currentParavelaStamina < 0)
            {
                ParavelaDisable();
            }
        }
    }


    private void FixedUpdate()
    {
        Yspeed = rb.velocity.y;
        if (playerState == PlayerStates.Normal)
        {
            Move();
        }

        if (!isGrounded && playerState == PlayerStates.BigFall)
        {
            BigFallMovement();
        }

        if (playerState == PlayerStates.Paravela)
        {
            //PARAVELA MOVEMENT
            ParavelaMovement();
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
            if (moveDirection != Vector3.zero)
            {
                playerObj.forward = Vector3.Slerp(playerObj.forward, moveDirection.normalized, Time.deltaTime * rotationSpeed);
            }

            //MovePosition Method
            if(!isRunning)
            {
                rb.MovePosition(rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime);               
            }
            else
            {
                rb.MovePosition(rb.position + moveDirection * runSpeed * Time.fixedDeltaTime);               
            }
        }
    }

    //JUMP
    private void Jump()
    {
        rb.AddForce(Vector3.up * Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y), ForceMode.VelocityChange); //MODO CALCULAR CuÁNTA FUERZA TENGO QUE DARLE PARA QUE LLEGUE A LA ALTURA REQUERIDA       
    }


    //BIG FALL
    private void SetBigFall()
    {
        playerObj.localRotation = Quaternion.Euler(90f, 0f, 0f);
        SetPlayerState(PlayerStates.BigFall);
        trail.enabled = true;
        rb.useGravity = false;
    }

    private void BigFallMovement()
    {
        // Rotación gradual hacia la cámara
        Vector3 viewDir = transform.position - new Vector3(freeLookPlayerCamera.transform.position.x, transform.position.y, freeLookPlayerCamera.transform.position.z);
        orientation.forward = viewDir.normalized;
        Vector3 targetDirection = orientation.forward;

        // Rotación gradual
        transform.forward = Vector3.Slerp(transform.forward, targetDirection, Time.deltaTime * bigFallRotationSpeed);

        // Movimiento paralelo al suelo
        Vector3 moveDirection = (orientation.right * moveInput.x) + (orientation.forward * moveInput.y);
        rb.MovePosition(rb.position + moveDirection * bigFallMoveSpeed * Time.fixedDeltaTime);


        //FALL VELOCITY
        float targetVelocity = isDiving ? terminalDiveVelocity : terminalVelocity;
        trail.startColor = isDiving ? diveColor : normalColor;
        float currentYVelocity = rb.velocity.y;        

        if (currentYVelocity != targetVelocity)
        {
            float newFallSpeed = Mathf.MoveTowards(currentYVelocity, targetVelocity, diveAcceleration * Time.fixedDeltaTime);
            rb.velocity = new Vector3(rb.velocity.x, newFallSpeed, rb.velocity.z);
        }
    }
    
    // Return to NORMAL STATE -> dismount, grounded, ParavelaDisable
    private void RestorePlayerNormalState()
    {
        if (playerObj != null)
        {
            playerObj.localRotation = Quaternion.Euler(0, 0, 0);
            SetPlayerState(PlayerStates.Normal);
            rb.useGravity = true;
        }

        if(paravelaGO != null && paravelaGO.activeSelf)
        {
            ParavelaDisable();
        }

        if (trail != null) //DIVE TESTING
        {
            trail.enabled = false;
        }
    }

    //DRAGON
    public void MountDragon()
    {
        SetPlayerState(PlayerStates.OnDragon); //SET PLAYER STATE

        //MOVE POSITION PLAYER ON DRAGON
        Transform playerPosOnDragon = dragonController.GetPlayerPos;
        transform.SetParent(playerPosOnDragon);
        transform.localPosition = Vector3.zero;
        playerObj.localRotation = Quaternion.identity;
        transform.localRotation = playerPosOnDragon.localRotation;

        //PHYSICS and colliders
        rb.isKinematic = true;        
        CapsuleCollider playerCollider = GetComponentInChildren<CapsuleCollider>();       
        playerCollider.enabled = false;
        
        //INPUT MAP CHANGE
        playerInput.SwitchCurrentActionMap("Dragon");

        //CAMERA CHANGE
        freeLookPlayerCamera.Priority = 0;
        
        //DIVE TESTING
        trail.enabled = false;
    }

    public void DismountDragon()
    {
        //RESTORE PLAYER ROTATION and set parent null
        RestorePlayerNormalState();       
        transform.SetParent(null);

        //PHYSICS and colliders
        rb.isKinematic = false;
        CapsuleCollider playerCollider = GetComponentInChildren<CapsuleCollider>();
        playerCollider.enabled = true;

        //Input map change
        playerInput.SwitchCurrentActionMap("Foot");

        //Camera
        //priority returns to original
        freeLookPlayerCamera.Priority = (int)playerCameraOriginalPriority;


        //APPLY JUMP
        Jump();
    }

    //PARAVELA
    private void ParavelaEnable()
    {
        Debug.Log("Playerstate == Paravela");
        SetPlayerState(PlayerStates.Paravela);
        paravelaGO.SetActive(true);
        //DEBUG
       // rb.isKinematic = true;

    }

    private void ParavelaDisable()
    {
        SetPlayerState(PlayerStates.Normal);
        paravelaGO.SetActive(false);
        Debug.Log("Paravela desactivada");
        //DEBUG
        //rb.isKinematic = false;
    }

    private void ChargeStamina() //WHEN GROUNDED
    {
        currentParavelaStamina += Time.deltaTime;
        if (currentParavelaStamina > totalParavelaStamina)
        {
            currentParavelaStamina = totalParavelaStamina;
        }
    }
    private void ParavelaMovement()
    {
        //PARAVELA MOVEMENT HERE
    }

    private void SetPlayerState(PlayerStates newPlayerState)
    {
        playerState = newPlayerState;
    }
}
