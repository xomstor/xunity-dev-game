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

    [Header("Audio")]
    public AudioClip attackSound;
    public AudioClip deathSound;
    [Tooltip("Дополнительные звуки (например, hit, aggro, idle). Используются EnemyAudio.")]
    public AudioClip[] extraSounds;

    [Header("Elemental")]
    public ElementalType element = ElementalType.Physical;
    [Tooltip("Индекс = ElementalType. 0.5 = 50% резист, 1 = иммунитет, -0.5 = уязвимость")]
    public float[] elementalResistances;
}
