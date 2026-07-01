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

    [Header("Progress")]
    public int level = 1;
    public int experience = 0;
    public int gold = 0;
    public int experienceToNextLevel = 100;
    public int skillPoints = 0;

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
        int finalDamage = Mathf.Max(1, amount - def);
        hp = Mathf.Max(0, hp - finalDamage);
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
        return Mathf.Min(baseCritChance + lck * 0.02f, 0.5f);
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
            experienceToNextLevel = Mathf.RoundToInt(experienceToNextLevel * 1.2f);
        }
    }

    public int GetDamage()
    {
        return GetDamage(out _);
    }
}
