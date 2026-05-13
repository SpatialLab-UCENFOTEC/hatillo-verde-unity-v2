using UnityEngine;
using System.Collections;
using TMPro;

public class IntroOutroManager : MonoBehaviour
{
    [Header("Panels")]
    public CanvasGroup introPanel;
    public CanvasGroup outroPanel;
    public GameObject experiencePanel;

    [Header("Configuración")]
    public float fadeDuration = 1f;

    void Start()
    {
        if (introPanel == null || outroPanel == null || experiencePanel == null)
        {
            Debug.LogError("Faltan referencias en IntroOutroManager.");
            return;
        }

        // Desactivar experiencia al inicio
        experiencePanel.SetActive(false);

        // Configurar Intro visible
        introPanel.gameObject.SetActive(true);
        introPanel.alpha = 1f;
        introPanel.interactable = true;
        introPanel.blocksRaycasts = true;

        // Configurar Outro oculto
        outroPanel.alpha = 0f;
        outroPanel.interactable = false;
        outroPanel.blocksRaycasts = false;
    }

    public void StartExperience()
    {
        Debug.Log("Botón presionado");

        StartCoroutine(HideIntro());
    }

    IEnumerator HideIntro()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            introPanel.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        introPanel.alpha = 0f;
        introPanel.interactable = false;
        introPanel.blocksRaycasts = false;
        introPanel.gameObject.SetActive(false);

        experiencePanel.SetActive(true);
    }
}