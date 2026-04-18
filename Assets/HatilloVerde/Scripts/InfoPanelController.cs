using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Video;

public class InfoPanelController : MonoBehaviour
{
    // Estado global para bloquear interaccion 3D
    public static bool IsUIOpen = false;

    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;

    public Image displayImage;
    public RawImage videoDisplay;

    public VideoPlayer videoPlayer;

    public void DisplayInfo(InteractableInfo data)
    {
        IsUIOpen = true; //  Bloquea interaccion 3D

        titleText.text = data.title;
        descriptionText.text = data.description;

        videoPlayer.Stop();

        if (!string.IsNullOrEmpty(data.videoURL) && videoPlayer != null)
        {
            videoDisplay.gameObject.SetActive(true);
            displayImage.gameObject.SetActive(false);

            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = data.videoURL;

            // Evita que el evento se acumule múltiples veces
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.prepareCompleted += OnVideoPrepared;

            videoPlayer.Prepare();
        }
        else
        {
            videoDisplay.gameObject.SetActive(false);
            displayImage.gameObject.SetActive(true);
            displayImage.sprite = data.displayImage;
        }

        gameObject.SetActive(true);
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        vp.Play();
    }

    public void ClosePanel()
    {
        videoPlayer.Stop();
        IsUIOpen = false; // Reactiva interaccion 3D
        gameObject.SetActive(false);
    }
}