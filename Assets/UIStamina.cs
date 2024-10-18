using Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class UIStamina : MonoBehaviour
{
    public Transform playerTransform; // Transform del jugador a seguir
    public Vector2 offset; // Offset en relación al jugador
    public RectTransform uiElement; // Elemento UI que seguirá al personaje

    private Camera mainCamera; // Cámara principal, que será la cámara de Cinemachine
    CinemachineFreeLook cinemachineCamera;

    [SerializeField] Image staminaRing;
    [SerializeField] PlayerController playerController;
    private void Start()
    {
        // Obtener la referencia de la cámara de Cinemachine (que en tu caso es la cámara principal)
        mainCamera = Camera.main;

        //playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>(); no encuentra al player por tener un hijo con el mismo tag
       
    }

    private void Update()
    {
        if (playerTransform == null || mainCamera == null)
        {
            return; // No hacemos nada si no hay personaje o cámara
        }

        //ACTIVAR Y DESACTIVAR CUANDO CONSUMA/RECUPERE
        if(playerController.GetCurrentStamina != playerController.GetMaxStamina)
        {
            if(!uiElement.GetChild(0).gameObject.activeInHierarchy)
            {
                uiElement.GetChild(0).gameObject.SetActive(true);
            }
            //WORLD TO VIEWPORT transforma una posición del mundo a un rango de (0,0) a (1,1) 
            Vector2 pos = Camera.main.WorldToViewportPoint(playerTransform.position);
            //VIEWPORT TO SCREEN POINT transforma de (0,0) (1,1) a píxeles
            uiElement.transform.position = Camera.main.ViewportToScreenPoint(pos + offset);

            staminaRing.fillAmount = playerController.GetCurrentStamina / playerController.GetMaxStamina;
        }
        else
        {
            uiElement.GetChild(0).gameObject.SetActive(false);
        }



    }
}
