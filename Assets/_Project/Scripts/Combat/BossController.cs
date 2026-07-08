using System.Collections.Generic;
using UnityEngine;

public enum BossAttackPattern
{
    CircleBurst,
    AimedBurst,
    SpreadFan,
    HomingOrbs
}

public class BossController : MonoBehaviour
{
    [Header("Refs")]
    public AutoCombat autoCombat;
    public SpriteRenderer spriteRenderer;
    public BossProjectile projectilePrefab;
    public Transform firePoint;

    [Header("Phases")]
    [Range(0f, 1f)] public float phase1Threshold = 0.7f;
    [Range(0f, 1f)] public float phase2Threshold = 0.3f;

    [Header("Timing")]
    public float patternIntervalPhase1 = 3.5f;
    public float patternIntervalPhase2 = 2.5f;
    public float patternIntervalPhase3 = 1.8f;

    [Header("Pattern Settings")]
    public int circleBurstCount = 10;
    public int aimedBurstCount = 4;
    public int spreadFanCount = 5;
    public int homingOrbCount = 2;

    [Header("Damage / Speed")]
    public int projectileDamage = 15;
    public float projectileSpeed = 6f;
    public float homingProjectileSpeed = 3.5f;

    [Header("Visual")]
    public Color phase1Color = new Color(1f, 0.4f, 0f, 1f);
    public Color phase2Color = new Color(1f, 0.2f, 0f, 1f);
    public Color phase3Color = new Color(1f, 0f, 0.2f, 1f);

    private Transform player;
    private float patternTimer;
    private int currentPhase = 1;
    private List<BossAttackPattern> availablePatterns = new List<BossAttackPattern>();

    void Awake()
    {
        if (autoCombat == null)
            autoCombat = GetComponent<AutoCombat>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (firePoint == null)
            firePoint = transform;

        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null)
            player = pc.transform;

        UpdatePhasePatterns();
        patternTimer = patternIntervalPhase1;
    }

    void Update()
    {
        if (autoCombat != null && autoCombat.IsDead) return;

        UpdatePhase();
        FacePlayer();

        patternTimer -= Time.deltaTime;
        if (patternTimer <= 0f)
        {
            FireNextPattern();
            patternTimer = GetCurrentInterval();
        }
    }

    void UpdatePhase()
    {
        if (autoCombat == null) return;

        float hpPercent = (float)autoCombat.CurrentHealth / Mathf.Max(1, autoCombat.maxHealth);
        int newPhase = hpPercent > phase1Threshold ? 1 : (hpPercent > phase2Threshold ? 2 : 3);
        if (newPhase != currentPhase)
        {
            currentPhase = newPhase;
            UpdatePhasePatterns();
            UpdateColor();
        }
    }

    void UpdatePhasePatterns()
    {
        availablePatterns.Clear();
        switch (currentPhase)
        {
            case 1:
                availablePatterns.Add(BossAttackPattern.CircleBurst);
                availablePatterns.Add(BossAttackPattern.AimedBurst);
                break;
            case 2:
                availablePatterns.Add(BossAttackPattern.CircleBurst);
                availablePatterns.Add(BossAttackPattern.AimedBurst);
                availablePatterns.Add(BossAttackPattern.SpreadFan);
                break;
            case 3:
                availablePatterns.Add(BossAttackPattern.CircleBurst);
                availablePatterns.Add(BossAttackPattern.AimedBurst);
                availablePatterns.Add(BossAttackPattern.SpreadFan);
                availablePatterns.Add(BossAttackPattern.HomingOrbs);
                break;
        }
    }

    void UpdateColor()
    {
        if (spriteRenderer == null) return;
        Color targetColor = currentPhase == 1 ? phase1Color : (currentPhase == 2 ? phase2Color : phase3Color);
        spriteRenderer.color = targetColor;
    }

    float GetCurrentInterval()
    {
        return currentPhase == 1 ? patternIntervalPhase1 : (currentPhase == 2 ? patternIntervalPhase2 : patternIntervalPhase3);
    }

    void FacePlayer()
    {
        if (player == null || spriteRenderer == null) return;
        float dir = player.position.x - transform.position.x;
        if (dir != 0)
            spriteRenderer.flipX = dir < 0;
    }

    void FireNextPattern()
    {
        if (availablePatterns.Count == 0) return;
        BossAttackPattern pattern = availablePatterns[Random.Range(0, availablePatterns.Count)];
        switch (pattern)
        {
            case BossAttackPattern.CircleBurst:
                FireCircleBurst();
                break;
            case BossAttackPattern.AimedBurst:
                FireAimedBurst();
                break;
            case BossAttackPattern.SpreadFan:
                FireSpreadFan();
                break;
            case BossAttackPattern.HomingOrbs:
                FireHomingOrbs();
                break;
        }
    }

    void FireCircleBurst()
    {
        Vector2 origin = firePoint.position;
        float angleStep = 360f / circleBurstCount;
        for (int i = 0; i < circleBurstCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            SpawnProjectile(dir, projectileSpeed, projectileDamage);
        }
    }

    void FireAimedBurst()
    {
        if (player == null) return;
        Vector2 origin = firePoint.position;
        Vector2 baseDir = (player.position - transform.position).normalized;
        for (int i = 0; i < aimedBurstCount; i++)
        {
            float spread = (i - (aimedBurstCount - 1) * 0.5f) * 6f * Mathf.Deg2Rad;
            Vector2 dir = RotateVector(baseDir, spread);
            SpawnProjectile(dir, projectileSpeed * 1.2f, projectileDamage);
        }
    }

    void FireSpreadFan()
    {
        if (player == null) return;
        Vector2 origin = firePoint.position;
        Vector2 baseDir = (player.position - transform.position).normalized;
        float fanAngle = 60f * Mathf.Deg2Rad;
        float startAngle = -fanAngle * 0.5f;
        float step = fanAngle / Mathf.Max(1, spreadFanCount - 1);
        for (int i = 0; i < spreadFanCount; i++)
        {
            float angle = startAngle + i * step;
            Vector2 dir = RotateVector(baseDir, angle);
            SpawnProjectile(dir, projectileSpeed, projectileDamage);
        }
    }

    void FireHomingOrbs()
    {
        for (int i = 0; i < homingOrbCount; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            var proj = SpawnProjectile(dir, homingProjectileSpeed, projectileDamage * 2);
            if (proj != null)
            {
                proj.homing = true;
                proj.homingStrength = 60f;
                proj.homingDelay = 0.4f;
            }
        }
    }

    BossProjectile SpawnProjectile(Vector2 direction, float speed, int dmg)
    {
        if (projectilePrefab == null) return null;
        BossProjectile proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        proj.damage = dmg;
        proj.speed = speed;
        proj.Initialize(direction, player);
        Color c = currentPhase == 1 ? phase1Color : (currentPhase == 2 ? phase2Color : phase3Color);
        proj.SetColor(c);
        return proj;
    }

    Vector2 RotateVector(Vector2 v, float angleRad)
    {
        float cos = Mathf.Cos(angleRad);
        float sin = Mathf.Sin(angleRad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    void OnValidate()
    {
        if (autoCombat == null)
            autoCombat = GetComponent<AutoCombat>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (firePoint == null)
            firePoint = transform;
    }
}
