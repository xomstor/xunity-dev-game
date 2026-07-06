using UnityEngine;
using TMPro; // ← Добавь этот namespace!
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("Target")]
    public AutoCombat target;

    [Header("Components")]
    public Image healthBarFill;
    private Image backgroundImage;
    private Image heartIcon;
    private Image heartFill;
    private Material heartMaterial;

    [Header("Heart Style")]
    public Sprite heartOutlineSprite;
    public Sprite heartInsideSprite;
    public Vector2 heartIconSize = new Vector2(40, 40);
    public Vector2 heartIconPosition = new Vector2(0, 0);
    public Color heartFullColor = Color.red;
    public Color heartMidColor = Color.blue;
    public Color heartEmptyColor = Color.black;
    public static string HealthBarStyleKey = "HealthBarStyle";

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

        backgroundImage = GetComponent<Image>();
        if (heartOutlineSprite == null)
            heartOutlineSprite = LoadHeartOutline();
        if (heartInsideSprite == null)
            heartInsideSprite = LoadHeartInside();
        CreateHeartIcon();
        ApplyStyle();

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
        if (target == null)
            return;

        if (!target.gameObject.activeInHierarchy || target.IsDead)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        ApplyStyle();

        if (followTarget && target != null)
        {
            UpdatePosition();
        }

        UpdateHealthDisplay();
    }

    void ApplyStyle()
    {
        bool useHeart = PlayerPrefs.GetInt(HealthBarStyleKey, 0) == 1;
        bool hasHeart = heartIcon != null && heartOutlineSprite != null && heartFill != null && heartInsideSprite != null;

        if (backgroundImage != null)
            backgroundImage.enabled = !useHeart;
        if (healthBarFill != null)
            healthBarFill.enabled = !useHeart;

        if (heartIcon != null)
            heartIcon.enabled = useHeart && hasHeart;
        if (heartFill != null)
            heartFill.enabled = useHeart && hasHeart;

        if (tmpText != null)
            tmpText.enabled = !useHeart;
        if (legacyText != null)
            legacyText.enabled = !useHeart;
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
        Color healthColor;
        if (healthPercent > 0.6f)
            healthColor = highHealthColor;
        else if (healthPercent > 0.3f)
            healthColor = midHealthColor;
        else
            healthColor = lowHealthColor;

        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = Mathf.Clamp01(healthPercent);
            healthBarFill.color = healthColor;
        }

        Color heartColor;
        if (healthPercent > 0.5f)
            heartColor = Color.Lerp(heartMidColor, heartFullColor, (healthPercent - 0.5f) * 2f);
        else
            heartColor = Color.Lerp(heartEmptyColor, heartMidColor, healthPercent * 2f);

        if (heartIcon != null)
            heartIcon.color = Color.white;

        if (heartMaterial != null)
        {
            heartMaterial.SetFloat("_Fill", Mathf.Clamp01(healthPercent));
            heartMaterial.SetColor("_ColorHigh", heartFullColor);
            heartMaterial.SetColor("_ColorMid", heartMidColor);
            heartMaterial.SetColor("_ColorLow", heartEmptyColor);
        }
        else if (heartFill != null)
        {
            heartFill.fillAmount = Mathf.Clamp01(healthPercent);
            heartFill.color = heartColor;
        }

        // ✅ ОБНОВЛЕНИЕ ТЕКСТА (поддерживает оба типа!)
        if (showText)
        {
            string hpText = $"{target.CurrentHealth}/{target.maxHealth}";

            if (tmpText != null)
            {
                // TextMeshPro
                tmpText.text = hpText;
                tmpText.color = healthColor;
            }
            else if (legacyText != null)
            {
                // Старый Text
                legacyText.text = hpText;
                legacyText.color = healthColor;
            }
        }
    }

    public void SetTarget(AutoCombat newTarget)
    {
        target = newTarget;
    }

    void CreateHeartIcon()
    {
        if (heartOutlineSprite == null || heartInsideSprite == null)
        {
            Debug.LogWarning($"[HealthBarUI] {name}: heart sprites missing, cannot create heart icon.");
            return;
        }

        // Find or create fill (liquid) behind the outline
        Transform fill = transform.Find("HeartFill");
        if (fill != null)
            heartFill = fill.GetComponent<Image>();
        if (heartFill == null)
        {
            GameObject fillGO = new GameObject("HeartFill");
            fillGO.transform.SetParent(transform, false);
            fillGO.AddComponent<CanvasRenderer>();
            heartFill = fillGO.AddComponent<Image>();
            RectTransform fillRt = fillGO.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0.5f, 0.5f);
            fillRt.anchorMax = new Vector2(0.5f, 0.5f);
            fillRt.pivot = new Vector2(0.5f, 0.5f);
            fillRt.anchoredPosition = heartIconPosition;
            fillRt.sizeDelta = heartIconSize;
        }

        heartFill.sprite = heartInsideSprite;
        heartFill.preserveAspect = true;
        heartFill.raycastTarget = false;

        if (heartMaterial == null)
        {
            Shader shader = Shader.Find("UI/HeartLiquid");
            if (shader != null)
                heartMaterial = new Material(shader);
            else
                Debug.LogWarning($"[HealthBarUI] {name}: Shader UI/HeartLiquid not found, using fallback heart fill.");
        }

        if (heartMaterial != null)
        {
            heartFill.material = heartMaterial;
        }
        else
        {
            // Fallback: use regular filled image
            heartFill.material = null;
            heartFill.type = Image.Type.Filled;
            heartFill.fillMethod = Image.FillMethod.Vertical;
            heartFill.fillOrigin = (int)Image.OriginVertical.Bottom;
        }

        // Find or create outline on top
        Transform heart = transform.Find("HeartIcon");
        if (heart != null)
        {
            heartIcon = heart.GetComponent<Image>();
            if (heartIcon != null)
            {
                heartIcon.sprite = heartOutlineSprite;
                heartIcon.preserveAspect = true;
                if (heartFill != null)
                    heartFill.transform.SetSiblingIndex(Mathf.Max(0, heartIcon.transform.GetSiblingIndex() - 1));
                return;
            }
        }

        GameObject iconGO = new GameObject("HeartIcon");
        iconGO.transform.SetParent(transform, false);
        iconGO.AddComponent<CanvasRenderer>();
        heartIcon = iconGO.AddComponent<Image>();
        heartIcon.sprite = heartOutlineSprite;
        heartIcon.preserveAspect = true;
        heartIcon.color = Color.white;
        heartIcon.raycastTarget = false;

        RectTransform iconRt = iconGO.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = heartIconPosition;
        iconRt.sizeDelta = heartIconSize;

        // Ensure fill is behind the outline
        if (heartFill != null)
            heartFill.transform.SetSiblingIndex(0);

        Debug.Log($"[HealthBarUI] {name}: created heart icon and fill. Shader={(heartMaterial != null)}");
    }

    static Sprite LoadHeartOutline()
    {
        return LoadSpriteFromPath("Assets/_Project/Art/UI/Heart/HppenisOutline.png");
    }

    static Sprite LoadHeartInside()
    {
        return LoadSpriteFromPath("Assets/_Project/Art/UI/Heart/HppenisInside.png");
    }

    static Sprite LoadSpriteFromPath(string path)
    {
#if UNITY_EDITOR
        Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        if (assets != null)
        {
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite)
                    return sprite;
            }
        }
        return null;
#else
        return Resources.Load<Sprite>("UI/Heart/" + System.IO.Path.GetFileNameWithoutExtension(path));
#endif
    }
}