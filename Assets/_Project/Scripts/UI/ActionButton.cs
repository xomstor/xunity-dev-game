using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ActionButton : MonoBehaviour
{
    [Header("Auto Create")]
    [Tooltip("If true and no Button is assigned, the button is created automatically at runtime.")]
    public bool autoCreate = true;

    [Header("UI Position")]
    public Vector2 anchoredPosition = new Vector2(922f, 439f);
    public Vector2 size = new Vector2(120f, 100f);
    public Color buttonColor = new Color(0.2f, 0.85f, 1f, 1f);
    public string buttonText = "E";

    [Header("Centered Circle Mode")]
    [Tooltip("If true, the button becomes a big tap zone in the center of the screen for dialogue taps.")]
    public bool centeredCircleMode = true;
    [Tooltip("Diameter of the invisible tap zone in pixels (reference 1080x2280).")]
    public float tapZoneSize = 300f;
    [Tooltip("Diameter of the visible circle in pixels. It will be smaller than the tap zone.")]
    public float circleVisualSize = 150f;
    [Tooltip("Vertical offset of the visual circle relative to the tap zone center.")]
    public float circleVisualOffsetY = 0f;
    public float topScreenOffsetY = 460f;
    [Tooltip("Speed of the visual pulse fade in/out.")]
    public float pulseSpeed = 2f;
    [Tooltip("Alpha range of the pulse effect.")]
    public float pulseAlphaMin = 0.2f;
    public float pulseAlphaMax = 0.6f;
    [Tooltip("If true, the button is only shown when the player is near an NPC with dialogue.")]
    public bool showOnlyNearDialogue = true;

    [Header("Icon")]
    [Tooltip("Optional icon to show on the button. If set, it will be drawn behind the text.")]
    public Sprite icon;
    [Tooltip("Optional Image component to use for the icon. If empty, a child icon is created automatically.")]
    public Image iconImage;

    [Header("Interaction")]
    public float interactRadius = 2f;

    private static ActionButton instance;
    private Button button;
    private GameObject tapZone;
    private GameObject circleVisual;
    private Image circleImage;
    private CanvasGroup circleGroup;
    private Canvas canvas;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (instance != null) return;

        GameObject go = new GameObject("ActionButton");
        instance = go.AddComponent<ActionButton>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        PlayerAction.interactRadius = interactRadius;
        BuildUI();
    }

    void BuildUI()
    {
        button = GetComponentInChildren<Button>(true);

        if (button == null && autoCreate)
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1;
                canvas.overrideSorting = true;
            }

            if (GetComponent<CanvasScaler>() == null)
            {
                CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 2280f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            tapZone = new GameObject("ActionButtonTapZone");
            tapZone.transform.SetParent(transform, false);

            RectTransform tapRt = tapZone.AddComponent<RectTransform>();
            tapRt.anchorMin = new Vector2(0.5f, 0.5f);
            tapRt.anchorMax = new Vector2(0.5f, 0.5f);
            tapRt.pivot = new Vector2(0.5f, 0.5f);
            tapRt.anchoredPosition = centeredCircleMode ? new Vector2(0f, topScreenOffsetY) : anchoredPosition;
            tapRt.sizeDelta = centeredCircleMode ? new Vector2(tapZoneSize, tapZoneSize) : size;

            tapZone.AddComponent<CanvasRenderer>();
            Image tapImg = tapZone.AddComponent<Image>();
            tapImg.color = new Color(1f, 1f, 1f, 0f);
            tapImg.type = Image.Type.Simple;
            tapImg.raycastTarget = true;

            button = tapZone.AddComponent<Button>();
            button.targetGraphic = tapImg;

            Transform textParent = tapZone.transform;
            if (centeredCircleMode)
            {
                circleVisual = new GameObject("ActionButtonVisual");
                circleVisual.transform.SetParent(tapZone.transform, false);

                RectTransform cvRt = circleVisual.AddComponent<RectTransform>();
                cvRt.anchorMin = new Vector2(0.5f, 0.5f);
                cvRt.anchorMax = new Vector2(0.5f, 0.5f);
                cvRt.pivot = new Vector2(0.5f, 0.5f);
                cvRt.anchoredPosition = new Vector2(0f, circleVisualOffsetY);
                cvRt.sizeDelta = new Vector2(circleVisualSize, circleVisualSize);

                circleVisual.AddComponent<CanvasRenderer>();
                circleImage = circleVisual.AddComponent<Image>();
                circleImage.color = buttonColor;
                circleImage.type = Image.Type.Simple;
                circleImage.raycastTarget = false;
                circleImage.alphaHitTestMinimumThreshold = 0.1f;
                circleGroup = circleVisual.AddComponent<CanvasGroup>();
                circleGroup.blocksRaycasts = false;

                if (icon == null)
                    circleImage.sprite = CreateCircleSprite(Color.white);

                textParent = circleVisual.transform;
            }

            CreateText(textParent);
        }

        if (button != null)
        {
            tapZone = button.gameObject;
            SetupIcon();
            button.onClick.AddListener(OnClick);
        }
    }

    void CreateText(Transform parent)
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject shadowGO = new GameObject("TextShadow");
        shadowGO.transform.SetParent(parent, false);
        RectTransform shadowRt = shadowGO.AddComponent<RectTransform>();
        shadowRt.anchorMin = Vector2.zero;
        shadowRt.anchorMax = Vector2.one;
        shadowRt.offsetMin = new Vector2(4f, -4f);
        shadowRt.offsetMax = new Vector2(4f, -4f);
        shadowGO.AddComponent<CanvasRenderer>();
        Text shadow = shadowGO.AddComponent<Text>();
        shadow.text = buttonText;
        shadow.font = font;
        shadow.fontSize = 60;
        shadow.fontStyle = FontStyle.Bold;
        shadow.alignment = TextAnchor.MiddleCenter;
        shadow.color = new Color(0f, 0.15f, 0.2f, 0.9f);

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(parent, false);
        RectTransform textRt = textGO.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        textGO.AddComponent<CanvasRenderer>();
        Text label = textGO.AddComponent<Text>();
        label.text = buttonText;
        label.font = font;
        label.fontSize = 60;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
    }

    void Update()
    {
        if (tapZone == null) return;
        PlayerAction.interactRadius = interactRadius;
        bool paused = PauseMenu.IsPaused;
        bool inDialogue = DialogueSystem.IsDialogueActive;
        bool nearInteractable = !showOnlyNearDialogue || PlayerAction.HasInteractableNearby();
        if (!paused && !inDialogue && nearInteractable)
            TutorialHintManager.ShowHint("interact", "Press E or tap the interaction button to talk.");
        bool shouldShow = !paused && !inDialogue && nearInteractable;
        if (tapZone.activeSelf != shouldShow)
            tapZone.SetActive(shouldShow);

        if (circleGroup != null && circleGroup.gameObject.activeInHierarchy)
        {
            float t = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            circleGroup.alpha = Mathf.Lerp(pulseAlphaMin, pulseAlphaMax, t);
        }

        if (!paused && !inDialogue && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && PlayerAction.HasInteractableNearby())
        {
            PlayerAction.interactRadius = interactRadius;
            PlayerAction.TryInteract();
        }
    }

    static Sprite CreateCircleSprite(Color color, int resolution = 256)
    {
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float radius = resolution / 2f - 2f;
        Color clear = new Color(0, 0, 0, 0);
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(center, new Vector2(x, y));
                if (dist <= radius)
                {
                    float alpha = 1f - Mathf.Clamp01((dist - (radius - 4f)) / 4f);
                    tex.SetPixel(x, y, new Color(color.r, color.g, color.b, alpha * 0.5f));
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        sprite.name = "GeneratedCircle";
        return sprite;
    }

    void SetupIcon()
    {
        if (icon == null) return;

        if (iconImage == null)
        {
            GameObject iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(circleVisual != null ? circleVisual.transform : button.transform, false);
            RectTransform iconRt = iconGO.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            iconGO.AddComponent<CanvasRenderer>();
            iconImage = iconGO.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconGO.transform.SetSiblingIndex(0);
        }

        iconImage.sprite = icon;
        iconImage.type = Image.Type.Simple;
        iconImage.color = Color.white;
    }

    void OnClick()
    {
        PlayerAction.interactRadius = interactRadius;
        PlayerAction.TryInteract();
    }

    void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);

        if (instance == this)
            instance = null;
    }
}
