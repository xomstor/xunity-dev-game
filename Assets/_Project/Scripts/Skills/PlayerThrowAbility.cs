using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerThrowAbility : PlayerSkillInstance
{
    [Header("References")]
    [Tooltip("Точка вылета. Если null, берется transform.position + offset из SkillData")]
    public Transform firePoint;

    private PlayerController playerController;
    private AudioSource audioSource;
    private bool inputHeld;

    protected override void Awake()
    {
        base.Awake();
        playerController = GetComponent<PlayerController>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            PlayerAudio pa = GetComponent<PlayerAudio>();
            if (pa != null) audioSource = pa.audioSource;
        }
    }

    protected override void Update()
    {
        base.Update();
        if (Data == null) return;

        var keyboard = Keyboard.current;
        if ((keyboard != null && keyboard[Data.throwKey].isPressed) || inputHeld)
            TryUse();
    }

    public override bool TryUse()
    {
        if (Data == null) return false;
        if (cooldownTimer > 0f) return false;
        if (Data.projectilePrefab == null)
        {
            Debug.LogWarning("[PlayerThrowAbility] projectilePrefab is not assigned in SkillData.");
            return false;
        }
        if (playerController != null && (playerController.IsInputBlocked || playerController.IsRolling || playerController.isBlocking)) return false;

        Vector2 direction = GetThrowDirection();
        if (direction == Vector2.zero)
            direction = Vector2.right * Mathf.Sign(transform.localScale.x);

        Vector3 spawnPos = firePoint != null
            ? firePoint.position
            : (Vector3)(Vector2)transform.position + (Vector3)(direction * Data.fireOffset.x + Vector2.up * Data.fireOffset.y);

        PlayerProjectile projectile = Instantiate(Data.projectilePrefab, spawnPos, Quaternion.identity);
        projectile.element = Data.projectileElement;
        projectile.Initialize(direction, GetCurrentDamage(), Data.projectileSpeed, Data.projectileLifetime);

        if (Data.muzzleEffectPrefab != null)
            Instantiate(Data.muzzleEffectPrefab, spawnPos, Quaternion.identity);
        if (Data.projectileEffectPrefab != null)
            AttachEffect(projectile.transform);

        if (Data.castSound != null && audioSource != null)
            audioSource.PlayOneShot(Data.castSound);

        cooldownTimer = GetCurrentCooldown();
        return true;
    }

    void AttachEffect(Transform projectileTransform)
    {
        GameObject effect = Instantiate(Data.projectileEffectPrefab, projectileTransform);
        effect.transform.localPosition = Vector3.zero;
        effect.transform.localRotation = Quaternion.identity;
    }

    Vector2 GetThrowDirection()
    {
        if (playerController == null || Data == null) return Vector2.right;

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

        if (Data.useFacingDirection)
            return new Vector2(Mathf.Sign(transform.localScale.x), 0f);

        return Vector2.right;
    }

    public void SetInputHeld(bool held) => inputHeld = held;
}
