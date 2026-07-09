using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerThrowAbility : MonoBehaviour
{
    [Header("Projectile")]
    [Tooltip("Префаб с компонентом PlayerProjectile")]
    public PlayerProjectile projectilePrefab;
    [Tooltip("Точка вылета. Если null, берется transform.position + offset")]
    public Transform firePoint;
    [Tooltip("Смещение точки вылета относительно игрока, если firePoint не назначен")]
    public Vector2 fireOffset = new Vector2(0.5f, 0.2f);

    [Header("Skill Stats")]
    public float cooldown = 1f;
    public int baseDamage = 20;
    public float projectileSpeed = 12f;
    public float projectileLifetime = 3f;
    public bool usePlayerStats = true;
    public bool useFacingDirection = true;

    [Header("Element")]
    public ElementalType projectileElement = ElementalType.Fire;

    [Header("Input")]
    [Tooltip("Клавиша для броска (по умолчанию L). Можно вызывать TryUse() из UI/скриптов.")]
    public Key throwKey = Key.L;

    [Header("Audio")]
    public AudioClip throwSound;

    private float cooldownTimer;
    private PlayerController playerController;
    private PlayerStats playerStats;
    private AudioSource audioSource;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerStats = GetComponent<PlayerStats>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            PlayerAudio pa = GetComponent<PlayerAudio>();
            if (pa != null) audioSource = pa.audioSource;
        }
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard[throwKey].wasPressedThisFrame)
        {
            TryUse();
        }
    }

    public bool TryUse()
    {
        if (cooldownTimer > 0f) return false;
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[PlayerThrowAbility] projectilePrefab не назначен.");
            return false;
        }
        if (playerController != null && (playerController.IsInputBlocked || playerController.IsRolling || playerController.isBlocking)) return false;

        Vector2 direction = GetThrowDirection();
        if (direction == Vector2.zero)
            direction = Vector2.right * Mathf.Sign(transform.localScale.x);

        Vector3 spawnPos = firePoint != null
            ? firePoint.position
            : (Vector3)(Vector2)transform.position + (Vector3)(direction * fireOffset.x + Vector2.up * fireOffset.y);

        PlayerProjectile projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        projectile.element = projectileElement;

        int damage = baseDamage;
        if (usePlayerStats && playerStats != null)
        {
            bool isCrit;
            damage = playerStats.GetDamage(out isCrit, 0);
        }

        projectile.Initialize(direction, damage, projectileSpeed, projectileLifetime);

        if (throwSound != null && audioSource != null)
            audioSource.PlayOneShot(throwSound);

        cooldownTimer = cooldown;
        return true;
    }

    public bool TryUse(Vector2 direction)
    {
        if (cooldownTimer > 0f) return false;
        if (projectilePrefab == null) return false;

        Vector3 spawnPos = firePoint != null
            ? firePoint.position
            : (Vector3)(Vector2)transform.position + (Vector3)(direction * fireOffset.x + Vector2.up * fireOffset.y);

        PlayerProjectile projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        projectile.element = projectileElement;

        int damage = baseDamage;
        if (usePlayerStats && playerStats != null)
        {
            bool isCrit;
            damage = playerStats.GetDamage(out isCrit, 0);
        }

        projectile.Initialize(direction, damage, projectileSpeed, projectileLifetime);

        if (throwSound != null && audioSource != null)
            audioSource.PlayOneShot(throwSound);

        cooldownTimer = cooldown;
        return true;
    }

    Vector2 GetThrowDirection()
    {
        if (playerController != null)
        {
            VirtualJoystick joystick = FindAnyObjectByType<VirtualJoystick>();
            if (joystick != null && joystick.IsUsable)
            {
                Vector2 joy = joystick.GetInput();
                if (joy.magnitude > 0.5f)
                    return joy.normalized;
            }

            float move = playerController.MoveInput;
            if (move != 0f)
                return new Vector2(Mathf.Sign(move), 0f);
        }

        if (useFacingDirection)
        {
            return new Vector2(Mathf.Sign(transform.localScale.x), 0f);
        }

        return Vector2.right;
    }
}
