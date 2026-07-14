using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;
using UnityEngine.Networking;

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

    [Tooltip("ScrollRect de la descripción; se reinicia arriba cada vez que se abre el modal.")]
    public ScrollRect descriptionScroll;

    private Coroutine currentAudioCoroutine;

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
        // Activar panel
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

        // Cancelar descarga anterior si existe
        if (currentAudioCoroutine != null)
            StopCoroutine(currentAudioCoroutine);

        // Reproducir audio desde URL
        if (audioSource != null && !string.IsNullOrEmpty(data.audioURL))
        {
            currentAudioCoroutine = StartCoroutine(LoadAudio(data.audioURL));
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

        ResetDescriptionScroll();
        StartCoroutine(ResetDescriptionScrollNextFrame());
    }

    private void ResetDescriptionScroll()
    {
        if (descriptionScroll == null) return;

        descriptionScroll.velocity = Vector2.zero;

        // El alto del contenido depende del texto recién asignado, así que hay
        // que rehacer el layout antes de fijar la posición del scroll.
        Canvas.ForceUpdateCanvases();

        if (descriptionScroll.content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(descriptionScroll.content);

        descriptionScroll.verticalNormalizedPosition = 1f;
    }

    // TextMeshPro puede recalcular su tamaño un frame después; repetir aquí evita
    // que el scroll quede a media altura cuando eso ocurre.
    private IEnumerator ResetDescriptionScrollNextFrame()
    {
        yield return null;
        ResetDescriptionScroll();
    }

    private IEnumerator LoadAudio(string url)
    {
        AudioType audioType = AudioType.UNKNOWN;

        string lowerUrl = url.ToLower();

        if (lowerUrl.EndsWith(".mp3"))
            audioType = AudioType.MPEG;
        else if (lowerUrl.EndsWith(".wav"))
            audioType = AudioType.WAV;
        else if (lowerUrl.EndsWith(".ogg"))
            audioType = AudioType.OGGVORBIS;

        using (UnityWebRequest request =
               UnityWebRequestMultimedia.GetAudioClip(url, audioType))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);

                audioSource.clip = clip;
                audioSource.Play();
            }
            else
            {
                Debug.LogError("Error cargando audio: " + request.error);
            }
        }
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        if (vp.targetTexture != null)
            videoDisplay.texture = vp.targetTexture;

        vp.Play();
    }

    public void ClosePanel()
    {
        if (currentAudioCoroutine != null)
        {
            StopCoroutine(currentAudioCoroutine);
            currentAudioCoroutine = null;
        }

        if (videoPlayer != null)
            videoPlayer.Stop();

        if (audioSource != null)
            audioSource.Stop();

        infoPanel.SetActive(false);
        closeButton.SetActive(false);
        IsUIOpen = false;
    }
}