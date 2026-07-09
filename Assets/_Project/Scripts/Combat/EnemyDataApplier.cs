using UnityEngine;

[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(EnemyReward))]
public class EnemyDataApplier : MonoBehaviour
{
    [Tooltip("Ссылка на данные врага из ScriptableObject")]
    public EnemyData enemyData;

    [Tooltip("Если true, значения перезаписывают настройки EnemyReward при старте")]
    public bool applyOnStart = true;

    private EnemyAudio enemyAudio;

    void Awake()
    {
        // Создаем EnemyAudio заранее, чтобы AutoCombat смог найти его в своем Awake
        if (enemyData != null && HasAnyAudio(enemyData) && GetComponent<EnemyAudio>() == null)
        {
            gameObject.AddComponent<EnemyAudio>();
        }
    }

    private void Start()
    {
        if (applyOnStart)
        {
            Apply();
        }
    }

    public void Apply()
    {
        if (enemyData == null)
        {
            Debug.LogWarning($"EnemyDataApplier на {gameObject.name}: enemyData не назначен.");
            return;
        }

        EnemyReward reward = GetComponent<EnemyReward>();
        if (reward == null)
        {
            Debug.LogError($"EnemyDataApplier на {gameObject.name}: не найден EnemyReward.");
            return;
        }

        reward.experienceReward = enemyData.experienceReward;
        reward.goldReward = enemyData.goldReward;
        reward.enemyLevel = enemyData.level;

        if (enemyData.uniqueDrop != null)
        {
            reward.drops = new DropItem[]
            {
                new DropItem
                {
                    itemData = enemyData.uniqueDrop,
                    quantity = 1,
                    baseDropChance = enemyData.dropChance,
                    dropOnlyOnce = false
                }
            };
        }

        AutoCombat combat = GetComponent<AutoCombat>();
        if (combat != null && combat.team == CombatTeam.Enemy)
        {
            combat.maxHealth = enemyData.maxHealth;
            combat.damage = enemyData.damage;
            combat.def = enemyData.defense;

            if (WorldLevelManager.Instance != null)
            {
                combat.maxHealth = Mathf.RoundToInt(combat.maxHealth * WorldLevelManager.Instance.CurrentEnemyHealthMultiplier);
                combat.damage = Mathf.RoundToInt(combat.damage * WorldLevelManager.Instance.CurrentEnemyDamageMultiplier);
            }

            combat.ResetHealth();
        }

        // Применяем звуки
        if (HasAnyAudio(enemyData))
        {
            enemyAudio = GetComponent<EnemyAudio>();
            if (enemyAudio == null)
            {
                enemyAudio = gameObject.AddComponent<EnemyAudio>();
            }
            enemyAudio.attackClip = enemyData.attackSound;
            enemyAudio.deathClip = enemyData.deathSound;
            enemyAudio.extraClips = enemyData.extraSounds;
        }

        // Применяем элементальные параметры
        combat = GetComponent<AutoCombat>();
        if (combat != null)
        {
            combat.element = enemyData.element;
            combat.elementalResistances = enemyData.elementalResistances != null && enemyData.elementalResistances.Length > 0
                ? enemyData.elementalResistances
                : ElementalSystem.CreateEmptyResistances();
        }

        Debug.Log($"Применены данные врага: {enemyData.enemyName} к {gameObject.name}");
    }

    static bool HasAnyAudio(EnemyData data)
    {
        if (data == null) return false;
        if (data.attackSound != null || data.deathSound != null) return true;
        if (data.extraSounds != null && data.extraSounds.Length > 0) return true;
        return false;
    }
}
