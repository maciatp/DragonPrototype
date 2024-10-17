using UnityEngine;

public class DragonCameraController : MonoBehaviour
{
    // Dragón a seguir (asigna esto en el inspector)
    public Transform dragonTransform;

    // Distancia y altura de la cámara respecto al dragón
    public float distance = 10.0f;
    public float height = 5.0f;

    // Velocidad de movimiento de la cámara
    public float followSpeed = 5.0f;

    // Sensibilidad de rotación
    public float rotationDamping = 5.0f;

    void LateUpdate()
    {
        if (!dragonTransform)
        {
            Debug.LogWarning("El transform del dragón no está asignado.");
            return;
        }

        // Calculamos la posición deseada de la cámara en relación al dragón
        Vector3 desiredPosition = dragonTransform.position
                                - dragonTransform.forward * distance // Mueve la cámara hacia atrás
                                + dragonTransform.up * height;       // Ajusta la altura de la cámara

        // Movemos la cámara suavemente a la posición deseada
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        // Alineamos la rotación de la cámara con la del dragón
        Quaternion desiredRotation = Quaternion.LookRotation(dragonTransform.forward, dragonTransform.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationDamping * Time.deltaTime);
    }
}
