using UnityEngine;

[RequireComponent(typeof(EnemyReward))]
public class EnemyDataApplier : MonoBehaviour
{
    [Tooltip("Ссылка на данные врага из ScriptableObject")]
    public EnemyData enemyData;

    [Tooltip("Если true, значения перезаписывают настройки EnemyReward при старте")]
    public bool applyOnStart = true;

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

            if (WorldLevelManager.Instance != null)
            {
                combat.maxHealth = Mathf.RoundToInt(combat.maxHealth * WorldLevelManager.Instance.CurrentEnemyHealthMultiplier);
                combat.damage = Mathf.RoundToInt(combat.damage * WorldLevelManager.Instance.CurrentEnemyDamageMultiplier);
            }

            combat.ResetHealth();
        }

        Debug.Log($"Применены данные врага: {enemyData.enemyName} к {gameObject.name}");
    }
}
