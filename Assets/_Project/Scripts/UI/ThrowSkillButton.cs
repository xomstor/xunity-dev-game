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
}
