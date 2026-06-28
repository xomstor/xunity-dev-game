using UnityEngine;
using TMPro; // ← Добавь этот namespace!
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("Target")]
    public AutoCombat target;

    [Header("Components")]
    public Image healthBarFill;

    [Header("Text Settings")]
    public bool showText = true;

    // ✅ ИЗМЕНЕНО: Теперь поддерживает ОБА типа текста!
    [Header("Text (Legacy or TMP)")]
    public Text legacyText; // Для старого Text
    public TextMeshProUGUI tmpText; // Для TextMeshPro

    [Header("Settings")]
    public Vector3 worldOffset = new Vector3(0, 1.5f, 0);
    public bool followTarget = true;

    [Header("Colors")]
    public Color highHealthColor = Color.green;
    public Color midHealthColor = Color.yellow;
    public Color lowHealthColor = Color.red;

    private Camera mainCamera;
    private RectTransform rectTransform;
    private Canvas parentCanvas;

    void Start()
    {
        mainCamera = Camera.main;
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

        if (target == null)
        {
            Debug.LogError($"{name}: Не указан target!");
            enabled = false;
        }

        // Авто-поиск текста если не задан
        if (showText && legacyText == null && tmpText == null)
        {
            // Пытаемся найти TextMeshPro сначала
            tmpText = GetComponentInChildren<TextMeshProUGUI>();

            // Если нет - ищем старый Text
            if (tmpText == null)
                legacyText = GetComponentInChildren<Text>();
        }
    }

    void LateUpdate()
    {
        if (target == null || healthBarFill == null)
            return;

        if (!target.gameObject.activeInHierarchy || target.IsDead)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (followTarget && target != null)
        {
            UpdatePosition();
        }

        UpdateHealthDisplay();
    }

    void UpdatePosition()
    {
        if (mainCamera == null || target == null) return;

        Vector3 worldPosition = target.transform.position + worldOffset;
        Vector2 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            screenPosition,
            parentCanvas.worldCamera,
            out localPoint
        );

        if (rectTransform != null)
        {
            rectTransform.localPosition = localPoint;
        }
    }

    void UpdateHealthDisplay()
    {
        if (target.maxHealth <= 0) return;

        float healthPercent = (float)target.CurrentHealth / target.maxHealth;

        healthBarFill.fillAmount = Mathf.Clamp01(healthPercent);

        // Цвет полоски
        if (healthPercent > 0.6f)
            healthBarFill.color = highHealthColor;
        else if (healthPercent > 0.3f)
            healthBarFill.color = midHealthColor;
        else
            healthBarFill.color = lowHealthColor;

        // ✅ ОБНОВЛЕНИЕ ТЕКСТА (поддерживает оба типа!)
        if (showText)
        {
            string hpText = $"{target.CurrentHealth}/{target.maxHealth}";

            if (tmpText != null)
            {
                // TextMeshPro
                tmpText.text = hpText;
                tmpText.color = healthBarFill.color;
            }
            else if (legacyText != null)
            {
                // Старый Text
                legacyText.text = hpText;
                legacyText.color = healthBarFill.color;
            }
        }
    }

    public void SetTarget(AutoCombat newTarget)
    {
        target = newTarget;
    }
}