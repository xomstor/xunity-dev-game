using UnityEngine;

public class AutoCombat : MonoBehaviour
{
    [Header("Auto Attack")]
    public float attackRadius = 3f;
    public float attackCooldown = 1f;
    public int attackDamage = 10;
    public LayerMask enemyLayer;

    private float attackTimer;

    private void Update()
    {
        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            TryAutoAttack();
        }
    }

    private void TryAutoAttack()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, attackRadius, enemyLayer);
        if (hit != null)
        {
            PerformAttack(hit.GetComponent<EnemyBase>());
        }
    }

    public void PerformAttack()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, attackRadius, enemyLayer);
        if (hit != null)
        {
            PerformAttack(hit.GetComponent<EnemyBase>());
        }
    }

    private void PerformAttack(EnemyBase enemy)
    {
        if (enemy == null) return;
        attackTimer = attackCooldown;
        enemy.TakeDamage(attackDamage);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}
