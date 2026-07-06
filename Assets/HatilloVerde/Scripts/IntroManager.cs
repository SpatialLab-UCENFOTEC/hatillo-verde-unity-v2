using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.Video;
using UnityEngine.UI;

public class IntroManager : MonoBehaviour
{
    [Header("Panels")]
    public CanvasGroup introPanel;
    public CanvasGroup subIntroPanel;
    public TextMeshProUGUI subIntroText;
    public GameObject experiencePanel;

    [Header("Video")]
    public VideoPlayer introVideo;

    [Header("Audio")]
    public AudioSource subIntroAudioSource;
    public AudioClip subIntroClip;

    [Header("Botones")]
    public Button backButton;
    public Button forwardButton;

    [Tooltip("Botón 'Saltar' duplicado en el canvas del intro (el original vive bajo fadeGroup y está oculto durante el intro).")]
    public Button skipButton;

    private bool isIntroPlaying = true;
    private bool skipIntroRequested = false;

    [Header("Config")]
    public float fadeDuration = 1f;

    void Start()
    {
        experiencePanel.SetActive(false);

        subIntroPanel.gameObject.SetActive(true);
        subIntroPanel.alpha = 1f;
        subIntroPanel.interactable = false;
        subIntroPanel.blocksRaycasts = false;

        introPanel.gameObject.SetActive(true);
        introPanel.alpha = 1f;
        introPanel.interactable = true;
        introPanel.blocksRaycasts = true;

        subIntroText.alpha = 0f;

        // Asignar clip al AudioSource
        if (subIntroAudioSource != null && subIntroClip != null)
        {
            subIntroAudioSource.clip = subIntroClip;
        }

        // Skip de la primera transición: oculto al inicio; onClick cableado por
        // código para no depender del inspector del botón duplicado.
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
            skipButton.onClick.AddListener(SkipIntro);
        }

        isIntroPlaying = true;
        UpdateButtons();

    }

    public void StartExperience()
    {
        // Detener video
        if (introVideo != null && introVideo.isPlaying)
        {
            introVideo.Stop();
        }

        StartCoroutine(Flow());
    }

    IEnumerator Flow()
    {
        yield return StartCoroutine(FadeIntroOut());
        yield return StartCoroutine(SubIntroSequence());

        introPanel.blocksRaycasts = false;
        subIntroPanel.blocksRaycasts = false;

        introPanel.gameObject.SetActive(false);
        subIntroPanel.gameObject.SetActive(false);

        isIntroPlaying = false;
        UpdateButtons();

        // Enciende el 3D justo al revelar la experiencia.
        if (Scene3DGate.Instance != null) Scene3DGate.Instance.Show();

        experiencePanel.SetActive(true);
    }

    IEnumerator FadeIntroOut()
    {
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            introPanel.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }

        introPanel.alpha = 0f;
        introPanel.interactable = false;
        introPanel.blocksRaycasts = false;
    }

    IEnumerator SubIntroSequence()
    {
        subIntroText.alpha = 0f;
        skipIntroRequested = false;

        // Reproducir audio
        if (subIntroAudioSource != null && subIntroClip != null)
        {
            subIntroAudioSource.clip = subIntroClip;
            subIntroAudioSource.Play();
        }

        // Mostrar el botón Saltar y habilitar la interacción del panel para que
        // reciba clics (el subIntroPanel viene con blocksRaycasts en false).
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(true);
            skipButton.interactable = true;
        }
        subIntroPanel.interactable = true;
        subIntroPanel.blocksRaycasts = true;

        float t = 0f;

        // Fade IN texto
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            subIntroText.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }

        subIntroText.alpha = 1f;

        // Esperar a que termine el audio o a que el usuario lo salte
        if (subIntroAudioSource != null)
        {
            yield return new WaitUntil(() => skipIntroRequested || !subIntroAudioSource.isPlaying);
        }

        // Ocultar el botón Saltar y restaurar el panel a no interactivo
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
        }
        subIntroPanel.interactable = false;
        subIntroPanel.blocksRaycasts = false;

        t = 0f;

        // Fade OUT texto
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            subIntroText.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }

        subIntroText.alpha = 0f;

    }

    // Salta la narración de la primera transición (intro).
    public void SkipIntro()
    {
        if (skipIntroRequested) return;

        skipIntroRequested = true;

        if (subIntroAudioSource != null)
        {
            subIntroAudioSource.Stop();
        }

        if (skipButton != null)
        {
            skipButton.interactable = false;
            skipButton.gameObject.SetActive(false);
        }
    }

    void UpdateButtons()
    {
        if (backButton != null)
        {
            backButton.interactable = !isIntroPlaying;
            backButton.gameObject.SetActive(!isIntroPlaying);
        }

        if (forwardButton != null)
        {
            forwardButton.interactable = !isIntroPlaying;
            forwardButton.gameObject.SetActive(!isIntroPlaying);
        }
    }

    
}