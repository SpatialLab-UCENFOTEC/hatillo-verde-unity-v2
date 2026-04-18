using UnityEngine;

public class InteractableInfo : MonoBehaviour
{
    [Header("UI Content")]
    public string title;

    [TextArea(5, 10)]
    public string description;

    public Sprite displayImage;

    [Header("Optional Multimedia")]
    public string videoURL; // para WebGL 

    public void TriggerPopup()
    {
        // Encuentra el controlador de la UI, incluso si está inactivo
        InfoPanelController uiManager =
            Object.FindAnyObjectByType<InfoPanelController>(FindObjectsInactive.Include);

        if (uiManager != null)
        {
            uiManager.DisplayInfo(this);
        }
    }
}