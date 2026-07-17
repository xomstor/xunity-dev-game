using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillHudLayoutController : MonoBehaviour, IDragHandler, IEndDragHandler
{
    public const int ActiveSlotCount = 4;
    public float defaultButtonSize = 110f;
    public float minimumButtonSize = 72f;
    public float maximumButtonSize = 180f;

    private RectTransform groupRect;
    private RectTransform canvasRect;
    private SkillSlotButton[] slots = new SkillSlotButton[0];
    private BlockButton blockButton;
    private SkillSlotLayoutController blockDrag;
    private bool editMode;
    private float buttonSize;
    private Vector2 defaultPosition;
    public float ButtonSize => buttonSize;
    public bool IsEditing => editMode;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        SkillHudLayoutController[] existing = FindObjectsByType<SkillHudLayoutController>(FindObjectsInactive.Include);
        if (existing.Length > 0) return;

        SkillSlotButton[] found = FindObjectsByType<SkillSlotButton>(FindObjectsInactive.Include);
        SkillSlotButton first = found.Length > 0 ? found[0] : null;
        if (first == null) return;
        Transform parent = first.transform.parent;
        if (parent == null) return;
        if (parent.GetComponent<SkillHudLayoutController>() != null) return;
        parent.gameObject.AddComponent<SkillHudLayoutController>();
    }

    void Awake()
    {
        groupRect = transform as RectTransform;
        canvasRect = GetComponentInParent<Canvas>()?.transform as RectTransform;
        buttonSize = PlayerPrefs.GetFloat("EirHold_SkillHud_Size", defaultButtonSize);
        defaultPosition = groupRect != null ? groupRect.anchoredPosition : Vector2.zero;
        ConfigureGroup();
        SetEditMode(false);
    }

    bool initialized;

    void Start()
    {
        HideLegacyFireballButton();
        EnsureFourSlots();
        if (canvasRect != null && canvasRect.rect.width > 0 && canvasRect.rect.height > 0)
        {
            RefreshSlots();
            SetupBlockButton();
            ApplySize(buttonSize);
        }
        else
        {
            StartCoroutine(DeferredStartRefresh());
        }
        initialized = true;
    }

    System.Collections.IEnumerator DeferredStartRefresh()
    {
        for (int i = 0; i < 10; i++)
        {
            yield return null;
            canvasRect = GetComponentInParent<Canvas>()?.transform as RectTransform;
            if (canvasRect != null && canvasRect.rect.width > 0 && canvasRect.rect.height > 0)
                break;
        }
        RefreshSlots();
        SetupBlockButton();
        ApplySize(buttonSize);
    }

    void OnEnable()
    {
        if (!initialized) return;
        canvasRect = GetComponentInParent<Canvas>()?.transform as RectTransform;
        if (canvasRect != null && canvasRect.rect.width > 0 && canvasRect.rect.height > 0)
        {
            RefreshSlots();
            SetupBlockButton();
            ApplySize(buttonSize);
        }
        else
        {
            StartCoroutine(DeferredRefresh());
        }
    }

    System.Collections.IEnumerator DeferredRefresh()
    {
        for (int i = 0; i < 10; i++)
        {
            yield return null;
            canvasRect = GetComponentInParent<Canvas>()?.transform as RectTransform;
            if (canvasRect != null && canvasRect.rect.width > 0 && canvasRect.rect.height > 0)
                break;
        }
        RefreshSlots();
        SetupBlockButton();
        ApplySize(buttonSize);
    }

    public void ForceRefresh()
    {
        canvasRect = GetComponentInParent<Canvas>()?.transform as RectTransform;
        RefreshSlots();
        SetupBlockButton();
        ApplySize(buttonSize);
    }

    void ConfigureGroup()
    {
        if (groupRect == null) return;
        HorizontalLayoutGroup layout = GetComponent<HorizontalLayoutGroup>();
        if (layout != null) layout.enabled = false;
        groupRect.anchorMin = Vector2.zero;
        groupRect.anchorMax = Vector2.one;
        groupRect.pivot = new Vector2(0.5f, 0.5f);
        groupRect.offsetMin = Vector2.zero;
        groupRect.offsetMax = Vector2.zero;
        groupRect.anchoredPosition = Vector2.zero;
    }

    void EnsureFourSlots()
    {
        // Respect the slots already placed in the scene; do not spawn duplicates.
        RefreshSlots();
    }

    void RefreshSlots()
    {
        SkillSlotButton[] found = GetComponentsInChildren<SkillSlotButton>(true);
        System.Array.Sort(found, (a, b) => a.slotIndex.CompareTo(b.slotIndex));
        // Destroy any extra slots above the expected count
        if (found.Length > ActiveSlotCount)
        {
            Debug.LogWarning($"[SkillHudLayoutController] RefreshSlots: found {found.Length} slots, trimming to {ActiveSlotCount}");
            for (int i = found.Length - 1; i >= ActiveSlotCount; i--)
            {
                if (found[i] != null && found[i].gameObject != null)
                {
                    Debug.LogWarning($"[SkillHudLayoutController] Destroying extra slot {found[i].name} at index {i}");
                    DestroyImmediate(found[i].gameObject);
                }
            }
            found = GetComponentsInChildren<SkillSlotButton>(true);
            System.Array.Sort(found, (a, b) => a.slotIndex.CompareTo(b.slotIndex));
        }
        slots = found;
        Debug.Log($"[SkillHudLayoutController] RefreshSlots: found {slots.Length} slot(s) on {gameObject.name}: " + string.Join(", ", System.Array.ConvertAll(slots, s => s != null ? s.name : "null")));
        bool canvasReady = canvasRect != null && canvasRect.rect.width > 0 && canvasRect.rect.height > 0;
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].slotIndex = i;
            RectTransform slotRect = slots[i].transform as RectTransform;
            if (slotRect != null)
            {
                slotRect.anchorMin = new Vector2(0.5f, 0.5f);
                slotRect.anchorMax = new Vector2(0.5f, 0.5f);
                slotRect.pivot = new Vector2(0.5f, 0.5f);
                SkillSlotLayoutController drag = slots[i].GetComponent<SkillSlotLayoutController>();
                if (drag == null) drag = slots[i].gameObject.AddComponent<SkillSlotLayoutController>();
                drag.Configure(slotRect, canvasRect, i, canvasReady ? GetDefaultSlotPosition(i) : Vector2.zero, gameObject.name);
                drag.SetEditable(editMode);
            }
            slots[i].Refresh();
        }
        // Log positions after layout
        System.Text.StringBuilder sb = new System.Text.StringBuilder("[SkillHudLayoutController] slot positions:");
        for (int i = 0; i < slots.Length; i++)
        {
            RectTransform r = slots[i].transform as RectTransform;
            if (r != null) sb.Append($" {slots[i].name}={r.anchoredPosition}");
        }
        Debug.Log(sb.ToString());
    }

    Vector2 GetDefaultSlotPosition(int index)
    {
        if (canvasRect == null) return Vector2.zero;
        float x = canvasRect.rect.xMax - 90f - (ActiveSlotCount - 1 - index) * (buttonSize + 14f);
        float y = canvasRect.rect.yMin + 100f;
        return new Vector2(x, y);
    }

    void HideLegacyFireballButton()
    {
        ThrowSkillButton[] legacyButtons = FindObjectsByType<ThrowSkillButton>(FindObjectsInactive.Include);
        foreach (ThrowSkillButton legacy in legacyButtons)
            if (legacy != null)
                legacy.gameObject.SetActive(false);
    }

    public void BeginEdit()
    {
        editMode = true;
        SetupBlockButton();
        SetEditMode(true);
    }

    public void FinishEdit()
    {
        editMode = false;
        SetEditMode(false);
        SaveLayout();
    }

    public void SetEditMode(bool value)
    {
        editMode = value;
        if (slots == null) RefreshSlots();
        foreach (SkillSlotButton slot in slots)
        {
            if (slot == null) continue;
            slot.SetEditMode(value);
            SkillSlotLayoutController drag = slot.GetComponent<SkillSlotLayoutController>();
            if (drag != null) drag.SetEditable(value);
        }
        if (blockButton != null) blockButton.SetEditMode(value);
        if (blockDrag != null) blockDrag.SetEditable(value);
    }

    public void ApplySize(float size)
    {
        buttonSize = Mathf.Clamp(size, minimumButtonSize, maximumButtonSize);
        if (slots == null) RefreshSlots();
        foreach (SkillSlotButton slot in slots)
            if (slot != null) slot.SetButtonSize(buttonSize);
        if (blockButton != null) blockButton.SetButtonSize(buttonSize);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!editMode || groupRect == null || canvasRect == null) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint)) return;
        float halfWidth = groupRect.rect.width * 0.5f;
        float halfHeight = groupRect.rect.height * 0.5f;
        Rect safe = Screen.safeArea;
        Vector2 safeMin = ScreenPointToCanvasPoint(safe.min);
        Vector2 safeMax = ScreenPointToCanvasPoint(safe.max);
        groupRect.anchoredPosition = new Vector2(
            Mathf.Clamp(localPoint.x, safeMin.x + halfWidth, safeMax.x - halfWidth),
            Mathf.Clamp(localPoint.y, safeMin.y + halfHeight, safeMax.y - halfHeight));
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (editMode) SaveLayout();
    }

    Vector2 ScreenPointToCanvasPoint(Vector2 screenPoint)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 point);
        return point;
    }

    public void ResetLayout()
    {
        ApplySize(defaultButtonSize);
        if (slots == null) RefreshSlots();
        foreach (SkillSlotButton slot in slots)
        {
            if (slot == null) continue;
            SkillSlotLayoutController drag = slot.GetComponent<SkillSlotLayoutController>();
            if (drag != null) drag.ResetPosition();
        }
        if (blockDrag != null) blockDrag.ResetPosition();
        SaveLayout();
    }

    void SaveLayout()
    {
        if (groupRect == null || canvasRect == null) return;
        PlayerPrefs.SetFloat("EirHold_SkillHud_Size", buttonSize);
        PlayerPrefs.Save();
    }

    void LoadPosition()
    {
        if (groupRect == null || canvasRect == null || !PlayerPrefs.HasKey("EirHold_SkillHud_X")) return;
        groupRect.anchoredPosition = new Vector2(
            Mathf.Lerp(canvasRect.rect.xMin, canvasRect.rect.xMax, PlayerPrefs.GetFloat("EirHold_SkillHud_X")),
            Mathf.Lerp(canvasRect.rect.yMin, canvasRect.rect.yMax, PlayerPrefs.GetFloat("EirHold_SkillHud_Y")));
    }

    void SetupBlockButton()
    {
        BlockButton[] found = FindObjectsByType<BlockButton>(FindObjectsInactive.Include);
        blockButton = null;
        foreach (BlockButton candidate in found)
        {
            if (candidate == null) continue;
            if (candidate.GetComponentInParent<Canvas>() != null && candidate.transform is RectTransform)
            {
                blockButton = candidate;
                break;
            }
        }
        if (blockButton == null) return;
        RectTransform blockRect = blockButton.transform as RectTransform;
        if (blockRect == null) return;
        blockRect.anchorMin = new Vector2(0.5f, 0.5f);
        blockRect.anchorMax = new Vector2(0.5f, 0.5f);
        blockRect.pivot = new Vector2(0.5f, 0.5f);
        RectTransform blockCanvasRect = blockButton.GetComponentInParent<Canvas>()?.transform as RectTransform;
        if (blockCanvasRect == null) blockCanvasRect = canvasRect;
        bool blockCanvasReady = blockCanvasRect != null && blockCanvasRect.rect.width > 0 && blockCanvasRect.rect.height > 0;
        blockDrag = blockButton.GetComponent<SkillSlotLayoutController>();
        if (blockDrag == null) blockDrag = blockButton.gameObject.AddComponent<SkillSlotLayoutController>();
        blockDrag.Configure(blockRect, blockCanvasRect, -1, blockCanvasReady ? GetDefaultBlockPosition(blockCanvasRect) : Vector2.zero);
        blockDrag.SetEditable(editMode);
    }

    Vector2 GetDefaultBlockPosition(RectTransform blockCanvasRect)
    {
        if (blockCanvasRect == null) return Vector2.zero;
        float x = blockCanvasRect.rect.xMin + 100f;
        float y = blockCanvasRect.rect.yMin + 100f;
        return new Vector2(x, y);
    }
}
