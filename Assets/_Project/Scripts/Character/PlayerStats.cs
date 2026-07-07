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

    [Header("Defense Curve")]
    public float defCurvePower = 0.774f;
    public float defCapPercent = 0.99f;
    public int defCapValue = 34221;

    [Header("Crit")]
    public float baseCritChance = 0.05f;
    public float critDamageMultiplier = 3f;

    [Header("XP Curve")]
    [Tooltip("Опыт для 1-го уровня")]
    public int baseXp = 100;
    [Tooltip("Линейный прирост опыта за уровень в лёгкой зоне")]
    public int earlyXpIncrement = 20;
    [Tooltip("Квадратичный прирост опыта в лёгкой зоне")]
    public int earlyXpCurve = 5;
    [Tooltip("До какого уровня действует лёгкая формула")]
    public int easyLevelCap = 30;
    [Tooltip("Степень роста опыта после лёгкой зоны")]
    public float lateXpPower = 2.5f;

    void Awake()
    {
        baseLck = lck;
        experienceToNextLevel = GetXpForLevel(level + 1);
    }

    public void TakeDamage(int amount)
    {
        if (IsInvulnerable()) return;
        int finalDamage = GetMitigatedDamage(amount, 0);
        hp = Mathf.Max(0, hp - finalDamage);
    }

    public void TakeDamage(int amount, int attackerLethality)
    {
        if (IsInvulnerable()) return;
        int finalDamage = GetMitigatedDamage(amount, attackerLethality);
        hp = Mathf.Max(0, hp - finalDamage);
    }

    bool IsInvulnerable()
    {
        PlayerController pc = GetComponentInChildren<PlayerController>();
        if (pc == null) pc = GetComponentInParent<PlayerController>();
        if (pc == null) pc = GetComponent<PlayerController>();
        return pc != null && pc.isInvulnerable;
    }

    public float GetDefenseMultiplier()
    {
        float effectiveDef = Mathf.Clamp(def, 0, defCapValue);
        float multiplier = Mathf.Pow(100f / (100f + effectiveDef), defCurvePower);
        return Mathf.Clamp01(multiplier);
    }

    public int GetMitigatedDamage(int rawDamage, int attackerLethality)
    {
        int pureDamage = Mathf.Min(attackerLethality, rawDamage);
        int mitigatable = rawDamage - pureDamage;
        float multiplier = GetDefenseMultiplier();
        int mitigated = Mathf.RoundToInt(mitigatable * multiplier);
        return Mathf.Max(1, pureDamage + mitigated);
    }

    public float GetDamageReductionPercent()
    {
        return (1f - GetDefenseMultiplier()) * 100f;
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

    public float GetCritDamageMultiplier(int comboHit = 0)
    {
        float mult = critDamageMultiplier;
        if ((comboHit == 2 || comboHit == 3) && lck > 300)
            mult += (lck - 300) * 0.106f;
        return mult;
    }

    public int GetDamage(out bool isCrit, int comboHit = 0)
    {
        isCrit = Random.value < GetCritChance();
        int damage = atk;
        if (isCrit)
            damage = Mathf.RoundToInt(damage * GetCritDamageMultiplier(comboHit));
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

    public void ApplyDeathPenalty()
    {
        int safeGold = 1000 * level + lck;
        int lossable = Mathf.Max(0, gold - safeGold);
        int loss = Mathf.RoundToInt(lossable * 0.05f);
        gold -= loss;
        Debug.Log($"[PlayerStats] Death penalty: lost {loss} gold. Safe amount: {safeGold}. Remaining: {gold}.");
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
        if (targetLevel <= 1) return baseXp;
        if (targetLevel <= easyLevelCap)
        {
            int level = targetLevel - 1;
            return baseXp + earlyXpIncrement * level + earlyXpCurve * level * (level - 1);
        }

        int easyMax = baseXp + earlyXpIncrement * (easyLevelCap - 1) + earlyXpCurve * (easyLevelCap - 1) * (easyLevelCap - 2);
        float scaled = easyMax * Mathf.Pow((float)targetLevel / easyLevelCap, lateXpPower);
        return Mathf.RoundToInt(scaled);
    }

    public int GetDamage()
    {
        return GetDamage(out _, 0);
    }
}
