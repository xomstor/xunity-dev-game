using UnityEngine;
using UnityEngine.UI;

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

    [Header("Icon")]
    [Tooltip("Optional icon to show on the button. If set, it will be drawn behind the text.")]
    public Sprite icon;
    [Tooltip("Optional Image component to use for the icon. If empty, a child icon is created automatically.")]
    public Image iconImage;

    [Header("Interaction")]
    public float interactRadius = 2f;

    private static ActionButton instance;
    private Button button;
    private GameObject visual;
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

            GameObject visual = new GameObject("ActionButtonVisual");
            visual.transform.SetParent(transform, false);

            RectTransform rt = visual.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;

            visual.AddComponent<CanvasRenderer>();

            Image img = visual.AddComponent<Image>();
            img.color = buttonColor;
            img.type = Image.Type.Simple;
            img.raycastTarget = true;

            button = visual.AddComponent<Button>();
            button.targetGraphic = img;

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(visual.transform, false);
            RectTransform textRt = textGO.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            textGO.AddComponent<CanvasRenderer>();
            Text label = textGO.AddComponent<Text>();
            label.text = buttonText;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 48;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
        }

        if (button != null)
        {
            visual = button.gameObject;
            SetupIcon();
            button.onClick.AddListener(OnClick);
        }
    }

    void Update()
    {
        if (visual == null) return;
        bool paused = PauseMenu.IsPaused;
        if (paused && visual.activeSelf)
            visual.SetActive(false);
        else if (!paused && !visual.activeSelf)
            visual.SetActive(true);
    }

    void SetupIcon()
    {
        if (icon == null) return;

        if (iconImage == null)
        {
            GameObject iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(button.transform, false);
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
