using UnityEngine;

public class DragonCameraController : MonoBehaviour
{
    // Drag�n a seguir (asigna esto en el inspector)
    public Transform dragonTransform;

    // Distancia y altura de la c�mara respecto al drag�n
    public float distance = 10.0f;
    public float height = 5.0f;

    // Velocidad de movimiento de la c�mara
    public float followSpeed = 5.0f;

    // Sensibilidad de rotaci�n
    public float rotationDamping = 5.0f;

    void LateUpdate()
    {
        if (!dragonTransform)
        {
            Debug.LogWarning("El transform del drag�n no est� asignado.");
            return;
        }

        // Calculamos la posici�n deseada de la c�mara en relaci�n al drag�n
        Vector3 desiredPosition = dragonTransform.position
                                - dragonTransform.forward * distance // Mueve la c�mara hacia atr�s
                                + dragonTransform.up * height;       // Ajusta la altura de la c�mara

        // Movemos la c�mara suavemente a la posici�n deseada
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        // Alineamos la rotaci�n de la c�mara con la del drag�n
        Quaternion desiredRotation = Quaternion.LookRotation(dragonTransform.forward, dragonTransform.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationDamping * Time.deltaTime);
    }
}
