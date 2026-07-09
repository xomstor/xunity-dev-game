using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BossProjectile : MonoBehaviour
{
    [Header("Base")]
    public int damage = 10;
    public float speed = 5f;
    public float lifetime = 8f;
    public bool canBeDestroyed = true;
    public bool destroyOnHit = true;

    [Header("Element")]
    public ElementalType element = ElementalType.Physical;

    [Header("Homing")]
    public bool homing = false;
    public float homingStrength = 30f;
    public float homingDelay = 0.3f;

    [Header("Visual")]
    public Color projectileColor = new Color(1f, 0.25f, 0f, 1f);

    private Rigidbody2D rb;
    private Collider2D col;
    private Transform target;
    private Vector2 currentDirection;
    private float spawnTime;
    private SpriteRenderer sr;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = projectileColor;
        if (col != null)
            col.isTrigger = true;
    }

    public void Initialize(Vector2 direction, Transform targetTransform = null)
    {
        currentDirection = direction.normalized;
        target = targetTransform;
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
        if (other.CompareTag("BossProjectile")) return;

        AutoCombat autoCombat = other.GetComponent<AutoCombat>();
        if (autoCombat == null) autoCombat = other.GetComponentInParent<AutoCombat>();
        if (autoCombat == null) autoCombat = other.GetComponentInChildren<AutoCombat>();

        if (autoCombat != null && autoCombat.team == CombatTeam.Player)
        {
            autoCombat.TakeDamage(damage, 0, element);
            if (destroyOnHit)
                Destroy(gameObject);
            return;
        }

        PlayerStats playerStats = other.GetComponent<PlayerStats>();
        if (playerStats == null) playerStats = other.GetComponentInParent<PlayerStats>();
        if (playerStats == null) playerStats = other.GetComponentInChildren<PlayerStats>();

        if (playerStats != null)
        {
            playerStats.TakeDamage(damage, 0, element);
            if (destroyOnHit)
                Destroy(gameObject);
        }
    }

    public void SetColor(Color color)
    {
        if (sr != null)
            sr.color = color;
        projectileColor = color;
    }
}
