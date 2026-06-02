using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Video;

public class InfoPanelController : MonoBehaviour
{
    public static bool IsUIOpen = false;

    [Header("Panel Root")]
    public GameObject infoPanel;

    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public Image displayImage;
    public RawImage videoDisplay;
    public VideoPlayer videoPlayer;

    [Header("Audio")]
    public AudioSource audioSource;

    [Header("UI")]
    public GameObject closeButton;

    void Start()
    {
        infoPanel.SetActive(false);
        closeButton.SetActive(false);

        if (videoDisplay != null)
            videoDisplay.gameObject.SetActive(false);

        IsUIOpen = false;
    }

    public void DisplayInfo(InteractableInfo data)
    {
        // ACTIVAMOS EL PANEL
        infoPanel.SetActive(true);
        closeButton.SetActive(true);
        IsUIOpen = true;

        titleText.text = data.title;
        descriptionText.text = data.description;

        // Detener video anterior
        if (videoPlayer != null)
            videoPlayer.Stop();

        // Detener audio anterior
        if (audioSource != null)
            audioSource.Stop();

        // Reproducir audio del objeto
        if (audioSource != null && data.audioClip != null)
        {
            audioSource.clip = data.audioClip;
            audioSource.Play();
        }

        // Mostrar video si existe
        if (!string.IsNullOrEmpty(data.videoURL) && videoPlayer != null)
        {
            videoDisplay.gameObject.SetActive(true);
            displayImage.gameObject.SetActive(false);

            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = data.videoURL;

            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.prepareCompleted += OnVideoPrepared;

            videoPlayer.Prepare();
        }
        else
        {
            videoDisplay.gameObject.SetActive(false);
            displayImage.gameObject.SetActive(true);

            if (data.displayImage != null)
                displayImage.sprite = data.displayImage;
        }
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        videoDisplay.texture = vp.targetTexture;
        vp.Play();
    }

    public void ClosePanel()
    {
        if (videoPlayer != null)
            videoPlayer.Stop();

        if (audioSource != null)
            audioSource.Stop();

        infoPanel.SetActive(false);
        closeButton.SetActive(false);
        IsUIOpen = false;
    }
}