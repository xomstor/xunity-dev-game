using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Base")]
    public int damage = 10;
    public float speed = 8f;
    public float lifetime = 5f;
    public bool destroyOnHit = true;

    [Header("Homing")]
    public bool homing = false;
    public float homingStrength = 30f;
    public float homingDelay = 0.3f;

    [Header("Visual")]
    public Color projectileColor = Color.red;

    [Header("Effects")]
    public GameObject impactEffect;

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;
    private Transform target;
    private Vector2 currentDirection;
    private float spawnTime;
    private int runtimeDamage;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        if (col != null)
            col.isTrigger = true;
        if (sr != null)
            sr.color = projectileColor;
        runtimeDamage = damage;
    }

    public void Initialize(Vector2 direction, Transform targetTransform = null, int? damageOverride = null, float? speedOverride = null)
    {
        currentDirection = direction.normalized;
        target = targetTransform;
        if (damageOverride.HasValue)
            runtimeDamage = damageOverride.Value;
        if (speedOverride.HasValue)
            speed = speedOverride.Value;
        spawnTime = Time.time;
    }

    void Start()
    {
        if (target == null)
        {
            PlayerController pc = FindAnyObjectByType<PlayerController>();
            if (pc != null)
                target = pc.transform;
        }
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (homing && target != null && Time.time - spawnTime > homingDelay)
        {
            Vector2 toTarget = (target.position - transform.position).normalized;
            currentDirection = Vector2.MoveTowards(currentDirection, toTarget, homingStrength * Time.deltaTime).normalized;
        }
        rb.linearVelocity = currentDirection * speed;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger) return;

        AutoCombat autoCombat = other.GetComponent<AutoCombat>();
        if (autoCombat == null) autoCombat = other.GetComponentInParent<AutoCombat>();
        if (autoCombat == null) autoCombat = other.GetComponentInChildren<AutoCombat>();

        if (autoCombat != null && autoCombat.team == CombatTeam.Player)
        {
            autoCombat.TakeDamage(runtimeDamage, 0);
            Hit();
            return;
        }

        PlayerStats playerStats = other.GetComponent<PlayerStats>();
        if (playerStats == null) playerStats = other.GetComponentInParent<PlayerStats>();
        if (playerStats == null) playerStats = other.GetComponentInChildren<PlayerStats>();

        if (playerStats != null)
        {
            playerStats.TakeDamage(runtimeDamage, 0);
            Hit();
        }
    }

    void Hit()
    {
        if (impactEffect != null)
            Instantiate(impactEffect, transform.position, Quaternion.identity);
        if (destroyOnHit)
            Destroy(gameObject);
    }

    public void SetDamage(int value) => runtimeDamage = value;
    public void SetSpeed(float value) => speed = value;
    public void SetColor(Color color)
    {
        projectileColor = color;
        if (sr != null) sr.color = color;
    }
}
