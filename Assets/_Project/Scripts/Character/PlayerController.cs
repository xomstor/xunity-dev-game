using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float runDelay = 0.3f;
    public float jumpForce = 10f;
    public int maxJumps = 2;

    [Header("Attack")]
    public float attackComboResetTime = 1f;

    [Header("Roll")]
    public float rollSpeed = 8f;
    public float rollDuration = 0.5f;
    public float rollCooldown = 1f;

    [Header("Ground Check")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.1f;

    [Header("Stats")]
    public PlayerStats playerStats;

    private Rigidbody2D rb;
    private BoxCollider2D col;
    private Animator anim;
    private bool isGrounded;
    private float moveInput;
    private int jumpCount;
    private float moveHeldTime;
    private float currentMoveSpeed;
    private int attackCombo;
    private float attackComboTimer;
    private bool isAttacking;
    private bool isRolling;
    private float rollTimer;
    private float rollCooldownTimer;
    private int rollDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();

        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

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

            if ((keyboard.spaceKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) && jumpCount > 0)
                Jump();

            if (keyboard.jKey.wasPressedThisFrame)
                Attack();

            if (keyboard.kKey.wasPressedThisFrame)
                Roll();
        }

        UpdateRoll();
        UpdateMoveSpeed();
        UpdateAttackComboTimer();

        if (moveInput > 0)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
        else if (moveInput < 0)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);

        UpdateAnimator();
    }

    void UpdateMoveSpeed()
    {
        float speedMultiplier = playerStats != null ? 1f + playerStats.spd * 0.05f : 1f;

        if (moveInput != 0)
        {
            moveHeldTime += Time.deltaTime;
            currentMoveSpeed = (moveHeldTime >= runDelay ? runSpeed : walkSpeed) * speedMultiplier;
        }
        else
        {
            moveHeldTime = 0f;
            currentMoveSpeed = 0f;
        }
    }

    void UpdateAttackComboTimer()
    {
        if (attackComboTimer > 0)
        {
            attackComboTimer -= Time.deltaTime;
            if (attackComboTimer <= 0)
                attackCombo = 0;
        }
    }

    public void Attack()
    {
        if (anim == null || isAttacking || isRolling) return;

        isAttacking = true;
        attackComboTimer = attackComboResetTime;
        attackCombo++;

        if (attackCombo > 3)
            attackCombo = 1;

        string triggerName = attackCombo switch
        {
            2 => "Attack2",
            3 => "Attack3",
            _ => "Attack1",
        };

        anim.SetTrigger(triggerName);

        AutoCombat combat = GetComponent<AutoCombat>();
        if (combat != null)
            combat.TryAttack();

        Invoke(nameof(ResetAttack), 0.5f);
    }

    void ResetAttack()
    {
        isAttacking = false;
    }

    public void Roll()
    {
        if (isRolling || rollCooldownTimer > 0 || !isGrounded) return;

        isRolling = true;
        rollTimer = rollDuration;
        rollCooldownTimer = rollCooldown;
        rollDirection = moveInput != 0 ? (int)Mathf.Sign(moveInput) : (int)Mathf.Sign(transform.localScale.x);

        if (anim != null)
            anim.SetTrigger("Roll");
    }

    void UpdateRoll()
    {
        if (rollCooldownTimer > 0)
            rollCooldownTimer -= Time.deltaTime;

        if (rollTimer > 0)
        {
            rollTimer -= Time.deltaTime;
            if (rollTimer <= 0)
                isRolling = false;
        }
    }

    void FixedUpdate()
    {
        CheckGround();

        if (isRolling)
        {
            rb.linearVelocity = new Vector2(rollDirection * rollSpeed, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(moveInput * currentMoveSpeed, rb.linearVelocity.y);
        }

        moveInput = 0f;
    }

    void UpdateAnimator()
    {
        if (anim == null) return;
        bool isMoving = Mathf.Abs(moveInput) > 0.1f && isGrounded;
        int animState = isMoving ? 1 : 0;
        anim.SetInteger("AnimState", animState);
        anim.SetBool("Grounded", isGrounded);
        anim.SetFloat("AirSpeedY", rb.linearVelocity.y);
    }

    void CheckGround()
    {
        Bounds bounds = col.bounds;
        Vector2 origin = new Vector2(bounds.center.x, bounds.min.y);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
        isGrounded = hit.collider != null;
        if (isGrounded) jumpCount = maxJumps;
    }

    public void SetMoveInput(float input) => moveInput = input;

    public void Jump()
    {
        if (jumpCount > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpCount--;
        }
    }

    public bool IsGrounded() => isGrounded;
}
