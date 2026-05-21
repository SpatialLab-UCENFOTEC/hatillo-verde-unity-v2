using UnityEngine;
using System.Collections;
using TMPro;

public class IntroManager : MonoBehaviour
{
    [Header("Panels")]
    public CanvasGroup introPanel;
    public CanvasGroup subIntroPanel;
    public TextMeshProUGUI subIntroText;
    public GameObject experiencePanel;

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
    }

    public void StartExperience()
    {
        StartCoroutine(Flow());
    }

    IEnumerator Flow()
    {
        yield return StartCoroutine(FadeIntroOut());
        yield return StartCoroutine(SubIntroSequence());

        // IMPORTANT: fully clear UI block state
        introPanel.blocksRaycasts = false;
        subIntroPanel.blocksRaycasts = false;

        introPanel.gameObject.SetActive(false);
        subIntroPanel.gameObject.SetActive(false);

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

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            subIntroText.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }

        subIntroText.alpha = 1f;

        yield return new WaitForSeconds(1.5f);

        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            subIntroText.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }

        subIntroText.alpha = 0f;
    }
}