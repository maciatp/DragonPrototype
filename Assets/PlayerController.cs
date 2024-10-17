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

    [SerializeField] private float moveSpeed = 12f;
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
    [SerializeField] CinemachineFreeLook cinemachineFreeLookCamera;

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
        //Dive Debug
        trail = GetComponentInChildren<TrailRenderer>();
        normalColor = trail.startColor;
        trail.enabled = false;

        if(dragonController == null)
        {
            dragonController = GameObject.FindGameObjectWithTag("Dragon").GetComponent<DragonController>();
        }
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
            //Rotación y movimiento respecto a cámara
            Vector3 viewDir = transform.position - new Vector3(cinemachineFreeLookCamera.transform.position.x, transform.position.y, cinemachineFreeLookCamera.transform.position.z);
            orientation.forward = viewDir.normalized;
            Vector3 moveDirection = (orientation.right * moveInput.x) + (orientation.forward * moveInput.y);
            if (moveDirection != Vector3.zero)
            {
                playerObj.forward = Vector3.Slerp(playerObj.forward, moveDirection.normalized, Time.deltaTime * rotationSpeed);
            }

            //MovePosition Method
            rb.MovePosition(rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime);
        }
    }

    private void Jump()
    {
        rb.AddForce(Vector3.up * Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y), ForceMode.VelocityChange); //MODO CALCULAR CuÁNTA FUERZA TENGO QUE DARLE PARA QUE LLEGUE A LA ALTURA REQUERIDA       
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
        // Rotación gradual hacia la cámara
        Vector3 viewDir = transform.position - new Vector3(cinemachineFreeLookCamera.transform.position.x, transform.position.y, cinemachineFreeLookCamera.transform.position.z);
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
        Debug.Log("Target Velocity es " + targetVelocity);

        if (currentYVelocity != targetVelocity)
        {
            float newFallSpeed = Mathf.MoveTowards(currentYVelocity, targetVelocity, diveAcceleration * Time.fixedDeltaTime);
            rb.velocity = new Vector3(rb.velocity.x, newFallSpeed, rb.velocity.z);
           // Debug.Log("RB VELOCITY Y ES " + rb.velocity.y);
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
        SetPlayerState(PlayerStates.OnDragon);
        trail.enabled = false;
        Transform playerPosOnDragon = dragonController.GetPlayerPos;
        transform.SetParent(playerPosOnDragon);
        transform.localPosition = Vector3.zero;
        playerObj.localRotation = Quaternion.identity;
        transform.localRotation = playerPosOnDragon.localRotation;
        rb.isKinematic = true;        
        CapsuleCollider playerCollider = GetComponentInChildren<CapsuleCollider>();       
        playerCollider.enabled = false;
        playerInput.SwitchCurrentActionMap("Dragon");
        //cinemachineFreeLookCamera.GetComponent<CinemachineInputProvider>().XYAxis = playerInput.actions.("Dragon/Camera");
        
       

        // quizá con los eventos no hace falta mover el componente al dragón?? Sólo referenciarlos desde ahí.
    }

    public void DismountDragon()
    {
        RestorePlayerRotation();
        Debug.Log("Set Player for DISmount");
        transform.SetParent(null);
        rb.isKinematic = false;
        CapsuleCollider playerCollider = GetComponentInChildren<CapsuleCollider>();
        playerCollider.enabled = true;


        playerInput.SwitchCurrentActionMap("Foot");
    }

    private void SetPlayerState(PlayerStates newPlayerState)
    {
        playerState = newPlayerState;
    }
}
