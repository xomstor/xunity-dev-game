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
    private RectTransform visualCircle;
    private Image visualCircleImage;
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

        ApplyTransparency();
        LoadSettings();
        if (isFloating) SetupFloatingMode();
    }

    // <-- Добавляем Update для непрерывной отправки ввода
    void Update()
    {
        if (isHolding && player != null)
        {
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
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isHolding = true;
        SetVisualAlpha(joystickActiveAlpha);

        if (isFloating && visualCircle != null)
        {
            RectTransform parentRt = background.parent as RectTransform;
            if (parentRt != null)
            {
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRt, eventData.position, uiCamera, out localPoint))
                {
                    visualCircle.anchoredPosition = localPoint;
                    visualCircle.gameObject.SetActive(true);
                    visualCircle.SetAsLastSibling();
                }
            }
        }

        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransform dragRef = isFloating ? visualCircle : background;
        if (dragRef == null) dragRef = background;

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragRef, eventData.position, uiCamera, out localPoint))
        {
            Vector2 delta = Vector2.ClampMagnitude(localPoint, handleRange);
            handle.anchoredPosition = delta;
            input = delta / handleRange;

            float horizontal = Mathf.Abs(input.x) > deadZone ? input.x : 0f;
            if (player != null) player.SetMoveInput(horizontal);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;
        input = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
        SetVisualAlpha(joystickAlpha);

        if (isFloating && visualCircle != null)
            visualCircle.gameObject.SetActive(false);

        jumpTriggered = false;
        if (wasCrouching)
        {
            wasCrouching = false;
            player?.StopCrouch();
        }

        if (player != null) player.SetMoveInput(0f);
    }

    public Vector2 GetInput() => input;

    void ApplyTransparency()
    {
        SetVisualAlpha(joystickAlpha);
    }

    void SetVisualAlpha(float alpha)
    {
        if (!isFloating)
        {
            if (backgroundImage != null)
            {
                Color c = backgroundImage.color;
                c.a = alpha;
                backgroundImage.color = c;
            }
        }
        else
        {
            if (visualCircleImage != null)
            {
                Color c = visualCircleImage.color;
                c.a = alpha;
                visualCircleImage.color = c;
            }
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
        Debug.Log($"[VirtualJoystick] SetFloating({enabled}) — isFloating now = {isFloating}");

        if (enabled)
            SetupFloatingMode();
        else
            DisableFloatingMode();
    }

    void SetupFloatingMode()
    {
        if (visualCircle != null) return;

        RectTransform parentRt = background.parent as RectTransform;
        if (parentRt != null)
        {
            background.anchorMin = new Vector2(0, 0);
            background.anchorMax = new Vector2(1, 1);
            background.offsetMin = Vector2.zero;
            background.offsetMax = Vector2.zero;
            background.pivot = new Vector2(0.5f, 0.5f);
        }

        // Ensure background has an Image for raycast, make it fully transparent
        if (backgroundImage == null)
            backgroundImage = background.GetComponent<Image>();
        if (backgroundImage == null)
            backgroundImage = background.gameObject.AddComponent<Image>();
        backgroundImage.color = new Color(0, 0, 0, 0);
        backgroundImage.raycastTarget = true;

        // Hide ALL other images under background except handleImage
        Image[] allImages = background.GetComponentsInChildren<Image>(true);
        foreach (Image img in allImages)
        {
            if (img == backgroundImage) continue;
            if (img == handleImage) continue;
            Color c = img.color;
            c.a = 0;
            img.color = c;
            img.raycastTarget = false;
        }

        // Create visual circle (ring) as sibling of background, same size as original
        GameObject visualGO = new GameObject("VisualCircle");
        visualGO.transform.SetParent(background.parent, false);
        visualCircle = visualGO.AddComponent<RectTransform>();
        visualCircle.anchorMin = new Vector2(0.5f, 0.5f);
        visualCircle.anchorMax = new Vector2(0.5f, 0.5f);
        visualCircle.pivot = new Vector2(0.5f, 0.5f);
        visualCircle.sizeDelta = originalRingSize != Vector2.zero ? originalRingSize : originalBackgroundSize;
        visualCircle.anchoredPosition = originalBackgroundPos;
        Image vImg = visualGO.AddComponent<Image>();
        vImg.sprite = originalBackgroundSprite;
        vImg.color = new Color(1, 1, 1, joystickAlpha);
        vImg.preserveAspect = true;
        vImg.raycastTarget = false;
        visualCircleImage = vImg;

        // Move handle to visualCircle, preserve original size
        if (handle != null)
        {
            handle.SetParent(visualCircle, false);
            handle.anchorMin = new Vector2(0.5f, 0.5f);
            handle.anchorMax = new Vector2(0.5f, 0.5f);
            handle.pivot = new Vector2(0.5f, 0.5f);
            handle.sizeDelta = originalHandleSize;
            handle.anchoredPosition = Vector2.zero;
            if (handleImage != null) handleImage.raycastTarget = false;
        }

        visualGO.transform.SetAsLastSibling();
        visualGO.SetActive(false);
        Debug.Log("[VirtualJoystick] Floating mode setup complete");
    }

    void DisableFloatingMode()
    {
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

        if (visualCircle != null)
        {
            Destroy(visualCircle.gameObject);
            visualCircle = null;
            visualCircleImage = null;
        }

        background.anchorMin = originalAnchorMin;
        background.anchorMax = originalAnchorMax;
        background.anchoredPosition = originalBackgroundPos;
        background.sizeDelta = originalBackgroundSize;

        // Restore alpha on background and all child images (except handle, handled by SetVisualAlpha)
        Image[] allImages = background.GetComponentsInChildren<Image>(true);
        foreach (Image img in allImages)
        {
            if (img == handleImage) continue;
            Color c = img.color;
            c.a = joystickAlpha;
            img.color = c;
            img.raycastTarget = true;
        }

        Debug.Log("[VirtualJoystick] Floating mode disabled");
    }

    void LoadSettings()
    {
        isFloating = PlayerPrefs.GetInt("FloatingJoystick", 0) == 1;
    }
}