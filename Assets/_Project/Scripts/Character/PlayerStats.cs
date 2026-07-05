using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Stats")]
    public int hp = 100;
    public int maxHp = 100;
    public int atk = 10;
    public int def = 5;
    public int spd = 10;
    public int lck = 5;
    public int atkSpd = 0;
    public int lethality = 0;

    [Header("Progress")]
    public int level = 1;
    public int experience = 0;
    public int gold = 0;
    public int experienceToNextLevel = 100;
    public int skillPoints = 1;

    [Header("Base Stats")]
    public int baseLck;

    [Header("Crit")]
    public float baseCritChance = 0.05f;
    public float critDamageMultiplier = 3f;

    void Awake()
    {
        baseLck = lck;
    }

    public void TakeDamage(int amount)
    {
        int finalDamage = GetMitigatedDamage(amount, 0);
        hp = Mathf.Max(0, hp - finalDamage);
    }

    public void TakeDamage(int amount, int attackerLethality)
    {
        int finalDamage = GetMitigatedDamage(amount, attackerLethality);
        hp = Mathf.Max(0, hp - finalDamage);
    }

    public int GetMitigatedDamage(int rawDamage, int attackerLethality)
    {
        int pureDamage = Mathf.Min(attackerLethality, rawDamage);
        int mitigatable = rawDamage - pureDamage;
        float multiplier = 100f / (100f + def);
        int mitigated = Mathf.RoundToInt(mitigatable * multiplier);
        return Mathf.Max(1, pureDamage + mitigated);
    }

    public float GetDamageReductionPercent()
    {
        return def / (def + 100f) * 100f;
    }

    public float GetAttackCooldownMultiplier()
    {
        float bonus = atkSpd * 0.02f + spd * 0.002f + Mathf.Floor(lck / 35f) * 0.002f;
        return 1f / (1f + bonus);
    }

    public int GetEffectiveLethality()
    {
        return lethality + Mathf.FloorToInt(lck / 50f);
    }

    public float GetHpRegenPerSecond()
    {
        return Mathf.Max(0f, (lck - baseLck) * 0.1f);
    }

    public int GetDamage(out bool isCrit)
    {
        isCrit = Random.value < GetCritChance();
        int damage = atk;
        if (isCrit)
            damage = Mathf.RoundToInt(damage * critDamageMultiplier);
        return damage;
    }

    public float GetCritChance()
    {
        float crit;
        if (lck <= 135)
            crit = baseCritChance + lck / 3f * 0.01f;
        else if (lck <= 435)
            crit = baseCritChance + 135f / 3f * 0.01f + (lck - 135f) / 10f * 0.01f;
        else
            crit = baseCritChance + 135f / 3f * 0.01f + 300f / 10f * 0.01f + (lck - 435f) / 250f * 0.01f;
        return Mathf.Min(crit, 1f);
    }

    public float GetDropChanceMultiplier()
    {
        return 1f + lck * 0.05f;
    }

    public void AddReward(int exp, int goldAmount)
    {
        experience += exp;
        gold += goldAmount;
        CheckLevelUp();
    }

    void CheckLevelUp()
    {
        while (experience >= experienceToNextLevel)
        {
            experience -= experienceToNextLevel;
            level++;
            skillPoints += 3;
            experienceToNextLevel = GetXpForLevel(level + 1);
        }
    }

    int GetXpForLevel(int targetLevel)
    {
        float result = 100f * Mathf.Pow(targetLevel, 1.8f);
        if (targetLevel >= 50)
            result *= 0.5f / 3f;
        else if (targetLevel >= 20)
            result *= 0.5f;
        if (targetLevel > 100)
            result = 100000f;
        return Mathf.RoundToInt(result);
    }

    public int GetDamage()
    {
        return GetDamage(out _);
    }
}
