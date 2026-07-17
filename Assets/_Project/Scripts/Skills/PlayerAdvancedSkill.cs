using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAdvancedSkill : PlayerSkillInstance
{
    public Transform firePoint;

    private PlayerController playerController;
    private Coroutine activeAreaRoutine;

    protected override void Awake()
    {
        base.Awake();
        playerController = GetComponent<PlayerController>();
    }

    public override bool TryUse()
    {
        if (Data == null || cooldownTimer > 0f) return false;
        if (playerController != null && (playerController.IsInputBlocked || playerController.IsRolling || playerController.isBlocking)) return false;

        Vector2 direction = GetDirection();
        Vector3 origin = firePoint != null ? firePoint.position : transform.position + (Vector3)(direction * Data.fireOffset.x + Vector2.up * Data.fireOffset.y);
        bool used = Data.behavior switch
        {
            SkillBehavior.Projectile => UseProjectile(origin, direction),
            SkillBehavior.SpawnObject => UseSpawnObject(origin, direction),
            SkillBehavior.AreaEffect => UseAreaEffect(origin),
            SkillBehavior.Decoy => UseDecoy(origin),
            _ => false
        };

        if (used)
            cooldownTimer = GetCurrentCooldown();
        return used;
    }

    bool UseProjectile(Vector3 origin, Vector2 direction)
    {
        if (Data.projectilePrefab == null) return false;
        PlayerProjectile projectile = Instantiate(Data.projectilePrefab, origin, Quaternion.identity);
        projectile.element = Data.projectileElement;
        projectile.Initialize(direction, GetCurrentDamage(), Data.projectileSpeed, Data.projectileLifetime);
        return true;
    }

    bool UseSpawnObject(Vector3 origin, Vector2 direction)
    {
        if (Data.specialPrefab == null) return false;
        GameObject spawned = Instantiate(Data.specialPrefab, origin, Quaternion.identity);
        spawned.transform.localScale = new Vector3(Mathf.Sign(direction.x) * Mathf.Abs(spawned.transform.localScale.x), spawned.transform.localScale.y, spawned.transform.localScale.z);
        Destroy(spawned, Data.effectDuration);
        return true;
    }

    bool UseDecoy(Vector3 origin)
    {
        if (Data.specialPrefab == null) return false;
        GameObject decoy = Instantiate(Data.specialPrefab, origin, Quaternion.identity);
        PlayerStealthState stealth = GetComponent<PlayerStealthState>();
        if (stealth == null) stealth = gameObject.AddComponent<PlayerStealthState>();
        stealth.SetStealth(Data.effectDuration);
        Destroy(decoy, Data.effectDuration);
        return true;
    }

    bool UseAreaEffect(Vector3 origin)
    {
        if (activeAreaRoutine != null) StopCoroutine(activeAreaRoutine);
        activeAreaRoutine = StartCoroutine(AreaEffectRoutine(origin));
        return true;
    }

    IEnumerator AreaEffectRoutine(Vector3 origin)
    {
        float elapsed = 0f;
        float tickTimer = 0f;
        while (elapsed < Data.effectDuration)
        {
            float delta = Time.deltaTime;
            elapsed += delta;
            tickTimer -= delta;
            if (tickTimer <= 0f)
            {
                tickTimer = Mathf.Max(0.05f, Data.effectTickRate);
                Collider2D[] hits = Physics2D.OverlapCircleAll(origin, Data.effectRadius);
                HashSet<AutoCombat> damaged = new HashSet<AutoCombat>();
                foreach (Collider2D hit in hits)
                {
                    AutoCombat combat = hit.GetComponent<AutoCombat>();
                    if (combat == null) combat = hit.GetComponentInParent<AutoCombat>();
                    if (combat == null || combat.team == CombatTeam.Player || damaged.Contains(combat)) continue;
                    damaged.Add(combat);
                    combat.TakeDamage(GetCurrentDamage(), 0, Data.projectileElement);
                }
            }
            yield return null;
        }
        activeAreaRoutine = null;
    }

    Vector2 GetDirection()
    {
        if (playerController == null) return Vector2.right;
        float move = playerController.MoveInput;
        if (move != 0f) return new Vector2(Mathf.Sign(move), 0f);
        return new Vector2(Mathf.Sign(transform.localScale.x), 0f);
    }

    void OnDrawGizmosSelected()
    {
        if (Data == null || Data.behavior != SkillBehavior.AreaEffect) return;
        Gizmos.color = new Color(0.2f, 0.8f, 0.3f, 0.35f);
        Gizmos.DrawSphere(transform.position, Data.effectRadius);
    }
}

public class PlayerStealthState : MonoBehaviour
{
    public bool IsStealthed { get; private set; }
    public float RemainingTime { get; private set; }

    public void SetStealth(float duration)
    {
        IsStealthed = true;
        RemainingTime = Mathf.Max(RemainingTime, duration);
    }

    void Update()
    {
        if (!IsStealthed) return;
        RemainingTime -= Time.deltaTime;
        if (RemainingTime <= 0f)
        {
            RemainingTime = 0f;
            IsStealthed = false;
        }
    }
}
