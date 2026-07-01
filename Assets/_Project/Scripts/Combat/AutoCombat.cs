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
    public float detectionRadius = 8f;

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
            ShowDamagePopup(target.position, finalDamage, isCrit, isPlayer);
            Debug.Log($"{target.name} took {finalDamage} damage");
            return;
        }

        PlayerStats targetStats = FindPlayerStats(target);
        if (targetStats != null)
        {
            targetStats.TakeDamage(finalDamage);
            ShowDamagePopup(target.position, finalDamage, isCrit, isPlayer);
            Debug.Log($"{target.name} took {finalDamage} damage");
            return;
        }

        Debug.LogWarning($"{name}: target {target.name} has no AutoCombat or PlayerStats!");
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
            int dmg = isCrit ? Mathf.RoundToInt(stats.atk * stats.critDamageMultiplier) : stats.atk;
            Debug.Log($"{name} attacks for {dmg} damage{(isCrit ? " (CRIT!)" : "")}");
            return dmg;
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
        // Check if target is alive and not destroyed
        if (target != null && target.gameObject.activeInHierarchy)
        {
            AutoCombat targetCombat = FindAutoCombat(target);
            if (targetCombat == null || !targetCombat.IsDead)
            {
                if (GetDistanceToTarget() <= detectionRadius)
                    return; // target still valid and within range
            }
        }

        Transform previousTarget = target;
        target = null;
        float closestDistance = detectionRadius;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
        foreach (Collider2D collider in colliders)
        {
            if (collider.isTrigger) continue;

            Transform root = collider.transform.root;
            if (root == transform.root) continue;

            AutoCombat other = root.GetComponentInChildren<AutoCombat>();
            PlayerController playerController = root.GetComponentInChildren<PlayerController>();

            if (other == null && playerController == null) continue;
            if (other != null && (other == this || other.isDead || !other.canBeTargeted || !IsValidTarget(other.team) || HasIgnoreTag(other.transform))) continue;
            if (playerController != null && HasIgnoreTag(playerController.transform)) continue;

            float distance = Vector2.Distance(transform.position, collider.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                target = collider.transform;
            }
        }

        if (target != previousTarget && Application.isEditor)
        {
            if (target != null)
                Debug.Log($"{name}: found target {target.name} ({closestDistance:F2})");
            else
                Debug.Log($"{name}: no target found within {detectionRadius}");
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
            ShowDamagePopup(target.position, damage, false, team == CombatTeam.Player);
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

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

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