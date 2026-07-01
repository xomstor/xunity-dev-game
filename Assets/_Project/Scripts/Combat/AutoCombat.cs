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
    public string[] ignoreTags = new string[] { "World" };

    [Header("Stats")]
    public int maxHealth = 100;
    public int damage = 10;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    public float detectionRadius = 15f;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public bool chaseTarget = true;

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
    private bool isDead;
    private Rigidbody2D rb;

    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    public void ResetHealth()
    {
        isDead = false;
        currentHealth = maxHealth;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = true;

        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }
    }

    void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = GetComponentInParent<Rigidbody2D>();
        if (anim == null)
            anim = GetComponent<Animator>();
        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null)
        {
            maxHealth = stats.maxHp;
            currentHealth = stats.hp;
            damage = stats.atk;
        }

        if (team == CombatTeam.Enemy && chaseTarget && rb == null)
            Debug.LogWarning($"{name}: chaseTarget is enabled but no Rigidbody2D found. Enemy will not move!");

        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
        bool hasNonTrigger = false;
        foreach (Collider2D c in cols)
        {
            if (!c.isTrigger) { hasNonTrigger = true; break; }
        }
        if (!hasNonTrigger && team == CombatTeam.Enemy)
            Debug.LogWarning($"{name}: has no non-trigger collider! Player cannot auto-target this enemy.");
    }

    void Update()
    {
        if (isDead) return;

        if (attackTimer > 0)
            attackTimer -= Time.deltaTime;

        FindTarget();

        PlayerController playerController = GetComponent<PlayerController>();

        if (target != null)
        {
            float distance = GetDistanceToTarget();

            if (distance <= attackRange)
            {
                if (playerController != null)
                {
                    playerController.Attack();
                    SetAnimState(0);
                }
                else
                {
                    Attack();
                    SetAnimState(0);
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
            SetAnimState(0);
        }
    }

    float GetDistanceToTarget()
    {
        if (target == null) return float.MaxValue;
        Collider2D targetCollider = target.GetComponent<Collider2D>();
        if (targetCollider != null)
            return Vector2.Distance(transform.position, targetCollider.ClosestPoint(transform.position));
        return Vector2.Distance(transform.position, target.position);
    }

    void SetAnimState(int state)
    {
        if (anim == null) return;
        if (!HasAnimatorParameter("AnimState")) return;
        anim.SetInteger("AnimState", state);
    }

    bool HasAnimatorParameter(string paramName)
    {
        if (anim == null) return false;
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }

    public void TryAttack()
    {
        if (attackTimer > 0) return;
        if (target == null) return;

        float distance = GetDistanceToTarget();
        if (distance > attackRange) return;

        attackTimer = attackCooldown;

        int finalDamage = GetFinalDamage(out bool isCrit);
        bool isPlayer = team == CombatTeam.Player;

        AutoCombat targetCombat = FindAutoCombat(target);
        if (targetCombat != null)
        {
            targetCombat.TakeDamage(finalDamage);
            ShowDamagePopup(GetPopupPosition(targetCombat.transform), finalDamage, isCrit, isPlayer);
            return;
        }

        PlayerStats targetStats = FindPlayerStats(target);
        if (targetStats != null)
        {
            targetStats.TakeDamage(finalDamage);
            ShowDamagePopup(GetPopupPosition(targetStats.transform), finalDamage, isCrit, isPlayer);
            return;
        }

        Debug.LogWarning($"{name}: target {target.name} has no AutoCombat or PlayerStats!");
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
        if (DamagePopupManager.Instance == null)
        {
            Debug.LogError($"{name}: DamagePopupManager not found in scene!");
            return;
        }
        DamagePopupManager.Instance.ShowDamage(position, damage, isCrit, isPlayer);
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
        Vector2 direction = (target.position - transform.position).normalized;

        // ✅ ИСПРАВЛЕНО: velocity вместо linearVelocity
        rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);

        // Поворот спрайта в направлении движения
        if (direction.x > 0.1f)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
        else if (direction.x < -0.1f)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
    }

    void Attack()
    {
        if (attackTimer > 0) return;

        attackTimer = attackCooldown;

        if (anim != null)
        {
            anim.SetTrigger(attackTrigger);
        }

        AutoCombat targetCombat = FindAutoCombat(target);
        if (targetCombat != null)
        {
            targetCombat.TakeDamage(damage);
            ShowDamagePopup(GetPopupPosition(targetCombat.transform), damage, false, team == CombatTeam.Player);
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        // ✅ Защита от отрицательного здоровья
        currentHealth = Mathf.Max(0, currentHealth - amount);

        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null)
            stats.hp = currentHealth;

        if (anim != null)
        {
            string trigger = GetComponentInChildren<PlayerController>() != null ? "Hurt" : hitTrigger;
            anim.SetTrigger(trigger);
        }

        PlayerAudio playerAudio = GetComponent<PlayerAudio>();
        if (playerAudio != null)
            playerAudio.PlayHurt();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;

        GiveRewards();
        GiveDrops();

        if (anim != null)
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

        EnemyRespawnManager.Instance?.RegisterDeath(this);
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

        foreach (DropItem drop in reward.Drops)
        {
            float chance = Mathf.Min(drop.baseDropChance * luckMultiplier, 1f);
            if (Random.value <= chance && drop.itemData != null)
            {
                playerInventory?.AddItem(drop.itemData, drop.quantity);
                Debug.Log($"Dropped: {drop.itemData.itemName} x{drop.quantity}");
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