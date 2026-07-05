using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "Combat/Enemy")]
public class EnemyData : ScriptableObject
{
    public string enemyId;
    public string enemyName;
    [TextArea(2, 4)]
    public string description;
    public int level = 1;
    public int maxHealth = 50;
    public int damage = 10;
    public int defense = 2;
    public int experienceReward = 10;
    public int goldReward = 5;
    public ItemData uniqueDrop;
    public float dropChance = 0.25f;
}
