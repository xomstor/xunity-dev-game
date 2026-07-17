using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SkillSlotButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public int slotIndex;
    public PlayerSkillsManager skillsManager;
    public Image iconImage;
    public Image cooldownFill;
    public CanvasGroup canvasGroup;
    public bool useCircularVisual = true;
    public Color circleColor = new Color(0.12f, 0.2f, 0.28f, 0.92f);
    public float buttonSize = 110f;

    private Button button;
    private bool isEditing;
    private PlayerSkillInstance skill;

    void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.transition = Selectable.Transition.None;
            button.interactable = true;
        }
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        if (skillsManager == null) skillsManager = FindAnyObjectByType<PlayerSkillsManager>();
        EnsureChildImages();
        ApplyCircularVisual();
        ApplySquareLayout();
    }

    void EnsureChildImages()
    {
        if (iconImage == null)
        {
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(transform, false);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(8f, 8f);
            iconRect.offsetMax = new Vector2(-8f, -8f);
            iconImage = iconObj.GetComponent<Image>();
            iconImage.raycastTarget = false;
        }

        if (cooldownFill == null)
        {
            GameObject cdObj = new GameObject("CooldownFill", typeof(RectTransform), typeof(Image));
            cdObj.transform.SetParent(transform, false);
            RectTransform cdRect = cdObj.GetComponent<RectTransform>();
            cdRect.anchorMin = Vector2.zero;
            cdRect.anchorMax = Vector2.one;
            cdRect.offsetMin = Vector2.zero;
            cdRect.offsetMax = Vector2.zero;
            cooldownFill = cdObj.GetComponent<Image>();
            cooldownFill.sprite = CreateCircleSprite();
            cooldownFill.type = Image.Type.Filled;
            cooldownFill.fillMethod = Image.FillMethod.Radial360;
            cooldownFill.fillOrigin = (int)Image.Origin360.Top;
            cooldownFill.fillClockwise = true;
            cooldownFill.fillAmount = 0f;
            cooldownFill.color = new Color(0f, 0f, 0f, 0.65f);
            cooldownFill.raycastTarget = false;
        }
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

    void ApplyCircularVisual()
    {
        if (!useCircularVisual) return;
        Image background = button != null ? button.targetGraphic as Image : GetComponent<Image>();
        if (background == null) background = GetComponent<Image>();
        if (background == null) return;
        background.sprite = CreateCircleSprite();
        background.type = Image.Type.Simple;
        background.color = circleColor;
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

    void OnEnable()
    {
        if (skillsManager == null) skillsManager = FindAnyObjectByType<PlayerSkillsManager>();
        if (skillsManager != null) skillsManager.OnChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (skillsManager != null) skillsManager.OnChanged -= Refresh;
    }

    void Update()
    {
        if (skillsManager == null)
        {
            skillsManager = FindAnyObjectByType<PlayerSkillsManager>();
            if (skillsManager != null)
            {
                skillsManager.OnChanged += Refresh;
                Refresh();
            }
            return;
        }
        if (cooldownFill == null) return;
        if (skill == null)
        {
            cooldownFill.gameObject.SetActive(false);
            return;
        }
        float progress = skill.CooldownProgress;
        bool onCooldown = progress > 0f;
        cooldownFill.gameObject.SetActive(onCooldown);
        cooldownFill.fillAmount = progress;
        if (iconImage != null)
            iconImage.color = onCooldown ? new Color(0.5f, 0.5f, 0.5f, 1f) : Color.white;
    }

    public void Refresh()
    {
        if (skillsManager == null)
        {
            skillsManager = FindAnyObjectByType<PlayerSkillsManager>();
            if (skillsManager != null) skillsManager.OnChanged += Refresh;
        }
        if (skillsManager == null) return;
        skill = skillsManager.GetSlot(slotIndex);
        bool enabled = skillsManager.IsSlotEnabled(slotIndex) && skill != null;

        canvasGroup.alpha = enabled ? 1f : 0.35f;
        canvasGroup.blocksRaycasts = enabled || isEditing;
        if (button != null)
        {
            button.interactable = true;
            button.transition = Selectable.Transition.None;
        }
        if (iconImage != null)
        {
            iconImage.sprite = skill != null && skill.Data != null ? skill.Data.icon : null;
            iconImage.preserveAspect = true;
            iconImage.enabled = iconImage.sprite != null;
        }
    }

    public void SetEditMode(bool value)
    {
        isEditing = value;
        Refresh();
    }

    public void SetButtonSize(float size)
    {
        buttonSize = Mathf.Clamp(size, 72f, 180f);
        ApplySquareLayout();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isEditing && skill != null)
            skillsManager?.TryUseSlot(slotIndex);
    }

    public void OnPointerUp(PointerEventData eventData) { }
    public void OnPointerExit(PointerEventData eventData) { }
}
