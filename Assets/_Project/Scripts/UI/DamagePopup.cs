using UnityEngine;
using TMPro;
using System.Collections;

public class DamagePopup : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    public float fadeDuration = 1.5f;
    public float moveDistance = 30f;
    public Vector2 randomOffset = new Vector2(10f, 10f);

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 startPosition;
    private Vector2 targetPosition;
    private float elapsed;

    public void Setup(int damage, bool isCrit, Color color)
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("DamagePopup: RectTransform is missing!");
            return;
        }

        if (textMesh == null)
            textMesh = GetComponentInChildren<TextMeshProUGUI>();
        if (textMesh == null)
        {
            Debug.LogError("DamagePopup: TextMeshProUGUI is missing!");
            return;
        }
        if (textMesh.font == null)
            Debug.LogError("DamagePopup: TextMeshProUGUI has no font assigned!");

        textMesh.gameObject.SetActive(true);
        textMesh.enabled = true;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        textMesh.text = damage.ToString();
        textMesh.color = color;
        textMesh.fontSize = isCrit ? 56 : 42;
        textMesh.outlineColor = Color.black;
        textMesh.outlineWidth = 0.25f;
        if (isCrit)
            textMesh.text += "!";

        startPosition = rectTransform.anchoredPosition;
        Vector2 randomPos = new Vector2(
            Random.Range(-randomOffset.x, randomOffset.x),
            Random.Range(-randomOffset.y, randomOffset.y));
        targetPosition = startPosition + Vector2.up * moveDistance + randomPos;

        elapsed = 0f;
        StartCoroutine(Animate());
    }

    IEnumerator Animate()
    {
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            canvasGroup.alpha = 1f - t;

            yield return null;
        }

        Destroy(gameObject);
    }
}
