using UnityEngine;
using UnityEngine.EventSystems;

public class SkillSlotLayoutController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public RectTransform target;
    public RectTransform canvasRect;
    public int slotIndex;
    public bool editable;

    private Vector2 defaultPosition;
    private string positionPrefix;
    private bool configured;

    void Awake()
    {
        if (target == null) target = transform as RectTransform;
        if (canvasRect == null && target != null) canvasRect = target.GetComponentInParent<Canvas>()?.transform as RectTransform;
    }

    public void Configure(RectTransform newTarget, RectTransform newCanvasRect, int newSlotIndex, Vector2 newDefaultPosition, string newPrefix = "SkillSlot")
    {
        if (configured) return;
        target = newTarget;
        canvasRect = newCanvasRect;
        slotIndex = newSlotIndex;
        defaultPosition = newDefaultPosition;
        positionPrefix = string.IsNullOrEmpty(newPrefix) ? "SkillSlot" : newPrefix;
        configured = true;
        LoadPosition();
    }

    public void SetEditable(bool value)
    {
        editable = value;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!editable || target == null || canvasRect == null) return;
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!editable || target == null || canvasRect == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
        Rect safeArea = Screen.safeArea;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, safeArea.min, eventData.pressEventCamera, out Vector2 safeMin);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, safeArea.max, eventData.pressEventCamera, out Vector2 safeMax);
        float halfWidth = target.rect.width * 0.5f;
        float halfHeight = target.rect.height * 0.5f;
        float x = Mathf.Clamp(localPoint.x, safeMin.x + halfWidth, safeMax.x - halfWidth);
        float y = Mathf.Clamp(localPoint.y, safeMin.y + halfHeight, safeMax.y - halfHeight);
        target.anchoredPosition = new Vector2(x, y);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!editable || target == null || canvasRect == null) return;
        SavePosition();
    }

    public void ResetPosition()
    {
        PlayerPrefs.DeleteKey(PositionKeyX);
        PlayerPrefs.DeleteKey(PositionKeyY);
        target.anchoredPosition = defaultPosition;
        PlayerPrefs.Save();
    }

    void SavePosition()
    {
        Vector2 normalized = new Vector2(
            Mathf.InverseLerp(canvasRect.rect.xMin, canvasRect.rect.xMax, target.anchoredPosition.x),
            Mathf.InverseLerp(canvasRect.rect.yMin, canvasRect.rect.yMax, target.anchoredPosition.y));
        PlayerPrefs.SetFloat(PositionKeyX, normalized.x);
        PlayerPrefs.SetFloat(PositionKeyY, normalized.y);
        PlayerPrefs.Save();
    }

    void LoadPosition()
    {
        if (!configured || target == null || canvasRect == null) return;
        if (!PlayerPrefs.HasKey(PositionKeyX))
        {
            target.anchoredPosition = defaultPosition;
            return;
        }
        Vector2 normalized = new Vector2(PlayerPrefs.GetFloat(PositionKeyX), PlayerPrefs.GetFloat(PositionKeyY));
        target.anchoredPosition = new Vector2(
            Mathf.Lerp(canvasRect.rect.xMin, canvasRect.rect.xMax, normalized.x),
            Mathf.Lerp(canvasRect.rect.yMin, canvasRect.rect.yMax, normalized.y));
    }

    string PositionKeyX => $"EirHold_{positionPrefix}_{slotIndex}_X_V2";
    string PositionKeyY => $"EirHold_{positionPrefix}_{slotIndex}_Y_V2";
}
