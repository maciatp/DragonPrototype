using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] PlayerStates playerState = PlayerStates.Normal;
    private Vector2 moveInput;
    private bool isGrounded;
    private bool isDiving = false;

    [SerializeField] private float moveSpeed = 12f;
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
    [SerializeField] CinemachineFreeLook cinemachineFreeLookCamera;

    private Rigidbody rb;

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
        //Dive Debug
        trail = GetComponentInChildren<TrailRenderer>();
        normalColor = trail.startColor;
        trail.enabled = false;
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
        rb.AddForce(Vector3.up * Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y), ForceMode.VelocityChange); //MODO CALCULAR Cu�NTA FUERZA TENGO QUE DARLE PARA QUE LLEGUE A LA ALTURA REQUERIDA       
    }

    private void SetBigFall()
    {
        playerObj.localRotation = Quaternion.Euler(90f, 0f, 0f);
        playerState = PlayerStates.BigFall;
        trail.enabled = true;
    }

    private void BigFallMovement()
    {
        // Rotaci�n gradual hacia la c�mara
        Vector3 viewDir = transform.position - new Vector3(cinemachineFreeLookCamera.transform.position.x, transform.position.y, cinemachineFreeLookCamera.transform.position.z);
        orientation.forward = viewDir.normalized;
        Vector3 targetDirection = orientation.forward;

        // Rotaci�n gradual
        transform.forward = Vector3.Slerp(transform.forward, targetDirection, Time.deltaTime * bigFallRotationSpeed);

        // Movimiento paralelo al suelo
        Vector3 moveDirection = (orientation.right * moveInput.x) + (orientation.forward * moveInput.y);
        rb.MovePosition(rb.position + moveDirection * bigFallMoveSpeed * Time.fixedDeltaTime);
    }

    private void RestorePlayerRotation()
    {
        if (playerObj != null)
        {
            playerObj.localRotation = Quaternion.Euler(0, 0, 0);
            SetPlayerState(PlayerStates.Normal);
        }
        if (trail != null) //DIVE TESTING
        {
            trail.enabled = false;
        }
    }

    private void SetPlayerState(PlayerStates newPlayerState)
    {
        playerState = newPlayerState;
    }
}
