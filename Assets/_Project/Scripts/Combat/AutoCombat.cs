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
    [Range(0.1f, 3f)] public float playerMeleeHitboxRange = 0.7f;
    [Range(0.1f, 3f)] public float playerMeleeVerticalTolerance = 0.8f;
    [Range(0.5f, 30f)] public float detectionRadius = 15f;
    public bool requireLineOfSight = false;

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

    [Header("Ranged Attack")]
    [Tooltip("If true, this enemy fires a projectile instead of using a melee hit")]
    public bool isRanged = false;
    [Tooltip("Projectile prefab to fire (must have an EnemyProjectile or Rigidbody2D)")]
    public GameObject projectilePrefab;
    [Tooltip("Optional spawn point; if null, uses this object's position")]
    public Transform firePoint;
    [Tooltip("Projectile flight speed")]
    public float projectileSpeed = 8f;
    [Tooltip("If true, projectile damage equals this enemy's damage stat")]
    public bool inheritProjectileDamage = true;

    [Header("Elemental")]
    public ElementalType element = ElementalType.Physical;
    [Tooltip("Размер ElementalType массива должен быть 20. Индекс = стихия. 0.5 = 50% резист, 1 = иммунитет, -0.5 = уязвимость")]
    public float[] elementalResistances;

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
    private bool hasSeenTarget;
    private GameObject questionMarkInstance;
    private EnemyAudio enemyAudio;

    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    public void ResetHealth()
    {
        isDead = false;
        currentHealth = maxHealth;
        lastAnimState = -1;
        chaseTimer = 0f;
        isReturning = false;
        hasSeenTarget = false;
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
        enemyAudio = GetComponent<EnemyAudio>();
        if (enemyAudio == null)
            enemyAudio = GetComponentInChildren<EnemyAudio>();
        currentHealth = maxHealth;
        startPosition = transform.position;
        lastKnownTargetDirection = Vector2.right;
        attackTimer = attackCooldown * 0.5f;
        rb = GetComponent<Rigidbody2D>();
        if (groundLayer == 0)
            groundLayer = LayerMask.GetMask("Ground");
        if (rb == null)
            rb = GetComponentInParent<Rigidbody2D>();
        if (anim == null || anim.runtimeAnimatorController == null)
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
        if (anim == null || anim.runtimeAnimatorController == null)
            anim = GetComponentInParent<Animator>();

        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null)
        {
            maxHealth = stats.maxHp;
            currentHealth = stats.hp;
            damage = stats.atk;
            def = stats.def;
        }

        if (team == CombatTeam.Enemy && WorldLevelManager.Instance != null)
        {
            maxHealth = Mathf.RoundToInt(maxHealth * WorldLevelManager.Instance.CurrentEnemyHealthMultiplier);
            damage = Mathf.RoundToInt(damage * WorldLevelManager.Instance.CurrentEnemyDamageMultiplier);
            currentHealth = maxHealth;
        }

        if (anim != null && anim.runtimeAnimatorController != null)
        {
            if (HasAnimatorParameter(attackTrigger)) anim.ResetTrigger(attackTrigger);
            if (HasAnimatorParameter(deathTrigger))  anim.ResetTrigger(deathTrigger);
            if (HasAnimatorParameter(hitTrigger))    anim.ResetTrigger(hitTrigger);
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

    void Start()
    {
        // Перепроверяем EnemyAudio после EnemyDataApplier
        if (enemyAudio == null)
        {
            enemyAudio = GetComponent<EnemyAudio>();
            if (enemyAudio == null)
                enemyAudio = GetComponentInChildren<EnemyAudio>();
        }
    }

    void Update()
    {
        if (isDead) return;

        if (attackTimer > 0)
            attackTimer -= Time.deltaTime;

        ApplyHpRegen();
        FindTarget();

        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            if (attackTimer <= 0 && HasAttackTargetInRange())
                playerController.AutoAttack();
            return;
        }

        if (target != null)
        {
            lastKnownTargetDirection = (target.position - transform.position).normalized;
            hasSeenTarget = true;
            chaseTimer = 0f;
            isReturning = false;
            ShowQuestionMark(false);

            float distance = GetDistanceToTarget();
            if (distance <= attackRange)
            {
                if (attackTimer <= 0)
                    Attack();
            }
            else if (chaseTarget && rb != null)
            {
                ChaseTarget();
                SetAnimState(1);
            }
        }
        else
        {
            if (isReturning)
            {
                ReturnToStart();
            }
            else if (chaseTarget && hasSeenTarget && chaseTimer < minChaseDuration)
            {
                chaseTimer += Time.deltaTime;
                if (rb != null)
                {
                    Vector3 farPoint = transform.position + new Vector3(lastKnownTargetDirection.x, 0f, 0f) * 100f;
                    MoveTowards(farPoint, moveSpeed);
                    SetAnimState(1);
                }
            }
            else if (chaseTarget && hasSeenTarget && chaseTimer >= minChaseDuration)
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

        Collider2D myCollider = GetComponent<Collider2D>();

        Collider2D targetCollider = target.GetComponent<Collider2D>();
        if (targetCollider == null)
        {
            Collider2D[] cols = target.GetComponentsInChildren<Collider2D>();
            foreach (var c in cols)
                if (!c.isTrigger) { targetCollider = c; break; }
            if (targetCollider == null && cols.Length > 0)
                targetCollider = cols[0];
        }

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
        if (team == CombatTeam.Enemy && enemyAudio != null)
            enemyAudio.SetMovementState(state);

        if (anim == null || anim.runtimeAnimatorController == null) return;
        if (state == lastAnimState) return;
        lastAnimState = state;

        if (HasAnimatorParameter("AnimState"))
        {
            anim.SetInteger("AnimState", state);
        }
        else if (HasAnimatorParameter("isMoving"))
        {
            anim.SetBool("isMoving", state == 1);
        }
        else if (HasAnimatorParameter("Speed"))
        {
            anim.SetFloat("Speed", state == 1 ? 1f : 0f);
        }
        else if (HasAnimatorParameter("Moving"))
        {
            anim.SetBool("Moving", state == 1);
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

    public bool TryAttack(int comboHit = 0)
    {
        if (attackTimer > 0) return false;

        if (team == CombatTeam.Player)
        {
            attackTimer = GetEffectiveAttackCooldown();
            return TryMultiAttack(comboHit);
        }

        if (target == null) return false;

        float distance = GetDistanceToTarget();
        if (distance > attackRange) return false;

        attackTimer = GetEffectiveAttackCooldown();

        int finalDamage = GetFinalDamage(out bool isCrit);
        int attackerLethality = GetLethality();
        bool isPlayer = team == CombatTeam.Player;

        AutoCombat targetCombat = FindAutoCombat(target);
        if (targetCombat != null)
        {
            targetCombat.TakeDamage(finalDamage, attackerLethality, element);
            ShowDamagePopup(GetPopupPosition(targetCombat.transform), finalDamage, isCrit, isPlayer);
            return true;
        }

        PlayerStats targetStats = FindPlayerStats(target);
        if (targetStats != null)
        {
            targetStats.TakeDamage(finalDamage, attackerLethality, element);
            ShowDamagePopup(GetPopupPosition(targetStats.transform), finalDamage, isCrit, isPlayer);
            return true;
        }

        Debug.LogWarning($"{name}: target {target.name} has no AutoCombat or PlayerStats!");
        return false;
    }

    bool HasAttackTargetInRange()
    {
        AutoCombat[] combats = FindObjectsByType<AutoCombat>();
        foreach (AutoCombat other in combats)
        {
            if (!IsValidAttackTarget(other)) continue;
            if (CanReachCombatColliders(other)) return true;
        }

        return false;
    }

    Collider2D[] GetAttackColliders()
    {
        return Physics2D.OverlapCircleAll(transform.position, team == CombatTeam.Player ? playerMeleeHitboxRange : attackRange);
    }

    bool IsValidAttackTarget(AutoCombat other)
    {
        return other != null && other != this && !other.isDead && other.canBeTargeted && IsValidTarget(other.team) && !HasIgnoreTag(other.transform);
    }

    bool CanReachCombatColliders(AutoCombat other)
    {
        Collider2D[] colliders = other.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            if (collider == null || collider.isTrigger) continue;
            if (IsColliderInAttackHitbox(collider)) return true;
        }
        return false;
    }

    bool IsColliderInAttackHitbox(Collider2D targetCollider)
    {
        if (targetCollider == null) return false;

        Collider2D myCollider = GetComponent<Collider2D>();
        float range = team == CombatTeam.Player ? playerMeleeHitboxRange : attackRange;
        float facing = Mathf.Sign(transform.localScale.x);
        float horizontalToTarget = targetCollider.bounds.center.x - transform.position.x;
        if (Mathf.Abs(horizontalToTarget) > 0.01f && Mathf.Sign(horizontalToTarget) != facing)
            return false;

        if (team == CombatTeam.Player && myCollider != null)
        {
            float verticalGap = Mathf.Max(0f, Mathf.Max(myCollider.bounds.min.y - targetCollider.bounds.max.y, targetCollider.bounds.min.y - myCollider.bounds.max.y));
            if (verticalGap > playerMeleeVerticalTolerance)
                return false;
        }

        if (myCollider != null)
        {
            ColliderDistance2D dist = myCollider.Distance(targetCollider);
            return dist.distance <= range;
        }

        return Vector2.Distance(transform.position, targetCollider.ClosestPoint(transform.position)) <= range;
    }

    bool TryMultiAttack(int comboHit = 0)
    {
        int finalDamage = GetFinalDamage(out bool isCrit, comboHit);

        if (comboHit == 3)
        {
            PlayerStats ps = GetComponent<PlayerStats>();
            int lck = ps != null ? ps.lck : 0;
            int bonus = Mathf.Max(1, Mathf.RoundToInt(finalDamage * lck * 0.005f));
            finalDamage += bonus;
        }

        int attackerLethality = GetLethality();
        bool isPlayer = team == CombatTeam.Player;
        bool hitAny = false;

        AutoCombat[] combats = FindObjectsByType<AutoCombat>();
        foreach (AutoCombat other in combats)
        {
            if (!IsValidAttackTarget(other)) continue;
            if (!CanReachCombatColliders(other)) continue;

            other.TakeDamage(finalDamage, attackerLethality, element);
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

    int GetFinalDamage(out bool isCrit, int comboHit = 0)
    {
        isCrit = false;
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null)
        {
            isCrit = Random.value < stats.GetCritChance();
            return isCrit ? Mathf.RoundToInt(stats.atk * stats.GetCritDamageMultiplier(comboHit)) : stats.atk;
        }
        return damage;
    }

    int GetFinalDamage()
    {
        return GetFinalDamage(out _, 0);
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

    int GetMitigatedDamage(int rawDamage, int attackerLethality, ElementalType attackElement = ElementalType.Physical)
    {
        int pureDamage = Mathf.Min(attackerLethality, rawDamage);
        int mitigatable = rawDamage - pureDamage;
        float effectiveDef = Mathf.Clamp(def, 0, 34221);
        float multiplier = Mathf.Pow(100f / (100f + effectiveDef), 0.774f);
        int mitigated = Mathf.RoundToInt(mitigatable * Mathf.Clamp01(multiplier));
        int damage = Mathf.Max(1, pureDamage + mitigated);

        if (elementalResistances == null || elementalResistances.Length == 0)
            elementalResistances = ElementalSystem.CreateEmptyResistances();

        return ElementalSystem.ApplyElemental(damage, attackElement, elementalResistances);
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

            float distance = Vector2.Distance(transform.position, root.position);

            AutoCombat other = FindAutoCombat(collider.transform);
            PlayerController playerController = collider.GetComponent<PlayerController>();
            if (playerController == null)
                playerController = collider.GetComponentInParent<PlayerController>();
            if (playerController == null)
                playerController = collider.GetComponentInChildren<PlayerController>();

            if (other == null && playerController == null) continue;
            if (other != null && (other == this || other.isDead || !other.canBeTargeted || !IsValidTarget(other.team) || HasIgnoreTag(other.transform))) continue;
            if (playerController != null && HasIgnoreTag(playerController.transform)) continue;
            if (team == CombatTeam.Enemy && !HasLineOfSight(collider.transform)) continue;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                target = root;
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
            hasSeenTarget = false;
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

    bool HasLineOfSight(Transform t)
    {
        if (!requireLineOfSight) return true;
        if (t == null) return false;

        int losLayer = groundLayer != 0 ? (int)groundLayer : LayerMask.GetMask("Ground");
        if (losLayer == 0) return true;

        Vector2 origin = transform.position;
        Vector2 targetPos = t.position;
        float distance = Vector2.Distance(origin, targetPos);
        if (distance < 0.01f) return true;
        Vector2 direction = (targetPos - origin).normalized;

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, losLayer);
        return hit.collider == null;
    }

    void Attack()
    {
        if (attackTimer > 0) return;

        attackTimer = GetEffectiveAttackCooldown();

        if (anim != null && anim.runtimeAnimatorController != null && HasAnimatorParameter(attackTrigger))
        {
            anim.SetTrigger(attackTrigger);
        }

        if (enemyAudio != null)
            enemyAudio.PlayAttack();

        if (isRanged && projectilePrefab != null)
        {
            FireProjectile();
            return;
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
            targetCombat.TakeDamage(finalDamage, attackerLethality, element);
            ShowDamagePopup(GetPopupPosition(targetCombat.transform), finalDamage, isCrit, team == CombatTeam.Player);
        }
    }

    void FireProjectile()
    {
        if (target == null || projectilePrefab == null) return;

        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Vector2 direction = (target.position - origin).normalized;

        GameObject go = Instantiate(projectilePrefab, origin, Quaternion.identity);
        EnemyProjectile projectile = go.GetComponent<EnemyProjectile>();
        if (projectile != null)
        {
            int dmg = inheritProjectileDamage ? damage : projectile.damage;
            projectile.Initialize(direction, target, dmg, projectileSpeed);
            projectile.element = element;
        }
        else
        {
            Rigidbody2D prb = go.GetComponent<Rigidbody2D>();
            if (prb != null)
                prb.linearVelocity = direction * projectileSpeed;
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

    public void TakeDamage(int amount, int attackerLethality = 0, ElementalType attackElement = ElementalType.Physical)
    {
        if (isDead) return;

        if (team == CombatTeam.Player)
        {
            PlayerController pc = GetComponentInChildren<PlayerController>();
            if (pc == null) pc = GetComponentInParent<PlayerController>();
            if (pc == null) pc = GetComponent<PlayerController>();
            if (pc != null && pc.isInvulnerable)
                return;
            if (pc != null && pc.isBlocking)
            {
                float multiplier = 1f - pc.blockDamageReduction;
                amount = Mathf.Max(1, Mathf.RoundToInt(amount * multiplier));
            }
        }

        int finalDamage = GetMitigatedDamage(amount, attackerLethality, attackElement);

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

        if (team == CombatTeam.Enemy)
        {
            if (enemyAudio != null)
                enemyAudio.PlayDeath();
            Bestiary.RegisterKill(name);
            
            // Отслеживаем убитого моба в статистике
            GameStatistics.Instance?.RecordEnemyKilled();
        }

        if (team == CombatTeam.Player)
        {
            PlayerStats stats = GetComponent<PlayerStats>();
            stats?.ApplyDeathPenalty();

            // Показываем панель смерти
            DeathPanel.Show(gameObject, stats, null);
            gameObject.SetActive(false);
            return;
        }

        GiveRewards();
        GiveDrops();

        if (anim != null && anim.runtimeAnimatorController != null && HasAnimatorParameter(deathTrigger))
            anim.SetTrigger(deathTrigger);

        // Отключаем управление игроком
        PlayerController pc = GetComponentInChildren<PlayerController>();
        if (pc != null)
            pc.enabled = false;

        PlayerAudio playerAudio = GetComponentInChildren<PlayerAudio>();
        playerAudio?.StopAll();

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