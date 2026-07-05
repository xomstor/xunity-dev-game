using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(PlatformEffector2D))]
public class InvisiblePlatform : MonoBehaviour
{
    [Tooltip("Цвет гизмо в редакторе")]
    public Color editorColor = new Color(0f, 1f, 0.5f, 0.35f);

    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0)
            gameObject.layer = groundLayer;

        boxCollider = GetComponent<BoxCollider2D>();
        SetupOneWay();

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
    }

    void OnValidate()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
            boxCollider.usedByEffector = true;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && Application.isEditor && !Application.isPlaying)
            spriteRenderer.enabled = true;
    }

    void SetupOneWay()
    {
        if (boxCollider != null)
        {
            boxCollider.usedByEffector = true;
            boxCollider.isTrigger = false;
        }

        PlatformEffector2D effector = GetComponent<PlatformEffector2D>();
        if (effector == null)
            effector = gameObject.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.useOneWayGrouping = true;
        effector.surfaceArc = 170f;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = editorColor;
        if (boxCollider == null)
            boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            Vector2 size = boxCollider.size;
            Vector2 offset = boxCollider.offset;
            Vector3 center = transform.position + (Vector3)offset;
            Gizmos.DrawCube(center, new Vector3(size.x, size.y, 0.1f));
        }
        Gizmos.color = Color.white;
    }
}
