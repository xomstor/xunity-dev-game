using UnityEngine;

[System.Serializable]
public class DropItem
{
    public ItemData itemData;
    public int quantity = 1;
    [Range(0f, 1f)] public float baseDropChance = 0.15f;
    [Tooltip("If true, this item will only drop once per save (stored in PlayerPrefs)")]
    public bool dropOnlyOnce;
}

public class EnemyReward : MonoBehaviour
{
    [Header("Base Rewards")]
    public int experienceReward = 10;
    public int goldReward = 5;

    [Header("Scaling (multiplied by enemy level if set)")]
    public bool useLevelScaling = false;
    public int enemyLevel = 1;
    public float expPerLevel = 5f;
    public float goldPerLevel = 3f;

    [Header("Drops")]
    public DropItem[] drops;

    public int Experience
    {
        get
        {
            float baseExp = useLevelScaling
                ? experienceReward + expPerLevel * enemyLevel
                : experienceReward;
            float multiplier = WorldLevelManager.Instance != null
                ? WorldLevelManager.Instance.CurrentRewardMultiplier
                : 1f;
            return Mathf.RoundToInt(baseExp * multiplier);
        }
    }

    public int Gold
    {
        get
        {
            float baseGold = useLevelScaling
                ? goldReward + goldPerLevel * enemyLevel
                : goldReward;
            float multiplier = WorldLevelManager.Instance != null
                ? WorldLevelManager.Instance.CurrentRewardMultiplier
                : 1f;
            return Mathf.RoundToInt(baseGold * multiplier);
        }
    }

    public DropItem[] Drops => drops;
}
