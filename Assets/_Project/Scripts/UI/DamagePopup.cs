using UnityEngine;
using TMPro;
using System.Collections;

public class DamagePopup : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    public float moveSpeed = 2f;
    public float fadeDuration = 1f;
    public float moveDistance = 2f;
    public Vector3 randomOffset = new Vector3(0.5f, 0.5f, 0f);

    private CanvasGroup canvasGroup;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float elapsed;
    private Color textColor;

    public void Setup(int damage, bool isCrit, Color color)
    {
        if (textMesh == null)
            textMesh = GetComponentInChildren<TextMeshProUGUI>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        textMesh.text = damage.ToString();
        textMesh.color = color;
        textMesh.fontSize = isCrit ? 48 : 36;
        if (isCrit)
            textMesh.text += "!";

        startPosition = transform.position;
        Vector3 randomPos = new Vector3(
            Random.Range(-randomOffset.x, randomOffset.x),
            Random.Range(-randomOffset.y, randomOffset.y),
            0f);
        targetPosition = startPosition + Vector3.up * moveDistance + randomPos;

        elapsed = 0f;
        StartCoroutine(Animate());
    }

    IEnumerator Animate()
    {
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            canvasGroup.alpha = 1f - t;

            yield return null;
        }

        Destroy(gameObject);
    }
}
