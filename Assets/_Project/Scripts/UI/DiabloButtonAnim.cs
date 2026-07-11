using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class DiabloButtonAnim : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private RectTransform rectTransform;
    private Image backgroundImage;
    private Image borderImage;
    private Image accentImage;

    private Color baseBgColor;
    private Color baseBorderColor;
    private Color baseAccentColor;

    public Color hoverBgColor = new Color(0.25f, 0.08f, 0.04f, 0.95f);
    public Color hoverBorderColor = new Color(0.9f, 0.7f, 0.2f, 1f);
    public Color hoverAccentColor = new Color(1f, 0.25f, 0.05f, 1f);

    public float hoverScale = 1.03f;
    public float pressedScale = 0.95f;
    public float animSpeed = 12f;

    private bool isHovering;
    private float targetScale = 1f;
    private float currentScale = 1f;
    private Color targetBgColor;
    private Color targetBorderColor;
    private Color targetAccentColor;

    public void Init(RectTransform rt)
    {
        rectTransform = rt;
        backgroundImage = transform.Find("Background")?.GetComponent<Image>();
        borderImage = transform.Find("Border")?.GetComponent<Image>();
        accentImage = transform.Find("BottomAccent")?.GetComponent<Image>();

        if (backgroundImage != null) baseBgColor = targetBgColor = backgroundImage.color;
        if (borderImage != null) baseBorderColor = targetBorderColor = borderImage.color;
        if (accentImage != null) baseAccentColor = targetAccentColor = accentImage.color;
    }

    void Update()
    {
        currentScale = Mathf.Lerp(currentScale, targetScale, Time.unscaledDeltaTime * animSpeed);
        if (rectTransform != null)
            rectTransform.localScale = new Vector3(currentScale, currentScale, 1f);

        if (backgroundImage != null)
            backgroundImage.color = Color.Lerp(backgroundImage.color, targetBgColor, Time.unscaledDeltaTime * animSpeed);
        if (borderImage != null)
            borderImage.color = Color.Lerp(borderImage.color, targetBorderColor, Time.unscaledDeltaTime * animSpeed);
        if (accentImage != null)
            accentImage.color = Color.Lerp(accentImage.color, targetAccentColor, Time.unscaledDeltaTime * animSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        targetScale = hoverScale;
        targetBgColor = hoverBgColor;
        targetBorderColor = hoverBorderColor;
        targetAccentColor = hoverAccentColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        targetScale = 1f;
        targetBgColor = baseBgColor;
        targetBorderColor = baseBorderColor;
        targetAccentColor = baseAccentColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = isHovering ? hoverScale : 1f;
    }
}
