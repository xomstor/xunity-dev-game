using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "NewSkill", menuName = "Skills/Skill Data", order = 0)]
public class SkillData : ScriptableObject
{
    [Header("Info")]
    public string skillName = "Fireball";
    [TextArea(3, 10)]
    public string description = "Launches a blazing projectile that damages enemies on impact.";
    public Sprite icon;

    [Header("Projectile")]
    [Tooltip("Prefab with PlayerProjectile component")]
    public PlayerProjectile projectilePrefab;
    public Vector2 fireOffset = new Vector2(0.5f, 0.2f);
    public float projectileSpeed = 12f;
    public float projectileLifetime = 3f;
    public ElementalType projectileElement = ElementalType.Fire;

    [Header("Stats")]
    public int baseDamage = 20;
    public int damagePerLevel = 5;
    public bool usePlayerStats = true;
    public bool useFacingDirection = true;

    [Header("Progression")]
    [Tooltip("Initial cooldown in seconds at level 1")]
    public float baseCooldown = 20f;
    [Tooltip("Minimum cooldown in seconds at high levels")]
    public float minCooldown = 0.1f;
    public int maxLevel = 100;
    public int killsPerTalentPoint = 10;

    [Header("Audio / Effects")]
    public AudioClip castSound;
    public GameObject muzzleEffectPrefab;
    public GameObject projectileEffectPrefab;

    [Header("Input")]
    public Key throwKey = Key.L;

    public int GetUpgradeCost(int currentLevel) => Mathf.Max(1, Mathf.CeilToInt(currentLevel / 10f));
    public float GetCooldown(int level) => Mathf.Max(minCooldown, baseCooldown / level);

    public int GetDamage(int level, int playerDamage)
    {
        int levelBonus = (level - 1) * damagePerLevel;
        if (usePlayerStats)
            return playerDamage + levelBonus;
        return baseDamage + levelBonus;
    }
}
