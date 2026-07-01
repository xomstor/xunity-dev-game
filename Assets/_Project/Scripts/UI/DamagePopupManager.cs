using UnityEngine;
using UnityEngine.UI;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance { get; private set; }

    [Header("UI")]
    public Canvas canvas;
    public GameObject damagePopupPrefab;
    public Transform popupContainer;

    [Header("Colors")]
    public Color playerDamageColor = Color.white;
    public Color enemyDamageColor = Color.yellow;
    public Color critColor = Color.red;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (canvas == null)
            canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = FindAnyObjectByType<Canvas>();

        // Damage popups need a Screen Space Overlay canvas to render correctly
        if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            GameObject canvasGO = new GameObject("DamagePopupCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            Debug.Log("DamagePopupManager: created dedicated ScreenSpaceOverlay canvas.");
        }
    }

    public void ShowDamage(Vector3 worldPosition, int damage, bool isCrit, bool isPlayer)
    {
        if (damagePopupPrefab == null)
        {
            Debug.LogError("DamagePopupManager: damagePopupPrefab is not assigned!");
            return;
        }
        if (Camera.main == null)
        {
            Debug.LogError("DamagePopupManager: no main camera found!");
            return;
        }
        if (canvas == null)
        {
            Debug.LogError("DamagePopupManager: canvas is not assigned!");
            return;
        }
        if (canvas.renderMode == RenderMode.WorldSpace)
            Debug.LogWarning("DamagePopupManager: canvas is in WorldSpace. Popups may not render correctly.");
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
        {
            Debug.LogWarning("DamagePopupManager: ScreenSpaceCamera canvas has no worldCamera. Using Camera.main.");
            canvas.worldCamera = Camera.main;
        }

        Vector2 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
        Transform parent = popupContainer != null ? popupContainer : canvas.transform;
        GameObject popup = Instantiate(damagePopupPrefab, parent);
        popup.SetActive(true);

        RectTransform rectTransform = popup.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                screenPosition,
                canvas.worldCamera,
                out localPoint))
            {
                rectTransform.anchoredPosition = localPoint;
            }
            else
            {
                rectTransform.anchoredPosition = screenPosition;
            }
        }

        DamagePopup damagePopup = popup.GetComponent<DamagePopup>();
        if (damagePopup != null)
        {
            Color color = isCrit ? critColor : (isPlayer ? playerDamageColor : enemyDamageColor);
            damagePopup.Setup(damage, isCrit, color);
        }
    }
}
