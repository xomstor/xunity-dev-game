using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("References")]
    public RectTransform background;
    public RectTransform handle;

    [Header("Settings")]
    public float handleRange = 1f;

    public Vector2 Direction { get; private set; }
    public bool IsActive { get; private set; }

    private Vector2 _startPos;
    private Canvas _canvas;

    private void Start()
    {
        _canvas = GetComponentInParent<Canvas>();
        _startPos = background.anchoredPosition;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        IsActive = true;
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 screenPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, _canvas.worldCamera, out screenPos);

        float radius = background.sizeDelta.x * 0.5f * handleRange;
        Vector2 clamped = Vector2.ClampMagnitude(screenPos, radius);
        handle.anchoredPosition = clamped;
        Direction = clamped / radius;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsActive = false;
        Direction = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
    }
}
