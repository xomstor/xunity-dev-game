using UnityEngine;

[System.Serializable]
public class DropItem
{
    public ItemData itemData;
    public int quantity = 1;
    [Range(0f, 1f)] public float baseDropChance = 0.2f;
}

public class EnemyReward : MonoBehaviour
{
    [Header("Rewards")]
    public int experienceReward = 10;
    public int goldReward = 5;

    [Header("Drops")]
    public DropItem[] drops;

    public int Experience => experienceReward;
    public int Gold => goldReward;
    public DropItem[] Drops => drops;
}
