using UnityEngine;
using UnityEngine.EventSystems;

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

    private Vector2 input = Vector2.zero;
    private Camera uiCamera;

    void Awake()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (background == null) background = GetComponent<RectTransform>();
        if (handle == null) handle = transform.GetChild(0).GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, uiCamera, out localPoint);

        Vector2 delta = Vector2.ClampMagnitude(localPoint, handleRange);
        handle.anchoredPosition = delta;

        input = delta / handleRange;

        float horizontal = Mathf.Abs(input.x) > deadZone ? input.x : 0f;
        if (player != null) player.SetMoveInput(horizontal);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        input = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
        if (player != null) player.SetMoveInput(0f);
    }

    public Vector2 GetInput() => input;
}
