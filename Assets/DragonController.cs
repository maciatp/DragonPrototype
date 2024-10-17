using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragonController : MonoBehaviour
{
    [SerializeField] private Transform playerTransform; // El jugador al que el drag�n sigue.
    [SerializeField] private float circleHeight = 20f; // Altura a la que vuela el drag�n.
    [SerializeField] private float circleRadius = 15f; // Radio del c�rculo que describe el vuelo.
    [SerializeField] private float circleSpeed = 10f; // Velocidad con la que el drag�n vuela en c�rculos.
    [SerializeField] private float pitchAngle = 0f; // �ngulo de alabeo del drag�n para simular el giro.
    [SerializeField] private float yawAngle = 0f; // �ngulo de alabeo del drag�n para simular el giro.
    [SerializeField] private float rollAngle = 30f; // �ngulo de alabeo del drag�n para simular el giro.

    private float currentAngle = 0f; // �ngulo actual en el c�rculo.
    private Vector3 circleCenter; // Centro del c�rculo, que ser� la posici�n del jugador.
    private Vector3 lastPosition; // Para calcular la direcci�n del movimiento.


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
        // Iniciar el c�rculo centrado en la posici�n del jugador
        circleCenter = playerTransform.position;

        // Guardamos la posici�n inicial del drag�n para calcular la direcci�n de movimiento
        lastPosition = transform.position;
    }

    private void Update()
    {
        if (playerTransform != null)
        {
            float angle = Time.time * circleSpeed; // �ngulo de rotaci�n
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * circleRadius; // C�lculo del desplazamiento en c�rculo
            Vector3 targetPosition = playerTransform.position + new Vector3(offset.x, circleHeight, offset.z); // Nueva posici�n en c�rculo


            // Mover el drag�n a la posici�n objetivo
            transform.position = targetPosition;           

            // Calculamos la direcci�n de movimiento bas�ndonos en la diferencia entre la posici�n actual y la �ltima.
            Vector3 direction = (transform.position - lastPosition).normalized;

            // Rotamos el drag�n para que mire hacia la direcci�n de su movimiento.
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = targetRotation;
            }

            // Aplicamos el �ngulo de alabeo (roll) al drag�n mientras vuela.
            ApplyRoll();

            // Actualizamos la �ltima posici�n del drag�n.
            lastPosition = transform.position;
        }
    }

    private void ApplyRoll()
    {
        // Aplicamos un �ngulo de rotaci�n en el eje Z (roll) para simular el alabeo.
        Quaternion rollRotation = Quaternion.Euler(pitchAngle, yawAngle, rollAngle);
        transform.rotation *= rollRotation;
    }

    public void CallDragon()
    {
        SetDragonState(DragonStates.Called);
    }

    public void SetDragonState(DragonStates _dragonState)
    {
        dragonState = _dragonState; // M�todo para cambiar el estado del drag�n
    }
}
