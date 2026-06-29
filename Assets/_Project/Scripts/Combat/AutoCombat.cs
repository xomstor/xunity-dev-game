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

    void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        if (anim == null)
            anim = GetComponent<Animator>();
        if (anim == null)
            anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (isDead) return;

        if (attackTimer > 0)
            attackTimer -= Time.deltaTime;

        FindTarget();

        if (target != null)
        {
            float distance = Vector2.Distance(transform.position, target.position);

            if (distance <= attackRange)
            {
                Attack();
                if (anim != null) anim.SetInteger("AnimState", 0);
            }
            else if (chaseTarget && rb != null)
            {
                ChaseTarget();
                if (anim != null) anim.SetInteger("AnimState", 1);
            }
        }
        else
        {
            if (anim != null) anim.SetInteger("AnimState", 0);
        }
    }

    void FindTarget()
    {
        // Check if target is alive
        if (target != null && target.gameObject.activeInHierarchy)
        {
            AutoCombat targetCombat = target.GetComponent<AutoCombat>();
            if (targetCombat == null || !targetCombat.IsDead)
                return; // target still valid
        }

        target = null;
        float closestDistance = detectionRadius;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
        foreach (Collider2D collider in colliders)
        {
            AutoCombat other = collider.GetComponent<AutoCombat>();
            if (other == null || other == this || other.isDead) continue;
            if (IsAlly(other.team)) continue;

            float distance = Vector2.Distance(transform.position, collider.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                target = collider.transform;
            }
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

        if (anim != null)
            anim.SetTrigger(hitTrigger);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;

        if (anim != null)
            anim.SetTrigger(deathTrigger);

        if (rb != null)
        {
            // ✅ ИСПРАВЛЕНО: velocity вместо linearVelocity
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        // ✅ ИСПРАВЛЕНИЕ: отключаем коллайдер для всех, а не только для Enemy
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        // Опционально: отключаем этот скрипт
        // this.enabled = false;
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