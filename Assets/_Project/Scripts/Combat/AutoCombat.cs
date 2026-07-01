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
            float distance = Vector2.Distance(transform.position, target.position);

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

        float distance = Vector2.Distance(transform.position, target.position);
        if (distance > attackRange) return;

        attackTimer = attackCooldown;

        int finalDamage = GetFinalDamage();

        AutoCombat targetCombat = target.GetComponentInChildren<AutoCombat>();
        if (targetCombat != null)
        {
            targetCombat.TakeDamage(finalDamage);
            return;
        }

        PlayerStats targetStats = target.GetComponentInChildren<PlayerStats>();
        if (targetStats != null)
        {
            targetStats.TakeDamage(finalDamage);
            Debug.Log($"{target.name} took {finalDamage} damage");
        }
    }

    int GetFinalDamage()
    {
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null)
        {
            bool isCrit = Random.value < stats.GetCritChance();
            int dmg = isCrit ? Mathf.RoundToInt(stats.atk * stats.critDamageMultiplier) : stats.atk;
            Debug.Log($"{name} attacks for {dmg} damage{(isCrit ? " (CRIT!)" : "")}");
            return dmg;
        }
        return damage;
    }

    void FindTarget()
    {
        // Check if target is alive and not destroyed
        if (target != null && target.gameObject.activeInHierarchy)
        {
            AutoCombat targetCombat = target.GetComponentInParent<AutoCombat>();
            if (targetCombat == null || !targetCombat.IsDead)
            {
                float distance = Vector2.Distance(transform.position, target.position);
                if (distance <= detectionRadius)
                    return; // target still valid and within range
            }
        }

        Transform previousTarget = target;
        target = null;
        float closestDistance = detectionRadius;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
        foreach (Collider2D collider in colliders)
        {
            Transform root = collider.transform.root;
            if (root == transform.root) continue;

            AutoCombat other = root.GetComponentInChildren<AutoCombat>();
            PlayerController playerController = root.GetComponentInChildren<PlayerController>();

            if (other == null && playerController == null) continue;
            if (other != null && (other == this || other.isDead || IsAlly(other.team))) continue;

            float distance = Vector2.Distance(transform.position, collider.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                target = root;
            }
        }

        if (target != previousTarget && Application.isEditor)
        {
            if (target != null)
                Debug.Log($"{name}: found target {target.name}");
            else
                Debug.Log($"{name}: no target found within {detectionRadius}");
        }
    }

    bool IsAlly(CombatTeam otherTeam)
    {
        if (team == CombatTeam.Neutral || otherTeam == CombatTeam.Neutral)
            return false;
        return team == otherTeam;
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
            Debug.Log($"{name}: Attack trigger set");
        }

        AutoCombat targetCombat = target?.GetComponent<AutoCombat>();
        if (targetCombat != null)
        {
            targetCombat.TakeDamage(damage);
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
            string trigger = GetComponent<PlayerController>() != null ? "Hurt" : hitTrigger;
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