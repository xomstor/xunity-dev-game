using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BlockButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public PlayerController player;
    public float buttonSize = 110f;
    public Color circleColor = new Color(0.15f, 0.22f, 0.3f, 0.92f);

    private bool isEditing;
    private CanvasGroup canvasGroup;
    private Image iconImage;
    private SkillSlotLayoutController dragController;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        EnsureIconChild();
        ApplyVisual();
        ApplySquareLayout();
    }

    void EnsureIconChild()
    {
        Transform existing = transform.Find("Icon");
        if (existing != null)
        {
            iconImage = existing.GetComponent<Image>();
            if (iconImage != null) return;
        }
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(transform, false);
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(10f, 10f);
        iconRect.offsetMax = new Vector2(-10f, -10f);
        iconImage = iconObj.GetComponent<Image>();
        iconImage.raycastTarget = false;
        iconImage.preserveAspect = true;
        iconImage.color = Color.white;
    }

    void ApplyVisual()
    {
        Image bg = GetComponent<Image>();
        if (bg == null) bg = gameObject.AddComponent<Image>();
        bg.sprite = CreateCircleSprite();
        bg.type = Image.Type.Simple;
        bg.color = circleColor;
    }

    void ApplySquareLayout()
    {
        RectTransform rect = transform as RectTransform;
        if (rect == null) return;
        rect.sizeDelta = new Vector2(buttonSize, buttonSize);
        LayoutElement layout = GetComponent<LayoutElement>();
        if (layout == null) layout = gameObject.AddComponent<LayoutElement>();
        layout.minWidth = buttonSize;
        layout.minHeight = buttonSize;
        layout.preferredWidth = buttonSize;
        layout.preferredHeight = buttonSize;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 0f;
    }

    static Sprite CreateCircleSprite(int resolution = 128)
    {
        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[resolution * resolution];
        Vector2 center = new Vector2((resolution - 1) * 0.5f, (resolution - 1) * 0.5f);
        float radius = resolution * 0.5f - 1f;
        for (int y = 0; y < resolution; y++)
        for (int x = 0; x < resolution; x++)
        {
            float distance = Vector2.Distance(new Vector2(x, y), center);
            float alpha = Mathf.Clamp01(radius + 1f - distance);
            pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, resolution, resolution), new Vector2(0.5f, 0.5f), 100f);
    }

    void Start()
    {
        if (player == null)
            player = FindAnyObjectByType<PlayerController>();
    }

    public void SetEditMode(bool value)
    {
        isEditing = value;
        if (canvasGroup != null)
            canvasGroup.alpha = value ? 0.8f : 1f;
        if (dragController == null)
            dragController = GetComponent<SkillSlotLayoutController>();
    }

    public void SetButtonSize(float size)
    {
        buttonSize = Mathf.Clamp(size, 72f, 180f);
        ApplySquareLayout();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isEditing) return;
        if (!ResolvePlayer()) return;
        player.StartBlock();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isEditing) return;
        if (!ResolvePlayer()) return;
        player.StopBlock();
    }

    bool ResolvePlayer()
    {
        if (player == null)
            player = FindAnyObjectByType<PlayerController>();
        return player != null;
    }
}
