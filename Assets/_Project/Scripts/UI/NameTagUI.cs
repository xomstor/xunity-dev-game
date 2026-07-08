using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

public class NameTagUI : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public AutoCombat targetCombat;

    [Header("UI")]
    public TextMeshProUGUI nameText;

    [Header("Settings")]
    public Vector3 worldOffset = new Vector3(0, 2.5f, 0);
    public float showDistance = 3f;

    private Camera mainCamera;
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private Transform player;
    private string originalText;

    void Start()
    {
        mainCamera = Camera.main;
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

        AutoCombat[] allCombatants = FindObjectsByType<AutoCombat>(FindObjectsInactive.Exclude);
        foreach (var combatant in allCombatants)
        {
            if (combatant.team == CombatTeam.Player)
            {
                player = combatant.transform;
                break;
            }
        }

        if (nameText != null)
        {
            originalText = nameText.text;
            if (targetCombat != null)
            {
                // Убираем цифры в скобках из имени врага
                string cleanName = Regex.Replace(targetCombat.name, @"\s*\(\d+\)\s*", "").Trim();
                nameText.text = cleanName;
            }
        }

        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    void LateUpdate()
    {
        if (targetCombat == null || targetCombat.IsDead)
        {
            if (nameText != null)
                nameText.gameObject.SetActive(false);
            return;
        }

        if (target == null) return;

        if (mainCamera != null && rectTransform != null && parentCanvas != null)
        {
            Vector3 worldPosition = target.position + worldOffset;
            Vector2 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                screenPosition,
                parentCanvas.worldCamera,
                out localPoint))
            {
                rectTransform.localPosition = localPoint;
            }
        }
    }

    public void Show()
    {
        if (nameText != null)
            nameText.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (nameText != null)
            nameText.gameObject.SetActive(false);
    }
}
