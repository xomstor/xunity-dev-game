using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Stats")]
    public int maxHP = 100;
    public int currentHP;
    public float moveSpeed = 3f;

    [Header("Combat")]
    public int attackDamage = 10;
    public float attackCooldown = 1f;

    private float attackTimer;
    private AutoCombat autoCombat;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        autoCombat = GetComponent<AutoCombat>();
        currentHP = maxHP;
    }

    private void Update()
    {
        attackTimer -= Time.deltaTime;
    }

    public void ManualAttack()
    {
        if (attackTimer > 0f) return;
        attackTimer = attackCooldown;
        autoCombat?.PerformAttack();
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
        Debug.Log("Player died");
        SceneLoader.Instance?.ReloadCurrent();
    }
}
