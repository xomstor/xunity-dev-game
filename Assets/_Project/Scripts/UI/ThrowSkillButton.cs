using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ThrowSkillButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("References")]
    public Button button;
    public PlayerThrowAbility throwAbility;

    [Header("Icon")]
    [Tooltip("Optional icon to show on the button.")]
    public Sprite icon;
    [Tooltip("Optional Image component to use. If empty, the button's current Image is used.")]
    public Image iconImage;

    void Start()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (iconImage == null)
        {
            if (button != null)
                iconImage = button.targetGraphic as Image;
            if (iconImage == null)
                iconImage = GetComponent<Image>();
        }

        if (throwAbility == null)
        {
            PlayerController pc = FindAnyObjectByType<PlayerController>();
            if (pc != null)
                throwAbility = pc.GetComponent<PlayerThrowAbility>();
        }

        if (icon != null && iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
        }
        else if (iconImage != null)
        {
            iconImage.sprite = CreateCircleSprite();
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
        }

        if (button != null)
            button.onClick.RemoveAllListeners();

        UpdateVisibility();
        PlayerSkillInstance.OnSelectionChanged += OnSkillSelectionChanged;
    }

    void OnDestroy()
    {
        PlayerSkillInstance.OnSelectionChanged -= OnSkillSelectionChanged;
        throwAbility?.SetInputHeld(false);
    }

    void OnSkillSelectionChanged(PlayerSkillInstance changedSkill)
    {
        if (throwAbility != null && changedSkill == throwAbility)
            UpdateVisibility();
    }

    void UpdateVisibility()
    {
        bool visible = throwAbility != null && throwAbility.IsSelected;
        gameObject.SetActive(visible);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        throwAbility?.SetInputHeld(true);
        throwAbility?.TryUse();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        throwAbility?.SetInputHeld(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        throwAbility?.SetInputHeld(false);
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
}
