using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [Header("Ground Check")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.1f;

    private Rigidbody2D rb;
    private BoxCollider2D col;
    private Animator anim;
    private bool isGrounded;
    private float moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            float keyInput = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) keyInput -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) keyInput += 1f;
            if (keyInput != 0f) moveInput = keyInput;

            if ((keyboard.spaceKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) && isGrounded)
                Jump();
        }

        if (moveInput > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else if (moveInput < 0)
            transform.localScale = new Vector3(-1, 1, 1);

        UpdateAnimator();
    }

    void FixedUpdate()
    {
        CheckGround();
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        moveInput = 0f;
    }

    void UpdateAnimator()
    {
        if (anim == null) return;
        anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        anim.SetBool("Grounded", isGrounded);
        anim.SetFloat("AirSpeedY", rb.linearVelocity.y);
    }

    void CheckGround()
    {
        Bounds bounds = col.bounds;
        Vector2 origin = new Vector2(bounds.center.x, bounds.min.y);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
        isGrounded = hit.collider != null;
    }

    public void SetMoveInput(float input) => moveInput = input;

    public void Jump()
    {
        if (isGrounded)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    public bool IsGrounded() => isGrounded;
}
