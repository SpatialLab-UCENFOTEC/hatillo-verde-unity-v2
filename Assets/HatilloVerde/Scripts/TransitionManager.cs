using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class TransitionManager : MonoBehaviour
{
    [Header("Configuración de Épocas")]
    [Tooltip("0: Pasado, 1: Presente, 2: Futuro")]
    public GameObject[] environments;

    [Header("Mensajes de Transición")]
    public string messageToPast = "Regresando al origen...";
    public string messageToPresent = "Llegando al presente...";
    public string messageToFuture = "Viajando al futuro degradado...";

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

        for (int i = 0; i < environments.Length; i++)
        {
            environments[i].SetActive(i == currentPeriodIndex);
        }

        UpdateButtons();
    }

    // Llamado por botón Forward
    public void NextPeriod()
    {
        if (currentPeriodIndex < environments.Length - 1 && !isTransitioning)
        {
            currentPeriodIndex++;
            string msg = (currentPeriodIndex == 1) ? messageToPresent : messageToFuture;
            StartCoroutine(PerformFullTransition(msg));
        }
    }

    // Llamado por botón Back
    public void PreviousPeriod()
    {
        if (currentPeriodIndex > 0 && !isTransitioning)
        {
            currentPeriodIndex--;
            string msg = (currentPeriodIndex == 1) ? messageToPresent : messageToPast;
            StartCoroutine(PerformFullTransition(msg));
        }
    }

    IEnumerator PerformFullTransition(string message)
    {
        isTransitioning = true;
        UpdateButtons(); // bloquear botones durante transición

        transitionText.text = message;

        yield return StartCoroutine(FadeCanvas(fadeGroup, 0, 1, 0.5f));
        yield return StartCoroutine(FadeText(transitionText, 0, 1, 0.4f));

        for (int i = 0; i < environments.Length; i++)
        {
            environments[i].SetActive(i == currentPeriodIndex);
        }

        yield return new WaitForSeconds(1.5f);

        yield return StartCoroutine(FadeText(transitionText, 1, 0, 0.4f));
        yield return StartCoroutine(FadeCanvas(fadeGroup, 1, 0, 0.6f));

        isTransitioning = false;
        UpdateButtons();
    }

    void UpdateButtons()
    {
        if (backButton != null)
            backButton.interactable = currentPeriodIndex > 0 && !isTransitioning;

        if (forwardButton != null)
            forwardButton.interactable = currentPeriodIndex < environments.Length - 1 && !isTransitioning;
    }

    IEnumerator FadeCanvas(CanvasGroup cg, float start, float end, float duration)
    {
        float elapsed = 0;
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
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            txt.alpha = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }
        txt.alpha = end;
    }
}