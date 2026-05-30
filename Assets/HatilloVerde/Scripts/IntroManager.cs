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
    public Button skipButton;
    private bool skipRequested = false;

    private bool isIntroPlaying = true;

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

        isIntroPlaying = true;
        UpdateButtons();

        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
        }
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

        // Reproducir audio
        if (subIntroAudioSource != null && subIntroClip != null)
        {
            subIntroAudioSource.clip = subIntroClip;
            subIntroAudioSource.Play();
        }

        float t = 0f;

        skipRequested = false;

        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(true);
            skipButton.interactable = true;
        }

        // Fade IN texto
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            subIntroText.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }

        subIntroText.alpha = 1f;

        // Esperar a que termine el audio
        if (subIntroAudioSource != null)
        {
            yield return new WaitUntil(() => 
                skipRequested || 
                !subIntroAudioSource.isPlaying
            );
        }

        t = 0f;

        // Fade OUT texto
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            subIntroText.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }

        subIntroText.alpha = 0f;

        if (skipButton != null)
        {
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

    public void SkipSubIntro()
    {
        Debug.Log("SKIP PRESIONADO");

        if (skipRequested) return;

        skipRequested = true;

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
}