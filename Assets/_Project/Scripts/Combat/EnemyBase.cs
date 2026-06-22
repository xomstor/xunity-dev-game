using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    public int maxHP = 50;
    public int currentHP;
    public int expReward = 20;
    public int goldReward = 10;

    private void Awake()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        currentHP = Mathf.Max(currentHP, 0);
        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        GameManager.Instance?.AddExp(expReward);
        GameManager.Instance?.AddGold(goldReward);
        Destroy(gameObject);
    }
}
