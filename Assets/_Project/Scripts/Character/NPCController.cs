using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class NPCController : MonoBehaviour
{
    [Header("Ground Check")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.1f;

    private Rigidbody2D rb;
    private BoxCollider2D col;
    private Animator anim;
    private bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void Update()
    {
        CheckGround();
        UpdateAnimator();
    }

    void UpdateAnimator()
    {
        if (anim == null) return;
        // Stationary NPC — no movement parameters needed
    }

    void CheckGround()
    {
        Bounds bounds = col.bounds;
        Vector2 origin = new Vector2(bounds.center.x, bounds.min.y);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
        isGrounded = hit.collider != null;
    }
}
