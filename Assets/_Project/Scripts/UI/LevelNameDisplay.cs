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

    [Header("Curtains")]
    public bool useCurtains = true;
    public Image leftCurtain;
    public Image rightCurtain;
    public Color curtainColor = Color.black;
    public float curtainDuration = 0.8f;
    public float curtainMidPause = 0.4f;
    public float curtainClosedOffset = 0f;

    private CanvasGroup canvasGroup;
    private Coroutine currentAnimation;
    private RectTransform leftCurtainRt;
    private RectTransform rightCurtainRt;

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

        if (useCurtains)
            EnsureCurtains();
    }

    void EnsureCurtains()
    {
        if (leftCurtain != null && rightCurtain != null)
        {
            leftCurtainRt = leftCurtain.GetComponent<RectTransform>();
            rightCurtainRt = rightCurtain.GetComponent<RectTransform>();
            return;
        }

        Transform parent = displayPanel != null ? displayPanel.transform : transform;

        leftCurtain = CreateCurtain(parent, "LeftCurtain", new Vector2(0, 0), new Vector2(0.5f, 1), new Vector2(0, 0.5f));
        rightCurtain = CreateCurtain(parent, "RightCurtain", new Vector2(0.5f, 0), new Vector2(1, 1), new Vector2(1, 0.5f));
        leftCurtainRt = leftCurtain.GetComponent<RectTransform>();
        rightCurtainRt = rightCurtain.GetComponent<RectTransform>();
    }

    Image CreateCurtain(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = go.AddComponent<Image>();
        img.color = curtainColor;
        img.raycastTarget = false;
        go.SetActive(false);
        return img;
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

        if (displayPanel != null)
            displayPanel.SetActive(true);
        else
            gameObject.SetActive(true);

        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        currentAnimation = StartCoroutine(AnimateLevelName(levelName));
    }

    IEnumerator AnimateLevelName(string levelName)
    {
        levelNameText.text = levelName;
        canvasGroup.alpha = 0f;
        if (levelNameText != null)
            levelNameText.alpha = 0f;

        yield return new WaitForSeconds(startDelay);

        if (useCurtains && leftCurtain != null && rightCurtain != null)
        {
            leftCurtain.gameObject.SetActive(true);
            rightCurtain.gameObject.SetActive(true);
            SetCurtainsClosed();

            float elapsed = 0f;
            while (elapsed < curtainDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / curtainDuration);
                SetCurtainsOpen(t);
                yield return null;
            }
            SetCurtainsOpen(1f);

            canvasGroup.alpha = 1f;
            if (levelNameText != null)
                levelNameText.alpha = 1f;

            yield return new WaitForSeconds(showDuration);

            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;

            leftCurtain.gameObject.SetActive(false);
            rightCurtain.gameObject.SetActive(false);
        }
        else
        {
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
        }

        if (displayPanel != null)
            displayPanel.SetActive(false);

        currentAnimation = null;
    }

    void SetCurtainsClosed()
    {
        if (leftCurtainRt == null || rightCurtainRt == null) return;
        leftCurtainRt.anchoredPosition = new Vector2(curtainClosedOffset, 0);
        rightCurtainRt.anchoredPosition = new Vector2(-curtainClosedOffset, 0);
    }

    void SetCurtainsOpen(float t)
    {
        if (leftCurtainRt == null || rightCurtainRt == null) return;
        float screenWidth = GetScreenWidth();
        float leftX = Mathf.Lerp(curtainClosedOffset, -screenWidth * 0.5f, t);
        float rightX = Mathf.Lerp(-curtainClosedOffset, screenWidth * 0.5f, t);
        leftCurtainRt.anchoredPosition = new Vector2(leftX, 0);
        rightCurtainRt.anchoredPosition = new Vector2(rightX, 0);
    }

    float GetScreenWidth()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
        {
            RectTransform canvasRt = canvas.GetComponent<RectTransform>();
            if (canvasRt != null)
                return canvasRt.rect.width;
        }
        return Screen.width;
    }
}
