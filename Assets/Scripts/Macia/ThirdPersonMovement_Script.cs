using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonMovement_Script : MonoBehaviour
{
    [SerializeField] CharacterController controller;
    Rigidbody rb;
    
  
    [SerializeField] float speed = 6;
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundDistance = 0.4f;

    float velocityY;

    bool isGrounded;
    [SerializeField] float jumpForce = 3f;
    float jumpHeight = 7f;
    [SerializeField] LayerMask groundMask;
   
    [SerializeField] float turnSmoothTime = 0.1f;
    float turnSmoothVelocity;
    [SerializeField] Transform cam;
    //[SerializeField] Transform dragonSpawner;
    //[SerializeField] GameObject dragon;
    //[SerializeField] DragonMovement_Script dragonMovement_Script_;
    //ThirdPersonCameraController tpCameraController;
    //DragonCameraController dCameraController;
    Vector2 moveInput;
    [SerializeField] PlayerStates playerState = PlayerStates.Normal;

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
    private void Start()
    {
        controller = gameObject.GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();
    }

    void OnJump()
    {
        if(isGrounded )
        {
            
            Jump();
        }


    }
    void OnMove(InputValue movementValue)
    {
        moveInput = movementValue.Get<Vector2>();
        
    }


    private void Update()
    {
        //JUMP
        isGrounded = Physics.CheckSphere(groundCheck.position,groundDistance, groundMask);

        if (isGrounded && playerState == PlayerStates.Normal)
        {
            Debug.Log("IS GROUNDED");
            velocityY = 0;
        }

        if (playerState != PlayerStates.OnDragon && !isGrounded)
        {

            velocityY += Physics.gravity.y * Time.deltaTime;

            controller.Move(new Vector3(0, velocityY * Time.deltaTime, 0));
           
        }


        if (playerState == PlayerStates.Normal)
        {
            
            //MOVEMENT
            Vector3 direction = new Vector3(moveInput.x, velocityY, moveInput.y); // NORMALIZED para que no vaya más rápido cuando te mueves en diagonal


            if (direction.magnitude >= 0.1f)
            {

                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y; //le sumo la rotación de la cámara para que camine a donde apunta la cam
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime); //para que gire suavemente

                if (playerState != PlayerStates.BigFall)
                {
                    transform.rotation = Quaternion.Euler(0, angle, 0f);
                }

                Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward + new Vector3(0,velocityY,0);

                controller.Move(moveDir.normalized * speed * Time.deltaTime);
            }
            

            if (!isGrounded && (velocityY <= -15f) && playerState != PlayerStates.BigFall && playerState != PlayerStates.OnDragon)
            {
                SetPlayerState(PlayerStates.BigFall);
                Debug.Log("CAIDA!");
                // float angle = Mathf.SmoothDampAngle(transform.eulerAngles.x, 90, ref turnSmoothVelocity, turnSmoothTime);
                transform.GetChild(0).rotation = Quaternion.Euler(90, gameObject.transform.eulerAngles.y, gameObject.transform.eulerAngles.z);
            }

        }

        if (isGrounded && playerState == PlayerStates.BigFall)
        {
            RepositionAfterBigFall();
            Debug.Log("Reposiciono después de caída");
        }
    }

    //private void Update()
    //{


    //    

    //    if(playerState != PlayerStates.OnDragon)
    //    {

    //        if (isGrounded && velocity.y < 0)
    //        {
    //           

    //            if (playerState == PlayerStates.BigFall)
    //            {
    //                //RECOLOCACION DESPUES DE CAIDA
    //                RepositionAfterBigFall();
    //            }
    //        }

    //       

    //        
    //        



    //       

    //        



    //        //CALL DRAGON
    //        if (playerState == PlayerStates.BigFall && Input.GetButtonDown("Jump"))
    //        {
    //            //CALL DRAGON
    //            dragon.transform.position = dragonSpawner.position;
    //            dragon.transform.rotation = dragonSpawner.rotation;
    //            dragonMovement_Script_.CallDragon();

    //        }

    //    }

    //    //ON DRAGON

    //    if(playerState == PlayerStates.OnDragon && Input.GetButtonDown("Jump"))
    //    {
    //        Debug.Log("pulso botón para desmontar");
    //        Jump();
    //        IsNOTonDragon();
    //    }
    //}

    private void Jump()
    {
        Debug.Log("JUMP");
        //controller.SimpleMove(new Vector3(0, Mathf.Sqrt(jumpHeight * -Physics.gravity.y),0));
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        //velocityY += Mathf.Sqrt(jumpHeight * -Physics.gravity.y);
    }

    private void RepositionAfterBigFall()
    {
        transform.GetChild(0).rotation = Quaternion.Euler(0, gameObject.transform.rotation.eulerAngles.y, 0);
        SetPlayerState(PlayerStates.Normal);
    }

    //public void IsOnDragon(GameObject dragon_)
    //{
    //    RepositionAfterBigFall();

    //    SetPlayerState(PlayerStates.OnDragon);
    //    velocity.y = 0;


    //    //CAMBIO CÁMARA
    //    Camera.main.GetComponent<DragonCameraController>().enabled = true;
    //    Camera.main.GetComponent<ThirdPersonCameraController>().enabled = false;


    //    gameObject.GetComponent<Rigidbody>().isKinematic = true;
    //}
    //public void IsNOTonDragon()
    //{
    //    Debug.Log("is not on Dragon");
    //    SetPlayerState(PlayerStates.Normal);
    //    dragonMovement_Script_.DismountDragon(gameObject);
    //    //CAMBIO CÁMARA
    //    Camera.main.GetComponent<DragonCameraController>().enabled = false;
    //    Camera.main.GetComponent<ThirdPersonCameraController>().enabled = true;
    //}

    void SetPlayerState(PlayerStates _state)
    {
        playerState = _state;
    }
}
