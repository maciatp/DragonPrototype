using Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class UIStamina : MonoBehaviour
{
    public Transform playerTransform; // Transform del jugador a seguir
    public Vector2 offset; // Offset en relaci�n al jugador
    public RectTransform uiElement; // Elemento UI que seguir� al personaje

    private Camera mainCamera; // C�mara principal, que ser� la c�mara de Cinemachine
    CinemachineFreeLook cinemachineCamera;

    [SerializeField] Image staminaRing;
    [SerializeField] PlayerController playerController;
    private void Start()
    {
        // Obtener la referencia de la c�mara de Cinemachine (que en tu caso es la c�mara principal)
        mainCamera = Camera.main;

        //playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>(); no encuentra al player por tener un hijo con el mismo tag
       
    }

    private void Update()
    {
        if (playerTransform == null || mainCamera == null)
        {
            return; // No hacemos nada si no hay personaje o c�mara
        }

        //ACTIVAR Y DESACTIVAR CUANDO CONSUMA/RECUPERE
        if(playerController.GetCurrentStamina != playerController.GetMaxStamina)
        {
            if(!uiElement.GetChild(0).gameObject.activeInHierarchy)
            {
                uiElement.GetChild(0).gameObject.SetActive(true);
            }
            //WORLD TO VIEWPORT transforma una posici�n del mundo a un rango de (0,0) a (1,1) 
            Vector2 pos = Camera.main.WorldToViewportPoint(playerTransform.position);
            //VIEWPORT TO SCREEN POINT transforma de (0,0) (1,1) a p�xeles
            uiElement.transform.position = Camera.main.ViewportToScreenPoint(pos + offset);

            staminaRing.fillAmount = playerController.GetCurrentStamina / playerController.GetMaxStamina;
        }
        else
        {
            uiElement.GetChild(0).gameObject.SetActive(false);
        }



    }
}
