using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Video;

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
    public VideoPlayer outroVideo;
    public CanvasGroup outroGroup;
    public CanvasGroup videoGroup;
    public TextMeshProUGUI finalMessage;
    public TextMeshProUGUI counterText;
    

    [Header("UI Referencias")]
    public CanvasGroup fadeGroup;
    public TextMeshProUGUI transitionText;

    [Header("Botones")]
    public Button backButton;
    public Button forwardButton;
    public Button skipButton;
    public Button ctaButton;
    public Button creditsButton;
    public Button closeCreditsButton;

    [Tooltip("Botón 'Volver' del outro: regresa a la experiencia (última época) ocultando el panel final.")]
    public Button outroBackButton;

    private bool skipRequested = false;

    private int currentPeriodIndex = 0;
    private bool isTransitioning = false;

    void Start()
    {

        if (fadeGroup != null)
        {
            fadeGroup.alpha = 0;
            fadeGroup.interactable = false;
            fadeGroup.blocksRaycasts = false;
        }

        if (transitionText != null)
        {
            transitionText.alpha = 0;
        }

        if (outroGroup != null)
        {
            outroGroup.alpha = 0;
            outroGroup.interactable = false;
            outroGroup.blocksRaycasts = false;
        }

        if (narrationSource == null)
        {
            narrationSource = GetComponent<AudioSource>();
        }

        for (int i = 0; i < environments.Length; i++)
        {
            if (environments[i] != null)
                environments[i].SetActive(i == currentPeriodIndex);
        }

        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
        }

        if (ctaButton != null)
        {
            ctaButton.gameObject.SetActive(false);
        }

        if (creditsButton != null)
        {
            creditsButton.gameObject.SetActive(false);
        }

        if (closeCreditsButton != null)
        {
            closeCreditsButton.gameObject.SetActive(false);
        }

        // Botón "Volver" del outro: oculto fuera del outro; onClick cableado por
        // código para no depender del inspector.
        if (outroBackButton != null)
        {
            outroBackButton.gameObject.SetActive(false);
            outroBackButton.onClick.AddListener(ReturnToExperience);
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
        {
            isTransitioning = true;
            UpdateButtons();
            StartCoroutine(
                PerformFullTransition(currentPeriodIndex - 1)
                );
        }
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
        skipRequested = false;

        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(true);
            skipButton.interactable = true;
        }

        if (narrationSource != null && targetIndex < narrationClips.Length && narrationClips[targetIndex] != null)
        {
            narrationSource.clip = narrationClips[targetIndex];
            narrationSource.Play();

            // Wait for audio to finish playing completely
            yield return new WaitUntil(() =>
                skipRequested ||
                !narrationSource.isPlaying
            );
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
        }

        // 4. Fade back out to reveal the new era
        yield return StartCoroutine(FadeText(transitionText, 1, 0, 0.4f));
        yield return StartCoroutine(FadeCanvas(fadeGroup, 1, 0, 0.6f));


        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
        }

        isTransitioning = false;
        UpdateButtons();
    }

    IEnumerator PlayOutro()
    {
        outroGroup.gameObject.SetActive(true);

        // Habilitar interacción del panel final
        outroGroup.interactable = true;
        outroGroup.blocksRaycasts = true;

        if (narrationSource != null && narrationSource.isPlaying)
        {
            narrationSource.Stop();
        }

        int totalFound = 0;
        int totalObjects = 0;
        if (DiscoveryManager.Instance != null)
        {
            totalFound = DiscoveryManager.Instance.GetDiscoveredCount();
            totalObjects = DiscoveryManager.Instance.GetTotalCount();
        }

        // Preparar todo el contenido (texto y botones) ANTES del fade del panel
        // para que entre con el único fade del canvas, sin fades por elemento.
        if (finalMessage != null) finalMessage.alpha = 1f;
        if (counterText != null)
            counterText.text = $"Elementos encontrados: 0 / {totalObjects}";

        if (ctaButton != null)
        {
            ctaButton.gameObject.SetActive(true);
            ctaButton.interactable = true;
        }

        if (creditsButton != null)
        {
            creditsButton.gameObject.SetActive(true);
            creditsButton.interactable = true;
        }

        // El botón "Volver" de créditos viene con m_Interactable en 0; lo dejamos
        // listo para responder al click cuando se muestre.
        if (closeCreditsButton != null)
        {
            closeCreditsButton.interactable = true;
        }

        if (outroBackButton != null)
        {
            outroBackButton.gameObject.SetActive(true);
            outroBackButton.interactable = true;
        }

        // Outro listo: ya no estamos en transición.
        isTransitioning = false;

        // Fade único de todo el panel (incluye textos y botones).
        yield return StartCoroutine(FadeCanvas(outroGroup, 0, 1, 1f));

        if (videoGroup != null)
        {
            videoGroup.gameObject.SetActive(true);
            videoGroup.alpha = 1f;
        }

        if (outroVideo != null)
        {
            outroVideo.Stop();
            outroVideo.Play();
        }

        // Conteo del contador (animación numérica, no fade) ya con el panel visible.
        if (counterText != null)
        {
            float t = 0f;
            while (t < 1.5f)
            {
                t += Time.deltaTime;
                int value = Mathf.RoundToInt(Mathf.Lerp(0, totalFound, t / 1.5f));
                counterText.text = $"Elementos encontrados: {value} / {totalObjects}";
                yield return null;
            }

            counterText.text = $"Elementos encontrados: {totalFound} / {totalObjects}";
        }
    }

    // Regresa del outro a la experiencia ocultando el panel final y reproduciendo
    // la transición (fade + narración) correspondiente a la última época.
    public void ReturnToExperience()
    {
        if (isTransitioning) return;

        isTransitioning = true;
        UpdateButtons();

        if (outroVideo != null)
        {
            outroVideo.Stop();
        }

        // Ocultar botones propios del outro.
        if (ctaButton != null) ctaButton.gameObject.SetActive(false);
        if (creditsButton != null) creditsButton.gameObject.SetActive(false);
        if (closeCreditsButton != null) closeCreditsButton.gameObject.SetActive(false);
        if (outroBackButton != null) outroBackButton.gameObject.SetActive(false);

        // Ocultar el panel final. fadeGroup se dibuja por encima del outro, así que
        // PerformFullTransition lo cubrirá con el fade a negro de inmediato.
        if (videoGroup != null)
        {
            videoGroup.alpha = 0f;
            videoGroup.gameObject.SetActive(false);
        }

        if (outroGroup != null)
        {
            outroGroup.alpha = 0f;
            outroGroup.interactable = false;
            outroGroup.blocksRaycasts = false;
            outroGroup.gameObject.SetActive(false);
        }

        // Reproducir la transición de la última época (reutiliza la misma lógica
        // que la navegación normal: fade, narración, botón Saltar y reveal).
        StartCoroutine(PerformFullTransition(environments.Length - 1));
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
        if (cg == null) yield break;

        // Si se está mostrando el panel, habilitamos interacción
        if (end > 0f)
        {
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }

        cg.alpha = end;

        // Si quedó invisible, deshabilitamos interacción
        if (end <= 0f)
        {
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
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

    public void CloseCredits()
    {
        Debug.Log("VOLVER CLICKED");

        // El onClick (en escena) ya desactiva el panel de créditos.
        // Aquí restablecemos el estado de los botones para poder reabrirlo.
        if (closeCreditsButton != null)
        {
            closeCreditsButton.gameObject.SetActive(false);
        }

        if (creditsButton != null)
        {
            creditsButton.gameObject.SetActive(true);
            creditsButton.interactable = true;
        }
    }

    public void SkipTransition()
    {
        Debug.Log("SKIP CLICKED");

        if (skipRequested) return;

        skipRequested = true;

        if (narrationSource != null)
        {
            narrationSource.Stop();
        }

        if (skipButton != null)
        {
            skipButton.interactable = false;
            skipButton.gameObject.SetActive(false);
        }
    }
}