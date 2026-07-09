using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerProjectile : MonoBehaviour
{
    [Header("Base")]
    public int damage = 20;
    public float speed = 12f;
    public float lifetime = 3f;
    public bool destroyOnHit = true;
    public int pierceCount = 0;

    [Header("Element")]
    public ElementalType element = ElementalType.Fire;

    [Header("Physics")]
    public bool useGravity = false;
    public float gravityScale = 0f;

    [Header("Visual")]
    public Color projectileColor = new Color(1f, 0.45f, 0f, 1f);
    public bool rotateToVelocity = true;
    public float rotationOffset = 0f;

    [Header("Effects")]
    public GameObject impactEffect;
    public AudioClip impactSound;

    [Header("Targeting")]
    [Tooltip("Если true, не наносит урон CombatTeam.Player")]
    public bool ignorePlayerTeam = true;

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;
    private Vector2 currentDirection;
    private int runtimeDamage;
    private int pierceHits;
    private AudioSource audioSource;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        if (col != null)
            col.isTrigger = true;

        if (rb != null)
        {
            rb.gravityScale = useGravity ? gravityScale : 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (sr != null)
        {
            sr.color = projectileColor;
        }

        runtimeDamage = damage;
    }

    public void Initialize(Vector2 direction, int? damageOverride = null, float? speedOverride = null, float? lifetimeOverride = null)
    {
        currentDirection = direction.normalized;

        if (damageOverride.HasValue)
            runtimeDamage = damageOverride.Value;
        if (speedOverride.HasValue)
            speed = speedOverride.Value;
        if (lifetimeOverride.HasValue)
            lifetime = lifetimeOverride.Value;

        if (rotateToVelocity)
        {
            float angle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg + rotationOffset;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        rb.linearVelocity = currentDirection * speed;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger) return;
        if (other.CompareTag("Player")) return;

        AutoCombat autoCombat = other.GetComponent<AutoCombat>();
        if (autoCombat == null) autoCombat = other.GetComponentInParent<AutoCombat>();
        if (autoCombat == null) autoCombat = other.GetComponentInChildren<AutoCombat>();

        if (autoCombat != null)
        {
            if (ignorePlayerTeam && autoCombat.team == CombatTeam.Player)
                return;

            autoCombat.TakeDamage(runtimeDamage, 0, element);
            Hit(other);
            return;
        }

        PlayerStats playerStats = other.GetComponent<PlayerStats>();
        if (playerStats == null) playerStats = other.GetComponentInParent<PlayerStats>();
        if (playerStats == null) playerStats = other.GetComponentInChildren<PlayerStats>();

        if (playerStats != null)
        {
            playerStats.TakeDamage(runtimeDamage, 0, element);
            Hit(other);
        }
    }

    void Hit(Collider2D other)
    {
        if (impactEffect != null)
            Instantiate(impactEffect, transform.position, Quaternion.identity);

        if (impactSound != null && audioSource != null)
            audioSource.PlayOneShot(impactSound);

        pierceHits++;
        if (pierceHits > pierceCount && destroyOnHit)
        {
            Destroy(gameObject);
        }
    }

    public void SetDamage(int value) => runtimeDamage = value;
    public void SetSpeed(float value) => speed = value;
    public void SetColor(Color color)
    {
        projectileColor = color;
        if (sr != null) sr.color = color;
    }
}
