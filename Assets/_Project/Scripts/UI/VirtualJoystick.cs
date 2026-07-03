using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("References")]
    public RectTransform background;
    public RectTransform handle;
    public PlayerController player;

    [Header("Settings")]
    [Tooltip("Max pixels handle can move from center")]
    public float handleRange = 60f;
    public float deadZone = 0.1f;
    [Tooltip("Alpha when idle")]
    [Range(0f, 1f)] public float joystickAlpha = 0.33f;
    [Tooltip("Alpha when actively touching")]
    [Range(0f, 1f)] public float joystickActiveAlpha = 0.66f;
    [Tooltip("If true, joystick appears at touch position. If false, stays fixed.")]
    public bool isFloating = false;

    private Vector2 input = Vector2.zero;
    private Camera uiCamera;
    private bool isHolding = false;
    private bool jumpTriggered = false;
    private bool wasCrouching = false;
    private Vector2 originalBackgroundPos;
    private Vector2 originalBackgroundSize;
    private Vector2 originalAnchorMin;
    private Vector2 originalAnchorMax;
    private Image backgroundImage;
    private Image handleImage;
    private RectTransform touchArea;
    private Image touchAreaImage;
    private RectTransform visualCircle;
    private JoystickRingGraphic visualCircleImage;
    private Vector2 floatingOrigin;
    private int activePointerId = int.MinValue;
    private Sprite originalBackgroundSprite;
    private Vector2 originalHandleSize;
    private Transform originalHandleParent;
    private Vector2 originalHandleAnchorMin;
    private Vector2 originalHandleAnchorMax;
    private Vector2 originalHandlePivot;
    private Vector2 originalHandlePosition;
    private Image originalRingImage;
    private Vector2 originalRingSize;

    void Awake()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (background == null) background = GetComponent<RectTransform>();
        if (handle == null && transform.childCount > 0) handle = transform.GetChild(0).GetComponent<RectTransform>();

        originalBackgroundPos = background.anchoredPosition;
        originalBackgroundSize = background.sizeDelta;
        originalAnchorMin = background.anchorMin;
        originalAnchorMax = background.anchorMax;
        backgroundImage = background.GetComponent<Image>();
        if (handle != null)
        {
            handleImage = handle.GetComponent<Image>();
            originalHandleSize = handle.sizeDelta;
            originalHandleParent = handle.parent;
            originalHandleAnchorMin = handle.anchorMin;
            originalHandleAnchorMax = handle.anchorMax;
            originalHandlePivot = handle.pivot;
            originalHandlePosition = handle.anchoredPosition;
        }

        Image[] initialImages = background.GetComponentsInChildren<Image>(true);
        foreach (Image img in initialImages)
        {
            if (img == handleImage) continue;
            if (img.sprite == null) continue;
            originalRingImage = img;
            originalBackgroundSprite = img.sprite;
            originalRingSize = img.rectTransform.sizeDelta;
            break;
        }
        if (originalRingImage == null && backgroundImage != null)
        {
            originalRingImage = backgroundImage;
            originalBackgroundSprite = backgroundImage.sprite;
            originalRingSize = background.sizeDelta;
        }

        EnsureProceduralRing();
        ApplyTransparency();
        LoadSettings();
        if (isFloating) SetupFloatingMode();
    }

    // <-- Добавляем Update для непрерывной отправки ввода
    void Update()
    {
        if (isHolding && player != null)
        {
            ApplyInputActions();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isHolding) return;

        isHolding = true;
        activePointerId = eventData.pointerId;
        input = Vector2.zero;
        if (handle != null)
            handle.anchoredPosition = Vector2.zero;

        if (!isFloating)
            SetVisualAlpha(joystickActiveAlpha);

        if (isFloating)
        {
            RectTransform parentRt = background.parent as RectTransform;
            if (parentRt != null)
            {
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRt, eventData.position, uiCamera, out localPoint))
                {
                    floatingOrigin = localPoint;
                    background.anchoredPosition = floatingOrigin;
                    background.SetAsLastSibling();
                    SetVisualAlpha(joystickActiveAlpha);
                }
            }
        }

        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isHolding || eventData.pointerId != activePointerId) return;

        Vector2 localPoint;
        if (isFloating)
        {
            RectTransform parentRt = background.parent as RectTransform;
            if (parentRt == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRt, eventData.position, uiCamera, out localPoint)) return;

            localPoint -= floatingOrigin;
        }
        else
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background, eventData.position, uiCamera, out localPoint)) return;
        }

        Vector2 delta = Vector2.ClampMagnitude(localPoint, handleRange);
        handle.anchoredPosition = delta;
        input = delta / handleRange;

        ApplyInputActions();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isHolding || eventData.pointerId != activePointerId) return;

        isHolding = false;
        activePointerId = int.MinValue;
        input = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
        SetVisualAlpha(isFloating ? 0f : joystickAlpha);

        jumpTriggered = false;
        if (wasCrouching)
        {
            wasCrouching = false;
            player?.StopCrouch();
        }

        if (player != null) player.SetMoveInput(0f);
    }

    public Vector2 GetInput() => input;

    public bool IsUsable => enabled && gameObject.activeInHierarchy && background != null && handle != null;

    void ApplyInputActions()
    {
        if (player == null) return;

        float horizontal = Mathf.Abs(input.x) > deadZone ? input.x : 0f;
        player.SetMoveInput(horizontal);

        float verticalThreshold = 0.7f;
        if (input.y > verticalThreshold && !jumpTriggered)
        {
            jumpTriggered = true;
            player.Jump();
        }
        else if (input.y < verticalThreshold * 0.5f)
        {
            jumpTriggered = false;
        }

        if (input.y < -verticalThreshold)
        {
            if (!wasCrouching)
            {
                wasCrouching = true;
                if (!player.TryDropThrough())
                    player.StartCrouch();
            }
        }
        else if (wasCrouching && input.y > -verticalThreshold * 0.5f)
        {
            wasCrouching = false;
            player.StopCrouch();
        }
    }

    void ApplyTransparency()
    {
        SetVisualAlpha(joystickAlpha);
    }

    void SetVisualAlpha(float alpha)
    {
        if (backgroundImage != null)
        {
            backgroundImage.enabled = true;
            backgroundImage.raycastTarget = !isFloating;
            Color c = backgroundImage.color;
            c.a = 0f;
            backgroundImage.color = c;
        }
        if (visualCircleImage != null)
        {
            Color c = visualCircleImage.color;
            c.a = alpha > 0f ? Mathf.Max(alpha, 0.55f) : 0f;
            visualCircleImage.color = c;
        }
        if (handleImage != null)
        {
            Color c = handleImage.color;
            c.a = alpha;
            handleImage.color = c;
        }
    }

    public void SetFloating(bool enabled)
    {
        isFloating = enabled;
        PlayerPrefs.SetInt("FloatingJoystick", enabled ? 1 : 0);
        PlayerPrefs.Save();
        if (enabled)
            SetupFloatingMode();
        else
            DisableFloatingMode();
    }

    void SetupFloatingMode()
    {
        RectTransform parentRt = background.parent as RectTransform;
        if (parentRt == null) return;

        EnsureProceduralRing();

        background.anchorMin = new Vector2(0.5f, 0.5f);
        background.anchorMax = new Vector2(0.5f, 0.5f);
        background.pivot = new Vector2(0.5f, 0.5f);
        background.sizeDelta = originalBackgroundSize;
        background.anchoredPosition = originalBackgroundPos;

        if (handle != null)
        {
            handle.SetParent(background, false);
            handle.anchorMin = new Vector2(0.5f, 0.5f);
            handle.anchorMax = new Vector2(0.5f, 0.5f);
            handle.pivot = new Vector2(0.5f, 0.5f);
            handle.sizeDelta = originalHandleSize;
            handle.anchoredPosition = Vector2.zero;
            if (handleImage != null) handleImage.raycastTarget = false;
        }

        if (touchArea == null)
        {
            GameObject touchGO = new GameObject("JoystickTouchArea");
            touchGO.transform.SetParent(parentRt, false);
            touchArea = touchGO.AddComponent<RectTransform>();
            touchArea.anchorMin = new Vector2(0f, 0f);
            touchArea.anchorMax = new Vector2(0.45f, 0.6f);
            touchArea.offsetMin = Vector2.zero;
            touchArea.offsetMax = Vector2.zero;
            touchArea.pivot = new Vector2(0.5f, 0.5f);
            touchAreaImage = touchGO.AddComponent<Image>();
            touchAreaImage.color = new Color(0f, 0f, 0f, 0f);
            touchAreaImage.raycastTarget = true;
            touchGO.AddComponent<JoystickTouchForwarder>().Init(this);
            touchArea.SetAsFirstSibling();
        }

        SetVisualAlpha(0f);
    }

    void DisableFloatingMode()
    {
        if (touchArea != null)
        {
            Destroy(touchArea.gameObject);
            touchArea = null;
            touchAreaImage = null;
        }

        EnsureProceduralRing();

        background.anchorMin = originalAnchorMin;
        background.anchorMax = originalAnchorMax;
        background.anchoredPosition = originalBackgroundPos;
        background.sizeDelta = originalBackgroundSize;

        if (handle != null)
        {
            handle.SetParent(originalHandleParent != null ? originalHandleParent : background, false);
            handle.anchorMin = originalHandleAnchorMin;
            handle.anchorMax = originalHandleAnchorMax;
            handle.pivot = originalHandlePivot;
            handle.sizeDelta = originalHandleSize;
            handle.anchoredPosition = originalHandlePosition;
            if (handleImage != null) handleImage.raycastTarget = true;
        }

        if (backgroundImage != null)
        {
            backgroundImage.enabled = true;
            backgroundImage.raycastTarget = true;
        }

        SetVisualAlpha(joystickAlpha);
    }

    void LoadSettings()
    {
        isFloating = PlayerPrefs.GetInt("FloatingJoystick", 0) == 1;
    }

    void EnsureProceduralRing()
    {
        if (backgroundImage == null)
            backgroundImage = background.GetComponent<Image>();

        if (visualCircle != null) return;

        GameObject ringGO = new GameObject("JoystickRing");
        ringGO.transform.SetParent(background, false);
        visualCircle = ringGO.AddComponent<RectTransform>();
        visualCircle.anchorMin = Vector2.zero;
        visualCircle.anchorMax = Vector2.one;
        visualCircle.offsetMin = Vector2.zero;
        visualCircle.offsetMax = Vector2.zero;
        visualCircle.pivot = new Vector2(0.5f, 0.5f);
        visualCircle.SetAsFirstSibling();
        visualCircleImage = ringGO.AddComponent<JoystickRingGraphic>();
        visualCircleImage.color = new Color(1f, 0.55f, 0.1f, Mathf.Max(joystickAlpha, 0.55f));
        visualCircleImage.raycastTarget = false;
    }

    private class JoystickTouchForwarder : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private VirtualJoystick joystick;

        public void Init(VirtualJoystick source)
        {
            joystick = source;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            joystick?.OnPointerDown(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            joystick?.OnDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            joystick?.OnPointerUp(eventData);
        }
    }
}