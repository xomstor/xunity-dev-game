using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class LevelNameDisplay : MonoBehaviour
{
    public static LevelNameDisplay Instance { get; private set; }

    [Header("UI")]
    public GameObject displayPanel;
    public TextMeshProUGUI levelNameText;
    public Image backgroundImage;

    [Header("Animation")]
    public float showDuration = 3f;
    public float fadeDuration = 1f;
    public float startDelay = 0.2f;

    private CanvasGroup canvasGroup;
    private Coroutine currentAnimation;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        if (displayPanel != null)
            displayPanel.SetActive(false);
    }

    public void Show(string levelName)
    {
        Debug.Log($"LevelNameDisplay.Show called with: {levelName}");

        if (string.IsNullOrEmpty(levelName))
        {
            Debug.LogWarning("LevelNameDisplay: levelName is empty");
            return;
        }
        if (levelNameText == null)
        {
            Debug.LogError("LevelNameDisplay: levelNameText is not assigned!");
            return;
        }
        if (canvasGroup == null)
        {
            Debug.LogError("LevelNameDisplay: canvasGroup is missing!");
            return;
        }

        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        currentAnimation = StartCoroutine(AnimateLevelName(levelName));
    }

    IEnumerator AnimateLevelName(string levelName)
    {
        levelNameText.text = levelName;

        if (displayPanel != null)
            displayPanel.SetActive(true);
        else
            gameObject.SetActive(true);

        canvasGroup.alpha = 0f;

        yield return new WaitForSeconds(startDelay);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(showDuration);

        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;

        if (displayPanel != null)
            displayPanel.SetActive(false);

        currentAnimation = null;
    }
}
