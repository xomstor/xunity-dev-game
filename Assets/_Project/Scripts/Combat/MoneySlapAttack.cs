using UnityEngine;

public class MoneySlapAttack : MonoBehaviour
{
    [Header("Special Attack: Удар купюрами в лицо")]
    [Tooltip("Максимальная доля текущих денег, которую можно потратить")]
    [Range(0.01f, 1f)] public float maxGoldPercent = 0.5f;
    [Tooltip("Максимальный урон от максимального HP врага")]
    [Range(0.1f, 1f)] public float maxHpDamagePercent = 0.5f;
    [Tooltip("Множитель удачи")]
    public float luckMultiplier = 1f;

    PlayerStats playerStats;
    AutoCombat playerCombat;

    void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        if (playerStats == null)
            playerStats = FindAnyObjectByType<PlayerStats>();
        playerCombat = GetComponent<AutoCombat>();
        if (playerCombat == null)
            playerCombat = FindAnyObjectByType<AutoCombat>();
    }

    public bool CanCast()
    {
        return playerStats != null && playerStats.gold > 0;
    }

    public bool Cast(AutoCombat target)
    {
        if (!CanCast()) return false;
        if (target == null || target.IsDead || target.team != CombatTeam.Enemy) return false;

        float percent = Random.Range(0.01f, maxGoldPercent);
        int spent = Mathf.RoundToInt(playerStats.gold * percent);
        if (spent <= 0) return false;

        playerStats.gold -= spent;

        int enemyMaxHp = Mathf.Max(1, target.maxHealth);
        float rawDamage = spent * playerStats.lck * (maxHpDamagePercent * enemyMaxHp) * (1f + luckMultiplier * 0.01f * playerStats.lck);
        int damage = Mathf.RoundToInt(rawDamage);
        damage = Mathf.Min(damage, Mathf.RoundToInt(enemyMaxHp * maxHpDamagePercent));
        damage = Mathf.Max(1, damage);

        target.TakeDamage(damage, 0);

        Debug.Log($"[MoneySlap] Потрачено {spent} gold, урон {damage} (cap {maxHpDamagePercent * 100}% HP).");
        return true;
    }

    public bool CastOnNearestEnemy()
    {
        if (playerCombat == null) return false;
        AutoCombat target = FindNearestEnemy();
        return Cast(target);
    }

    AutoCombat FindNearestEnemy()
    {
        if (playerCombat == null) return null;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, playerCombat.attackRange * 2f);
        AutoCombat nearest = null;
        float nearestDist = float.MaxValue;
        foreach (Collider2D c in colliders)
        {
            AutoCombat other = c.GetComponent<AutoCombat>();
            if (other == null) other = c.GetComponentInParent<AutoCombat>();
            if (other == null) other = c.GetComponentInChildren<AutoCombat>();
            if (other == null || other == playerCombat || other.IsDead || other.team != CombatTeam.Enemy) continue;
            float dist = Vector2.Distance(transform.position, other.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = other;
            }
        }
        return nearest;
    }
}
