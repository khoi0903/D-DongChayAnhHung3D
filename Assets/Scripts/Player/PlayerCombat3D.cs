using UnityEngine;

public class PlayerCombat3D : MonoBehaviour
{
    public int damage = 20;
    public float attackRange = 4f;
    public float attackCooldown = 0.5f;

    private float lastAttackTime;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Attack();
        }
    }

    private void Attack()
    {
        if (Time.time < lastAttackTime + attackCooldown)
            return;

        lastAttackTime = Time.time;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        int hitCount = 0;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);

            if (distance <= attackRange)
            {
                EnemyHealth3D enemyHealth = enemy.GetComponent<EnemyHealth3D>();

                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(damage);
                    hitCount++;
                }
                else
                {
                    Debug.LogWarning(enemy.name + " chưa có EnemyHealth3D.");
                }
            }
        }

        Debug.Log("Player click chuột trái tấn công. Số quái trúng: " + hitCount);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}