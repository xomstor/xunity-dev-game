using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemNotification : MonoBehaviour
{
    public static ItemNotification Instance { get; private set; }

    [Header("UI")]
    public GameObject notificationPanel;
    public TextMeshProUGUI notificationText;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip pickupClip;
    public AudioClip spendClip;

    [Header("Settings")]
    public float displayDuration = 2f;
    public float fadeDuration = 0.5f;

    private CanvasGroup canvasGroup;
    private Coroutine currentRoutine;

    public static ItemNotification EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        GameObject go = new GameObject("ItemNotification");
        DontDestroyOnLoad(go);
        return go.AddComponent<ItemNotification>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (notificationPanel == null || notificationText == null)
            CreateDefaultUI();

        if (notificationPanel != null)
        {
            canvasGroup = notificationPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = notificationPanel.AddComponent<CanvasGroup>();
            notificationPanel.SetActive(false);
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0f;
                audioSource.playOnAwake = false;
            }
        }
    }

    void CreateDefaultUI()
    {
        GameObject canvasGO = new GameObject("ItemNotificationCanvas");
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        notificationPanel = new GameObject("NotificationPanel");
        notificationPanel.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRt = notificationPanel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.82f);
        panelRt.anchorMax = new Vector2(0.5f, 0.82f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(520f, 70f);
        panelRt.anchoredPosition = Vector2.zero;

        Image bg = notificationPanel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.65f);

        GameObject textGO = new GameObject("NotificationText");
        textGO.transform.SetParent(notificationPanel.transform, false);
        RectTransform textRt = textGO.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(20f, 8f);
        textRt.offsetMax = new Vector2(-20f, -8f);

        notificationText = textGO.AddComponent<TextMeshProUGUI>();
        notificationText.alignment = TextAlignmentOptions.Center;
        notificationText.fontSize = 28f;
        notificationText.color = Color.white;
        notificationText.raycastTarget = false;
    }

    public void ShowPickup(string itemName, int quantity = 1)
    {
        string text = quantity > 1
            ? $"Получено: {itemName} x{quantity}"
            : $"Получено: {itemName}";
        Show(text, pickupClip);
    }

    public void ShowSpend(string itemName, int quantity = 1)
    {
        string text = quantity > 1
            ? $"Потрачено: {itemName} x{quantity}"
            : $"Потрачено: {itemName}";
        Show(text, spendClip);
    }

    void Show(string text, AudioClip clip)
    {
        if (notificationText != null)
            notificationText.text = text;

        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(ShowRoutine());
    }

    IEnumerator ShowRoutine()
    {
        if (notificationPanel != null)
            notificationPanel.SetActive(true);

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(displayDuration);

        if (canvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = 1f - (elapsed / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }

        if (notificationPanel != null)
            notificationPanel.SetActive(false);
    }
}
