using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragonController : MonoBehaviour
{
    [SerializeField] private Transform playerTransform; // El jugador al que el dragón sigue.
    [SerializeField] private float circleHeight = 20f; // Altura a la que vuela el dragón.
    [SerializeField] private float circleRadius = 15f; // Radio del círculo que describe el vuelo.
    [SerializeField] private float circleSpeed = 10f; // Velocidad con la que el dragón vuela en círculos.
    [SerializeField] private float pitchAngle = 0f; // Ángulo de alabeo del dragón para simular el giro.
    [SerializeField] private float yawAngle = 0f; // Ángulo de alabeo del dragón para simular el giro.
    [SerializeField] private float rollAngle = 30f; // Ángulo de alabeo del dragón para simular el giro.

    private float currentAngle = 0f; // Ángulo actual en el círculo.
    private Vector3 circleCenter; // Centro del círculo, que será la posición del jugador.
    private Vector3 lastPosition; // Para calcular la dirección del movimiento.


    [SerializeField] DragonStates dragonState;
    public DragonStates GetDragonState
    {
        get { return dragonState; }
        set { dragonState = value; }
    }

    public enum DragonStates
    {
        Free,
        Called,
        Mounted,
        Dismounted
    }
    

    
    private void Start()
    {
        // Iniciar el círculo centrado en la posición del jugador
        circleCenter = playerTransform.position;

        // Guardamos la posición inicial del dragón para calcular la dirección de movimiento
        lastPosition = transform.position;
    }

    private void Update()
    {
        if (playerTransform != null)
        {
            float angle = Time.time * circleSpeed; // Ángulo de rotación
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * circleRadius; // Cálculo del desplazamiento en círculo
            Vector3 targetPosition = playerTransform.position + new Vector3(offset.x, circleHeight, offset.z); // Nueva posición en círculo


            // Mover el dragón a la posición objetivo
            transform.position = targetPosition;           

            // Calculamos la dirección de movimiento basándonos en la diferencia entre la posición actual y la última.
            Vector3 direction = (transform.position - lastPosition).normalized;

            // Rotamos el dragón para que mire hacia la dirección de su movimiento.
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = targetRotation;
            }

            // Aplicamos el ángulo de alabeo (roll) al dragón mientras vuela.
            ApplyRoll();

            // Actualizamos la última posición del dragón.
            lastPosition = transform.position;
        }
    }

    private void ApplyRoll()
    {
        // Aplicamos un ángulo de rotación en el eje Z (roll) para simular el alabeo.
        Quaternion rollRotation = Quaternion.Euler(pitchAngle, yawAngle, rollAngle);
        transform.rotation *= rollRotation;
    }

    public void CallDragon()
    {
        SetDragonState(DragonStates.Called);
    }

    public void SetDragonState(DragonStates _dragonState)
    {
        dragonState = _dragonState; // Método para cambiar el estado del dragón
    }
}
