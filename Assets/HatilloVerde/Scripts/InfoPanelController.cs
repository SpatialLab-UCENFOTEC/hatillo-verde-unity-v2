using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Video;

public class InfoPanelController : MonoBehaviour
{
    public static bool IsUIOpen = false;

    [Header("Panel Root")]
    public GameObject infoPanel; // SOLO el popup visual

    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public Image displayImage;
    public RawImage videoDisplay;
    public VideoPlayer videoPlayer;

    void Start()
    {
        infoPanel.SetActive(false);
        IsUIOpen = false;
    }

    public void DisplayInfo(InteractableInfo data)
    {
        // ACTIVAMOS EL PANEL
        infoPanel.SetActive(true);
        IsUIOpen = true;

        titleText.text = data.title;
        descriptionText.text = data.description;

        videoPlayer.Stop();

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
        videoPlayer.Stop();
        infoPanel.SetActive(false);
        IsUIOpen = false;
    }
}