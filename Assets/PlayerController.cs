using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] PlayerStates playerState = PlayerStates.Normal;


    Vector2 moveInput;

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
    void OnMove(InputValue movementValue)
    {
        moveInput = movementValue.Get<Vector2>();
        Debug.Log("MoveInput es " + moveInput);
    }

    private void OnJump()
    {
        Debug.Log("Salto");
        //if (isGrounded)
        //{
        //    velocity.y = Mathf.Sqrt(jumpHeight * -1.5f * gravity);//mueve el jugador a la velocidad que necesita para llegar a determinada altura (jumpHeight)
        //}
    }
}
