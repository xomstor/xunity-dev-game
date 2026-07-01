using UnityEngine;

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

        Vector2 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
        GameObject popup = Instantiate(damagePopupPrefab, popupContainer != null ? popupContainer : transform);
        popup.transform.position = screenPosition;

        RectTransform rectTransform = popup.GetComponent<RectTransform>();
        if (rectTransform != null)
            rectTransform.position = screenPosition;

        DamagePopup damagePopup = popup.GetComponent<DamagePopup>();
        if (damagePopup != null)
        {
            Color color = isCrit ? critColor : (isPlayer ? playerDamageColor : enemyDamageColor);
            damagePopup.Setup(damage, isCrit, color);
        }
    }
}
