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
    [SerializeField] private float bigFallRotationSpeed = 5f; // Velocidad de rotaci�n gradual en BigFall
    [SerializeField] private float jumpHeight = 3f;
    [SerializeField] private float fallThreshold = -10f; // Umbral para activar BigFall
    [SerializeField] private float terminalVelocity = -20f;
    [SerializeField] private float terminalDiveVelocity = -50f;
    [SerializeField] private float diveAcceleration = 10f;  // Rapidez para alcanzar terminalDiveVelocity
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private Transform playerObj; // El modelo que rotar� en BigFall. Es hijo del gameobject que lleva este script
    [SerializeField] Transform orientation; //transform que se usa para determinar la orientaci�n. es hijo del gameobject que lleva este script.
    [SerializeField] CinemachineFreeLook freeLookPlayerCamera;
    float playerCameraOriginalPriority;
    private Rigidbody rb;

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

        //Dive Debug
        trail = GetComponentInChildren<TrailRenderer>();
        normalColor = trail.startColor;
        trail.enabled = false;

        if(dragonController == null)
        {
            dragonController = GameObject.FindGameObjectWithTag("Dragon").GetComponent<DragonController>();
        }
    }

    public void OnMove(InputAction.CallbackContext movementContext) //A�ADIDO A PLAYER INPUT MEDIANTE EVENTOS
    {
        moveInput = movementContext.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext jumpContext)
    {
        if (jumpContext.action.triggered) //S�lo se llama una vez, cuando pulsas el bot�n
        {
            if (isGrounded)
            {
                Jump();
            }
        }
    }
    public void OnRun(InputAction.CallbackContext runContext)
    {
        if(runContext.action.IsPressed())
        {
            isRunning = true;
        }
        if(runContext.action.WasReleasedThisFrame())
        {
            isRunning = false;
        }
    }

    public void OnDive(InputAction.CallbackContext diveContext) //s�lo true cuando el bot�n est� pulsado (no hold)
    {
        if (diveContext.action.IsPressed()) //mira si est� presionado
        {
            isDiving = true;
        }
        if (diveContext.action.WasReleasedThisFrame()) //mira si se ha soltado el bot�n
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

    private void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask);

        if (!isGrounded && playerState == PlayerStates.Normal && rb.velocity.y <= fallThreshold)
        {
            SetBigFall();
        }

        if (isGrounded && playerState == PlayerStates.BigFall)
        {
            RestorePlayerRotation();
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
    }

    private void Move()
    {
        if (moveInput.sqrMagnitude > 0.01f)
        {
            //Rotaci�n y movimiento respecto a c�mara
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

    private void Jump()
    {
        rb.AddForce(Vector3.up * Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y), ForceMode.VelocityChange); //MODO CALCULAR Cu�NTA FUERZA TENGO QUE DARLE PARA QUE LLEGUE A LA ALTURA REQUERIDA       
    }


    //BIG FALL
    private void SetBigFall()
    {
        playerObj.localRotation = Quaternion.Euler(90f, 0f, 0f);
        playerState = PlayerStates.BigFall;
        trail.enabled = true;
        rb.useGravity = false;
    }

    private void BigFallMovement()
    {
        // Rotaci�n gradual hacia la c�mara
        Vector3 viewDir = transform.position - new Vector3(freeLookPlayerCamera.transform.position.x, transform.position.y, freeLookPlayerCamera.transform.position.z);
        orientation.forward = viewDir.normalized;
        Vector3 targetDirection = orientation.forward;

        // Rotaci�n gradual
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

    private void RestorePlayerRotation()
    {
        if (playerObj != null)
        {
            playerObj.localRotation = Quaternion.Euler(0, 0, 0);
            SetPlayerState(PlayerStates.Normal);
            rb.useGravity = true;
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
        RestorePlayerRotation();       
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
    }

    private void SetPlayerState(PlayerStates newPlayerState)
    {
        playerState = newPlayerState;
    }
}
