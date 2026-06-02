using UnityEngine;

public class InteractableInfo : MonoBehaviour
{
    private bool hasBeenDiscovered = false;

    [Header("UI Content")]
    public string title;

    [TextArea(5, 10)]
    public string description;

    public Sprite displayImage;

    [Header("Optional Multimedia")]
    public string videoURL; // para WebGL

    [Header("Audio")]
    public AudioClip audioClip;

    public void TriggerPopup()
    {
        // Solo contar objetos con tag "Discoverable"
        if (!hasBeenDiscovered && CompareTag("Discoverable"))
        {
            hasBeenDiscovered = true;

            if (DiscoveryManager.Instance != null)
            {
                DiscoveryManager.Instance.RegisterDiscovery();
            }
        }

        // Mostrar popup
        InfoPanelController uiManager =
            Object.FindAnyObjectByType<InfoPanelController>(FindObjectsInactive.Include);

        if (uiManager != null)
        {
            uiManager.DisplayInfo(this);
        }
    }
}