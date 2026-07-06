using UnityEngine;

public enum CombatTeam
{
    Player,
    Enemy,
    Neutral
}

public class AutoCombat : MonoBehaviour
{
    [Header("Team")]
    public CombatTeam team = CombatTeam.Enemy;
    public bool canBeTargeted = true;
    public bool faceRightByDefault = true;
    public string[] ignoreTags = new string[] { "World" };

    [Header("Stats")]
    public int maxHealth = 100;
    public int damage = 10;
    public int def = 0;
    [Range(0.1f, 10f)] public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    [Range(0.5f, 30f)] public float detectionRadius = 15f;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public bool chaseTarget = true;
    public float minChaseDuration = 90f;
    public float returnSpeed = 2f;
    public float returnArriveDistance = 0.2f;
    public GameObject questionMarkPrefab;
    public bool checkEdges = true;
    public float edgeCheckForwardOffset = 0.1f;
    public float edgeCheckDownDistance = 0.5f;

    [Header("Jump Attack")]
    public bool canJumpAttack = true;
    [Range(0f, 1f)] public float jumpAttackChance = 0.05f;
    public float jumpForce = 5f;
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.1f;

    [Header("Animation")]
    public Animator anim;
    public string attackTrigger = "Attack";
    public string deathTrigger = "Death";
    public string hitTrigger = "Hit";

    [Header("Effects")]
    public GameObject deathEffect;
    [Header("Health (Runtime)")]
    [SerializeField]
    private int currentHealth;
    private float attackTimer;
    private Transform target;
    public Transform Target => target;
    private bool isDead;
    private Rigidbody2D rb;
    private float regenAccum;
    private int lastAnimState = -1;
    private Vector3 startPosition;
    private Vector2 lastKnownTargetDirection;
    private float chaseTimer;
    private bool isReturning;
    private GameObject questionMarkInstance;

    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    public void ResetHealth()
    {
        isDead = false;
        currentHealth = maxHealth;
        lastAnimState = -1;
        chaseTimer = 0f;
        isReturning = false;
        ShowQuestionMark(false);
        CancelInvoke(nameof(HideAfterDeath));
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = true;

        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        if (team == CombatTeam.Player)
        {
            PlayerController pc = GetComponentInChildren<PlayerController>();
            if (pc == null) pc = GetComponent<PlayerController>();
            if (pc != null)
                pc.enabled = true;
        }
    }

    void Awake()
    {
        currentHealth = maxHealth;
        startPosition = transform.position;
        lastKnownTargetDirection = Vector2.right;
        rb = GetComponent<Rigidbody2D>();
        if (groundLayer == 0)
            groundLayer = LayerMask.GetMask("Ground");
        if (rb == null)
            rb = GetComponentInParent<Rigidbody2D>();
        if (anim == null)
            anim = GetComponent<Animator>();
        if (anim == null || anim.runtimeAnimatorController == null)
        {
            Animator[] animators = GetComponentsInChildren<Animator>(true);
            foreach (Animator candidate in animators)
            {
                if (candidate != null && candidate.runtimeAnimatorController != null)
                {
                    anim = candidate;
                    break;
                }
            }
        }

        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null)
        {
            maxHealth = stats.maxHp;
            currentHealth = stats.hp;
            damage = stats.atk;
        }

        if (team == CombatTeam.Enemy && WorldLevelManager.Instance != null)
        {
            maxHealth = Mathf.RoundToInt(maxHealth * WorldLevelManager.Instance.CurrentEnemyHealthMultiplier);
            damage = Mathf.RoundToInt(damage * WorldLevelManager.Instance.CurrentEnemyDamageMultiplier);
            currentHealth = maxHealth;
        }

        if (team == CombatTeam.Enemy && chaseTarget && rb == null)
            Debug.LogWarning($"{name}: chaseTarget is enabled but no Rigidbody2D found. Enemy will not move!");

        if (rb != null && team == CombatTeam.Enemy)
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
        bool hasNonTrigger = false;
        foreach (Collider2D c in cols)
        {
            if (!c.isTrigger) { hasNonTrigger = true; break; }
        }
        if (!hasNonTrigger && team == CombatTeam.Enemy)
            Debug.LogWarning($"{name}: has no non-trigger collider! Player cannot auto-target this enemy.");

        // <--- ДОБАВЛЕНО: Синхронизация при старте игры ---
        if (team == CombatTeam.Player && HealthSystem.Instance != null)
        {
            HealthSystem.Instance.hitPoint = currentHealth;
            HealthSystem.Instance.maxHitPoint = maxHealth;
            HealthSystem.Instance.UpdateGraphics();
        }
        // ------------------------------------------------
    }

    void Update()
    {
        if (isDead) return;

        if (attackTimer > 0)
            attackTimer -= Time.deltaTime;

        ApplyHpRegen();
        FindTarget();

        PlayerController playerController = GetComponent<PlayerController>();

        if (target != null)
        {
            lastKnownTargetDirection = (target.position - transform.position).normalized;
            chaseTimer = 0f;
            isReturning = false;
            ShowQuestionMark(false);

            float distance = GetDistanceToTarget();
            if (distance <= attackRange)
            {
                if (attackTimer <= 0)
                {
                    if (playerController != null)
                        playerController.Attack();
                    else
                    {
                        Attack();
                        SetAnimState(0);
                    }
                }
            }
            else if (playerController == null && chaseTarget && rb != null)
            {
                ChaseTarget();
                SetAnimState(1);
            }
        }
        else if (playerController == null)
        {
            if (isReturning)
            {
                ReturnToStart();
            }
            else if (chaseTarget && chaseTimer < minChaseDuration)
            {
                chaseTimer += Time.deltaTime;
                if (rb != null)
                {
                    Vector3 farPoint = transform.position + new Vector3(lastKnownTargetDirection.x, 0f, 0f) * 100f;
                    MoveTowards(farPoint, moveSpeed);
                    SetAnimState(1);
                }
            }
            else if (chaseTarget && chaseTimer >= minChaseDuration)
            {
                isReturning = true;
                ShowQuestionMark(true);
            }
            else
            {
                SetAnimState(0);
            }
        }
    }

    void ApplyHpRegen()
    {
        if (team != CombatTeam.Player) return;
        if (currentHealth >= maxHealth) return;

        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats == null) return;

        float regen = stats.GetHpRegenPerSecond() * Time.deltaTime;
        if (regen <= 0f) return;

        regenAccum += regen;
        if (regenAccum >= 1f)
        {
            int heal = Mathf.FloorToInt(regenAccum);
            currentHealth = Mathf.Min(maxHealth, currentHealth + heal);
            stats.hp = currentHealth;
            regenAccum -= heal;

            // <--- ДОБАВЛЕНО: Обновление шара при регене ---
            if (HealthSystem.Instance != null)
            {
                HealthSystem.Instance.hitPoint = currentHealth;
                HealthSystem.Instance.UpdateGraphics();
            }
            // -----------------------------------------------
        }
    }

    float GetDistanceToTarget()
    {
        if (target == null) return float.MaxValue;
        Collider2D targetCollider = target.GetComponent<Collider2D>();
        Collider2D myCollider = GetComponent<Collider2D>();
        if (targetCollider != null && myCollider != null)
        {
            ColliderDistance2D dist = myCollider.Distance(targetCollider);
            return Mathf.Max(0f, dist.distance);
        }
        if (targetCollider != null)
            return Vector2.Distance(transform.position, targetCollider.ClosestPoint(transform.position));
        return Vector2.Distance(transform.position, target.position);
    }

    void SetAnimState(int state)
    {
        if (anim == null || anim.runtimeAnimatorController == null) return;
        if (state == lastAnimState) return;
        lastAnimState = state;

        if (HasAnimatorParameter("AnimState"))
        {
            anim.SetInteger("AnimState", state);
        }
        else
        {
            if (state == 1)
                anim.Play("Move", 0, 0f);
        }
    }

    bool HasAnimatorParameter(string paramName)
    {
        if (anim == null || anim.runtimeAnimatorController == null) return false;
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }

    public bool TryAttack()
    {
        if (attackTimer > 0) return false;
        if (target == null) return false;

        float distance = GetDistanceToTarget();
        if (distance > attackRange) return false;

        attackTimer = GetEffectiveAttackCooldown();

        if (team == CombatTeam.Player)
        {
            return TryMultiAttack();
        }

        int finalDamage = GetFinalDamage(out bool isCrit);
        int attackerLethality = GetLethality();
        bool isPlayer = team == CombatTeam.Player;

        AutoCombat targetCombat = FindAutoCombat(target);
        if (targetCombat != null)
        {
            targetCombat.TakeDamage(finalDamage, attackerLethality);
            ShowDamagePopup(GetPopupPosition(targetCombat.transform), finalDamage, isCrit, isPlayer);
            return true;
        }

        PlayerStats targetStats = FindPlayerStats(target);
        if (targetStats != null)
        {
            targetStats.TakeDamage(finalDamage, attackerLethality);
            ShowDamagePopup(GetPopupPosition(targetStats.transform), finalDamage, isCrit, isPlayer);
            return true;
        }

        Debug.LogWarning($"{name}: target {target.name} has no AutoCombat or PlayerStats!");
        return false;
    }

    bool TryMultiAttack()
    {
        int finalDamage = GetFinalDamage(out bool isCrit);
        int attackerLethality = GetLethality();
        bool isPlayer = team == CombatTeam.Player;
        bool hitAny = false;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, attackRange);
        foreach (Collider2D collider in colliders)
        {
            if (collider.transform.root == transform.root) continue;

            AutoCombat other = FindAutoCombat(collider.transform);
            if (other == null || other == this || other.isDead || !other.canBeTargeted || !IsValidTarget(other.team) || HasIgnoreTag(other.transform))
                continue;

            other.TakeDamage(finalDamage, attackerLethality);
            ShowDamagePopup(GetPopupPosition(other.transform), finalDamage, isCrit, isPlayer);
            hitAny = true;
        }

        return hitAny;
    }

    Vector3 GetPopupPosition(Transform t)
    {
        Collider2D col = t.GetComponent<Collider2D>();
        if (col != null)
            return col.bounds.center + Vector3.up * col.bounds.extents.y;
        return t.position + Vector3.up * 0.5f;
    }

    void ShowDamagePopup(Vector3 position, int damage, bool isCrit, bool isPlayer)
    {
        if (DamagePopupManager.Instance == null) return;
        try
        {
            DamagePopupManager.Instance.ShowDamage(position, damage, isCrit, isPlayer);
        }
        catch { }
    }

    int GetFinalDamage(out bool isCrit)
    {
        isCrit = false;
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null)
        {
            isCrit = Random.value < stats.GetCritChance();
            return isCrit ? Mathf.RoundToInt(stats.atk * stats.critDamageMultiplier) : stats.atk;
        }
        return damage;
    }

    int GetFinalDamage()
    {
        return GetFinalDamage(out _);
    }

    int GetLethality()
    {
        PlayerStats stats = GetComponent<PlayerStats>();
        return stats != null ? stats.GetEffectiveLethality() : 0;
    }

    float GetEffectiveAttackCooldown()
    {
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null)
            return attackCooldown * stats.GetAttackCooldownMultiplier();
        return attackCooldown;
    }

    int GetMitigatedDamage(int rawDamage, int attackerLethality)
    {
        int pureDamage = Mathf.Min(attackerLethality, rawDamage);
        int mitigatable = rawDamage - pureDamage;
        float effectiveDef = Mathf.Clamp(def, 0, 34221);
        float multiplier = Mathf.Pow(100f / (100f + effectiveDef), 0.774f);
        int mitigated = Mathf.RoundToInt(mitigatable * Mathf.Clamp01(multiplier));
        return Mathf.Max(1, pureDamage + mitigated);
    }

    AutoCombat FindAutoCombat(Transform t)
    {
        if (t == null) return null;
        AutoCombat combat = t.GetComponent<AutoCombat>();
        if (combat != null) return combat;
        combat = t.GetComponentInChildren<AutoCombat>();
        if (combat != null) return combat;
        return t.GetComponentInParent<AutoCombat>();
    }

    PlayerStats FindPlayerStats(Transform t)
    {
        if (t == null) return null;
        PlayerStats stats = t.GetComponent<PlayerStats>();
        if (stats != null) return stats;
        stats = t.GetComponentInChildren<PlayerStats>();
        if (stats != null) return stats;
        return t.GetComponentInParent<PlayerStats>();
    }

    void FindTarget()
    {
        Transform previousTarget = target;
        target = null;
        float closestDistance = detectionRadius;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
        foreach (Collider2D collider in colliders)
        {
            if (collider.isTrigger && collider.name == "NameTagDetector") continue;

            Transform root = collider.transform.root;
            if (root == transform.root) continue;

            float distance = Vector2.Distance(transform.position, collider.transform.position);

            AutoCombat other = FindAutoCombat(collider.transform);
            PlayerController playerController = collider.GetComponent<PlayerController>();
            if (playerController == null)
                playerController = collider.GetComponentInParent<PlayerController>();
            if (playerController == null)
                playerController = collider.GetComponentInChildren<PlayerController>();

            if (other == null && playerController == null) continue;
            if (other != null && (other == this || other.isDead || !other.canBeTargeted || !IsValidTarget(other.team) || HasIgnoreTag(other.transform))) continue;
            if (playerController != null && HasIgnoreTag(playerController.transform)) continue;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                target = collider.transform;
            }

        }

    }

    bool IsValidTarget(CombatTeam otherTeam)
    {
        if (team == CombatTeam.Neutral || otherTeam == CombatTeam.Neutral)
            return false;
        return team != otherTeam;
    }

    bool HasIgnoreTag(Transform t)
    {
        if (ignoreTags == null || ignoreTags.Length == 0) return false;
        foreach (string tag in ignoreTags)
        {
            if (string.IsNullOrEmpty(tag)) continue;
            if (t.CompareTag(tag)) return true;
        }
        return false;
    }

    void ChaseTarget()
    {
        if (target == null) return;
        MoveTowards(target.position, moveSpeed);
    }

    void MoveTowards(Vector3 destination, float speed)
    {
        Vector2 direction = (destination - transform.position).normalized;

        if (Mathf.Abs(direction.x) > 0.01f && !HasGroundAhead(direction.x))
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        rb.linearVelocity = new Vector2(direction.x * speed, rb.linearVelocity.y);

        float absX = Mathf.Abs(transform.localScale.x);
        if (faceRightByDefault)
        {
            if (direction.x > 0.1f)
                transform.localScale = new Vector3(absX, transform.localScale.y, 1);
            else if (direction.x < -0.1f)
                transform.localScale = new Vector3(-absX, transform.localScale.y, 1);
        }
        else
        {
            if (direction.x > 0.1f)
                transform.localScale = new Vector3(-absX, transform.localScale.y, 1);
            else if (direction.x < -0.1f)
                transform.localScale = new Vector3(absX, transform.localScale.y, 1);
        }
    }

    void ReturnToStart()
    {
        if (Vector3.Distance(transform.position, startPosition) <= returnArriveDistance)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            isReturning = false;
            chaseTimer = 0f;
            ShowQuestionMark(false);
            SetAnimState(0);
            return;
        }

        MoveTowards(startPosition, returnSpeed);
        SetAnimState(1);
    }

    void ShowQuestionMark(bool show)
    {
        if (team != CombatTeam.Enemy) return;

        if (show)
        {
            if (questionMarkInstance == null)
            {
                if (questionMarkPrefab != null)
                {
                    questionMarkInstance = Instantiate(questionMarkPrefab, transform.position + Vector3.up * 1.5f, Quaternion.identity, transform);
                }
                else
                {
                    GameObject qm = new GameObject("QuestionMark");
                    qm.transform.SetParent(transform, false);
                    qm.transform.localPosition = new Vector3(0, 1.5f, 0);
                    TextMesh tm = qm.AddComponent<TextMesh>();
                    tm.text = "?";
                    tm.characterSize = 0.3f;
                    tm.fontSize = 64;
                    tm.anchor = TextAnchor.MiddleCenter;
                    tm.alignment = TextAlignment.Center;
                    tm.color = Color.yellow;
                    questionMarkInstance = qm;
                }
            }
            questionMarkInstance.SetActive(true);
        }
        else if (questionMarkInstance != null)
        {
            questionMarkInstance.SetActive(false);
        }
    }

    bool IsGrounded()
    {
        if (groundLayer == 0) return true;
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return false;
        Vector2 origin = new Vector2(col.bounds.center.x, col.bounds.min.y);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
        return hit.collider != null;
    }

    bool HasGroundAhead(float directionX)
    {
        if (!checkEdges || groundLayer == 0) return true;
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return true;

        float sign = Mathf.Sign(directionX);
        float x = sign > 0 ? col.bounds.max.x : col.bounds.min.x;
        Vector2 origin = new Vector2(x + sign * edgeCheckForwardOffset, col.bounds.min.y);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, edgeCheckDownDistance, groundLayer);
        return hit.collider != null;
    }

    void Attack()
    {
        if (attackTimer > 0) return;

        attackTimer = GetEffectiveAttackCooldown();

        if (anim != null && anim.runtimeAnimatorController != null && HasAnimatorParameter(attackTrigger))
        {
            anim.SetTrigger(attackTrigger);
        }

        if (canJumpAttack && Random.value < jumpAttackChance && IsGrounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        int finalDamage = GetFinalDamage(out bool isCrit);
        int attackerLethality = GetLethality();

        AutoCombat targetCombat = FindAutoCombat(target);
        if (targetCombat != null)
        {
            targetCombat.TakeDamage(finalDamage, attackerLethality);
            ShowDamagePopup(GetPopupPosition(targetCombat.transform), finalDamage, isCrit, team == CombatTeam.Player);
        }
    }

    public void Heal(int amount)
    {
        if (isDead || amount <= 0) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null)
            stats.hp = currentHealth;

        // <--- ДОБАВЛЕНО: Синхронизация при лечении ---
        if (team == CombatTeam.Player && HealthSystem.Instance != null)
        {
            HealthSystem.Instance.hitPoint = currentHealth;
            HealthSystem.Instance.maxHitPoint = maxHealth;
            HealthSystem.Instance.UpdateGraphics();
        }
        // -----------------------------------------------
    }

    public void TakeDamage(int amount, int attackerLethality = 0)
    {
        if (isDead) return;

        if (team == CombatTeam.Player)
        {
            PlayerController pc = GetComponentInChildren<PlayerController>();
            if (pc == null) pc = GetComponentInParent<PlayerController>();
            if (pc == null) pc = GetComponent<PlayerController>();
            if (pc != null && pc.isInvulnerable)
                return;
        }

        int finalDamage = GetMitigatedDamage(amount, attackerLethality);

        currentHealth = Mathf.Max(0, currentHealth - finalDamage);

        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null)
            stats.hp = currentHealth;

        if (anim != null && anim.runtimeAnimatorController != null)
        {
            if (GetComponentInChildren<PlayerController>() != null && HasAnimatorParameter("Hurt"))
                anim.SetTrigger("Hurt");
        }

        PlayerAudio playerAudio = GetComponent<PlayerAudio>();
        if (playerAudio != null)
            playerAudio.PlayHurt();

        if (currentHealth <= 0)
        {
            Die();
        }

        // <--- ДОБАВЛЕНО: Синхронизация при получении урона ---
        if (team == CombatTeam.Player && HealthSystem.Instance != null)
        {
            HealthSystem.Instance.hitPoint = currentHealth;
            HealthSystem.Instance.maxHitPoint = maxHealth;
            HealthSystem.Instance.UpdateGraphics();
        }
        // ---------------------------------------------------
    }

    void Die()
    {
        isDead = true;
        ShowQuestionMark(false);

        if (team == CombatTeam.Player)
        {
            PlayerStats stats = GetComponent<PlayerStats>();
            stats?.ApplyDeathPenalty();
        }

        GiveRewards();
        GiveDrops();

        if (anim != null && anim.runtimeAnimatorController != null && HasAnimatorParameter(deathTrigger))
            anim.SetTrigger(deathTrigger);

        // Отключаем управление игроком
        PlayerController pc = GetComponentInChildren<PlayerController>();
        if (pc != null)
            pc.enabled = false;

        // Останавливаем все Rigidbody2D в иерархии
        Rigidbody2D[] bodies = GetComponentsInChildren<Rigidbody2D>();
        foreach (Rigidbody2D body in bodies)
        {
            body.linearVelocity = Vector2.zero;
            body.bodyType = RigidbodyType2D.Kinematic;
        }

        // Отключаем все коллайдеры в иерархии
        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D c in cols)
            c.enabled = false;

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        Invoke(nameof(HideAfterDeath), 1.5f);

        EnemyRespawnManager.Instance?.RegisterDeath(this);
    }

    void HideAfterDeath()
    {
        SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in sprites)
        {
            if (sr != null)
                sr.enabled = false;
        }
        if (anim != null)
            anim.enabled = false;
    }

    void GiveRewards()
    {
        if (team != CombatTeam.Enemy) return;

        EnemyReward reward = GetComponent<EnemyReward>();
        if (reward == null) return;

        PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerStats != null)
            playerStats.AddReward(reward.Experience, reward.Gold);
    }

    void GiveDrops()
    {
        if (team != CombatTeam.Enemy) return;

        EnemyReward reward = GetComponent<EnemyReward>();
        if (reward == null || reward.Drops == null || reward.Drops.Length == 0) return;

        PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();
        float luckMultiplier = playerStats != null ? playerStats.GetDropChanceMultiplier() : 1f;

        Inventory playerInventory = FindAnyObjectByType<Inventory>();
        if (playerInventory == null)
        {
            Debug.LogWarning($"[{name}] Drop skipped: Inventory not found.");
            return;
        }

        foreach (DropItem drop in reward.Drops)
        {
            if (drop.itemData == null)
            {
                Debug.LogWarning($"[{name}] Drop skipped: itemData is null.");
                continue;
            }

            if (drop.dropOnlyOnce)
            {
                bool hasItem = playerInventory.GetItemCount(drop.itemData) > 0;

                if (!hasItem && EquipmentManager.Instance != null)
                    hasItem = EquipmentManager.Instance.IsEquipped(drop.itemData);

                if (hasItem)
                {
                    Debug.Log($"[{name}] Drop skipped: {drop.itemData.itemName} already in inventory/equipped.");
                    continue;
                }
            }

            float chance = Mathf.Min(drop.baseDropChance * luckMultiplier, 1f);
            float roll = Random.value;
            Debug.Log($"[{name}] Drop roll: {drop.itemData.itemName}, roll={roll:0.000}, chance={chance:0.000}, once={drop.dropOnlyOnce}");

            if (roll <= chance)
            {
                bool added = playerInventory.AddItem(drop.itemData, drop.quantity);
                if (added)
                {
                    Debug.Log($"Dropped: {drop.itemData.itemName} x{drop.quantity}");
                }
                else
                {
                    Debug.LogWarning($"[{name}] Drop failed: Inventory rejected {drop.itemData.itemName} x{drop.quantity}.");
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Радиус атаки (красный)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Радиус обнаружения (жёлтый)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}