using System.Collections;
using UnityEngine;
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

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

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
