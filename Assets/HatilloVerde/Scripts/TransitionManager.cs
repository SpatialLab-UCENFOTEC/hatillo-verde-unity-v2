using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class TransitionManager : MonoBehaviour
{
    [Header("Configuración de Épocas")]
    public GameObject[] environments;

    [Tooltip("Asegúrate de que este orden coincida perfectamente con el de tus entornos.")]
    public AudioClip[] narrationClips;

    [Tooltip("El texto personalizado que se mostrará para cada transición (ej: Mensaje 1 para el Pasado).")]
    public string[] transitionMessages;

    [Header("Componentes de Audio")]
    public AudioSource narrationSource;

    [Header("Outro")]
    public CanvasGroup outroGroup;
    public CanvasGroup videoGroup;
    public TextMeshProUGUI finalMessage;
    public TextMeshProUGUI counterText;
    public Button ctaButton;

    [Header("UI Referencias")]
    public CanvasGroup fadeGroup;
    public TextMeshProUGUI transitionText;

    [Header("Botones")]
    public Button backButton;
    public Button forwardButton;

    private int currentPeriodIndex = 0;
    private bool isTransitioning = false;

    void Start()
    {
        if (fadeGroup != null) fadeGroup.alpha = 0;
        if (transitionText != null) transitionText.alpha = 0;
        if (outroGroup != null) outroGroup.alpha = 0;

        if (narrationSource == null)
        {
            narrationSource = GetComponent<AudioSource>();
        }

        for (int i = 0; i < environments.Length; i++)
        {
            if (environments[i] != null)
                environments[i].SetActive(i == currentPeriodIndex);
        }

        UpdateButtons();
    }

    // NEXT
    public void NextPeriod()
    {
        if (isTransitioning) return;

        isTransitioning = true;
        UpdateButtons();

        if (currentPeriodIndex < environments.Length - 1)
        {
            StartCoroutine(PerformFullTransition(currentPeriodIndex + 1));
        }
        else
        {
            StartCoroutine(PlayOutro());
        }
    }

    public void PreviousPeriod()
    {
        if (isTransitioning) return;

        if (currentPeriodIndex > 0)
            isTransitioning = true;
        UpdateButtons();
        StartCoroutine(PerformFullTransition(currentPeriodIndex - 1));
    }

    // REMOVED 'bool forward' since text is now explicitly defined per scene index
    IEnumerator PerformFullTransition(int targetIndex)
    {
        if (narrationSource != null && narrationSource.isPlaying)
        {
            narrationSource.Stop();
        }

        // --- CUSTOM TEXT IMPLEMENTATION ---
        // Grab the custom message assigned to this target index from the Inspector
        if (targetIndex < transitionMessages.Length && !string.IsNullOrEmpty(transitionMessages[targetIndex]))
        {
            transitionText.text = transitionMessages[targetIndex];
        }
        else
        {
            // Fallback just in case a slot is left blank in the inspector
            transitionText.text = $"Cargando Época {targetIndex + 1}...";
        }

        // 1. Fade to black and show transition text
        yield return StartCoroutine(FadeCanvas(fadeGroup, 0, 1, 0.5f));
        yield return StartCoroutine(FadeText(transitionText, 0, 1, 0.4f));

        // 2. Change environment while screen is black
        currentPeriodIndex = targetIndex;
        for (int i = 0; i < environments.Length; i++)
        {
            if (environments[i] != null)
                environments[i].SetActive(i == currentPeriodIndex);
        }

        // 3. Play narration audio
        if (narrationSource != null && targetIndex < narrationClips.Length && narrationClips[targetIndex] != null)
        {
            narrationSource.clip = narrationClips[targetIndex];
            narrationSource.Play();

            // Wait for audio to finish playing completely
            yield return new WaitWhile(() => narrationSource.isPlaying);
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
        }

        // 4. Fade back out to reveal the new era
        yield return StartCoroutine(FadeText(transitionText, 1, 0, 0.4f));
        yield return StartCoroutine(FadeCanvas(fadeGroup, 1, 0, 0.6f));

        isTransitioning = false;
        UpdateButtons();
    }

    IEnumerator PlayOutro()
    {
        if (narrationSource != null && narrationSource.isPlaying)
        {
            narrationSource.Stop();
        }

        yield return StartCoroutine(FadeCanvas(outroGroup, 0, 1, 1f));

        if (videoGroup != null)
            videoGroup.alpha = 1f;

        int totalFound = Random.Range(10, 100);
        float t = 0f;
        while (t < 1.5f)
        {
            t += Time.deltaTime;
            int value = Mathf.RoundToInt(Mathf.Lerp(0, totalFound, t / 1.5f));
            counterText.text = $"Found: {value}";
            yield return null;
        }

        counterText.text = $"Found: {totalFound}";

        yield return StartCoroutine(FadeText(finalMessage, 0, 1, 1f));

        if (ctaButton != null)
        {
            ctaButton.gameObject.SetActive(true);
            ctaButton.interactable = true;
        }

        isTransitioning = false;
    }

    void UpdateButtons()
    {
        if (backButton != null)
            backButton.interactable = currentPeriodIndex > 0 && !isTransitioning;

        if (forwardButton != null)
            forwardButton.interactable = !isTransitioning;
    }

    IEnumerator FadeCanvas(CanvasGroup cg, float start, float end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }
        cg.alpha = end;
    }

    IEnumerator FadeText(TextMeshProUGUI txt, float start, float end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            txt.alpha = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }
        txt.alpha = end;
    }
}