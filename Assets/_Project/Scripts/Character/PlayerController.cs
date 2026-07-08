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
    public float attackComboResetTime = 1.5f;
    [Tooltip("Максимальный визуальный ускоритель анимации атаки (без капа самого AtkSpd)")]
    public float maxAttackAnimSpeed = 3f;

    [Header("Roll")]
    public float rollSpeed = 8f;
    public float rollDuration = 0.5f;
    public float rollCooldown = 1f;
    [Header("Invulnerability")]
    public float rollInvulnerabilityDuration = 0.5f;
    public bool isInvulnerable { get; private set; }

    [Header("Ground Check")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.1f;

    [Header("Wall Slide")]
    public bool enableWallSlide = true;
    public float wallSlideSpeed = 0.5f;
    public float wallJumpForceX = 6f;
    public float wallJumpForceY = 10f;
    public float wallCheckDistance = 0.1f;
    public GameObject slideDustPrefab;

    [Header("Stats")]
    public PlayerStats playerStats;

    [Header("Audio")]
    public PlayerAudio playerAudio;

    private Rigidbody2D rb;
    private BoxCollider2D col;
    private Animator anim;
    private bool isGrounded;
    private float moveInput;
    private float keyboardMoveInput;
    private float externalMoveInput;
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
    private float rawMoveInput;
    private bool isCrouching;
    private Collider2D currentGroundCollider;
    private float dropThroughTimer;
    private Collider2D ignoredPlatform;
    private bool isWallSliding;
    private int wallDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();

        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();
        if (playerAudio == null)
            playerAudio = GetComponent<PlayerAudio>();

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    public bool IsInputBlocked => DialogueSystem.IsDialogueActive;

    void Update()
    {
        if (DialogueSystem.IsDialogueActive)
        {
            keyboardMoveInput = 0f;
            externalMoveInput = 0f;
            moveInput = 0f;
            rawMoveInput = 0f;
            currentMoveSpeed = 0f;
            if (rb != null && isGrounded)
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            UpdateAnimator();
            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            keyboardMoveInput = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) keyboardMoveInput -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) keyboardMoveInput += 1f;

            if (keyboard.wKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
                Jump();

            if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
            {
                if (!TryDropThrough())
                    StartCrouch();
            }

            if (keyboard.sKey.wasReleasedThisFrame || keyboard.downArrowKey.wasReleasedThisFrame)
                StopCrouch();

            if (keyboard.jKey.wasPressedThisFrame)
                Attack();

            if (keyboard.kKey.wasPressedThisFrame)
                Roll();
        }

        moveInput = keyboardMoveInput != 0f ? keyboardMoveInput : externalMoveInput;
        rawMoveInput = moveInput;
        if (isCrouching)
            moveInput = 0f;

        UpdateRoll();
        UpdateMoveSpeed();
        UpdateAttackComboTimer();
        UpdateWallSlide();

        if (rawMoveInput > 0)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
        else if (rawMoveInput < 0)
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

    public void AutoAttack()
    {
        if (anim == null || isRolling) return;
        if (isAttacking) return;
        Attack();
    }

    public void Attack()
    {
        if (anim == null || isAttacking || isRolling) return;

        isAttacking = true;
        attackComboTimer = attackComboResetTime;
        attackCombo++;
        
        // Отслеживаем атаку в статистике
        GameStatistics.Instance?.RecordAttack();

        if (attackCombo > 3)
            attackCombo = 1;

        string triggerName = attackCombo switch
        {
            2 => "Attack2",
            3 => "Attack3",
            _ => "Attack1",
        };

        ApplyAttackAnimationSpeed();
        anim.ResetTrigger("Attack1");
        anim.ResetTrigger("Attack2");
        anim.ResetTrigger("Attack3");
        anim.SetTrigger(triggerName);
        playerAudio?.PlayAttack(attackCombo);

        AutoCombat combat = GetComponent<AutoCombat>();
        combat?.TryAttack(attackCombo);

        float resetDelay = combat != null ? Mathf.Max(0.4f, combat.attackCooldown - 0.1f) : 0.5f;
        CancelInvoke(nameof(ResetAttack));
        Invoke(nameof(ResetAttack), resetDelay);
    }

    void ApplyAttackAnimationSpeed()
    {
        if (anim == null) return;
        float multiplier = 1f;
        if (playerStats != null)
            multiplier = playerStats.GetAttackCooldownMultiplier();
        float desiredSpeed = multiplier > 0.0001f ? Mathf.Min(1f / multiplier, maxAttackAnimSpeed) : maxAttackAnimSpeed;
        anim.speed = desiredSpeed;
    }

    void ResetAttack()
    {
        isAttacking = false;
        if (anim != null)
            anim.speed = 1f;
    }

    public void Roll()
    {
        int direction = moveInput != 0 ? (int)Mathf.Sign(moveInput) : (int)Mathf.Sign(transform.localScale.x);
        Roll(direction);
    }

    public void Roll(int direction)
    {
        if (isRolling || rollCooldownTimer > 0 || !isGrounded) return;

        isRolling = true;
        rollTimer = rollDuration;
        rollCooldownTimer = rollCooldown;
        rollDirection = direction;
        isInvulnerable = true;
        CancelInvoke(nameof(EndRollInvulnerability));
        Invoke(nameof(EndRollInvulnerability), rollInvulnerabilityDuration);

        if (anim != null)
            anim.SetTrigger("Roll");

        playerAudio?.PlayRoll();
    }

    void EndRollInvulnerability()
    {
        isInvulnerable = false;
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
        CheckWall();

        if (dropThroughTimer > 0)
        {
            dropThroughTimer -= Time.fixedDeltaTime;
            if (dropThroughTimer <= 0 && ignoredPlatform != null)
            {
                Physics2D.IgnoreCollision(col, ignoredPlatform, false);
                ignoredPlatform = null;
            }
        }

        if (isRolling)
        {
            rb.linearVelocity = new Vector2(rollDirection * rollSpeed, rb.linearVelocity.y);
        }
        else if (isWallSliding)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -wallSlideSpeed));
        }
        else
        {
            rb.linearVelocity = new Vector2(moveInput * currentMoveSpeed, rb.linearVelocity.y);
        }
    }

    void UpdateAnimator()
    {
        if (anim == null) return;
        bool isMoving = Mathf.Abs(moveInput) > 0.1f && isGrounded;
        int animState = isMoving ? 1 : 0;
        anim.SetInteger("AnimState", animState);
        anim.SetBool("Grounded", isGrounded);
        anim.SetFloat("AirSpeedY", rb.linearVelocity.y);
        anim.SetBool("WallSlide", isWallSliding);

        playerAudio?.SetMovement(isMoving, currentMoveSpeed);
    }

    void CheckGround()
    {
        Bounds bounds = col.bounds;
        Vector2 origin = new Vector2(bounds.center.x, bounds.min.y);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
        isGrounded = hit.collider != null;
        currentGroundCollider = hit.collider;
        if (isGrounded) jumpCount = maxJumps;
    }

    void CheckWall()
    {
        if (!enableWallSlide || isGrounded) { isWallSliding = false; return; }

        Bounds bounds = col.bounds;
        float facing = Mathf.Sign(transform.localScale.x);
        Vector2 origin = new Vector2(facing > 0 ? bounds.max.x : bounds.min.x, bounds.center.y);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right * facing, wallCheckDistance, groundLayer);

        bool touchingWall = hit.collider != null;
        bool pushingIntoWall = Mathf.Sign(rawMoveInput) == facing && rawMoveInput != 0;
        bool falling = rb.linearVelocity.y < 0;

        bool wasSliding = isWallSliding;
        isWallSliding = touchingWall && pushingIntoWall && falling;
        wallDirection = (int)facing;

        if (isWallSliding && !wasSliding && slideDustPrefab != null)
            Instantiate(slideDustPrefab, new Vector3(bounds.center.x + facing * bounds.extents.x, bounds.min.y, 0), Quaternion.identity);
    }

    void UpdateWallSlide()
    {
        if (!isWallSliding) return;

        var keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.wKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame))
            WallJump();
    }

    void WallJump()
    {
        isWallSliding = false;
        jumpCount = Mathf.Max(jumpCount, 1);
        rb.linearVelocity = new Vector2(-wallDirection * wallJumpForceX, wallJumpForceY);
        playerAudio?.PlayJump();
    }

    public bool TryDropThrough()
    {
        if (!isGrounded || currentGroundCollider == null) return false;

        PlatformEffector2D effector = currentGroundCollider.GetComponent<PlatformEffector2D>();
        if (effector != null)
        {
            Physics2D.IgnoreCollision(col, currentGroundCollider, true);
            ignoredPlatform = currentGroundCollider;
            dropThroughTimer = 0.5f;
            isGrounded = false;
            jumpCount = Mathf.Max(jumpCount, 1);
            return true;
        }
        return false;
    }

    public void SetMoveInput(float input)
    {
        externalMoveInput = input;
    }

    public void StartCrouch()
    {
        if (isGrounded && !isCrouching)
        {
            isCrouching = true;
            if (anim != null) anim.SetBool("Crouch", true);
        }
    }

    public void StopCrouch()
    {
        if (isCrouching)
        {
            isCrouching = false;
            if (anim != null) anim.SetBool("Crouch", false);
        }
    }

    public void Jump()
    {
        if (isWallSliding)
        {
            WallJump();
            return;
        }

        if (jumpCount > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpCount--;
            
            // Отслеживаем прыжок в статистике
            GameStatistics.Instance?.RecordJump();
            playerAudio?.PlayJump();
        }
    }

    public bool IsGrounded() => isGrounded;
}
