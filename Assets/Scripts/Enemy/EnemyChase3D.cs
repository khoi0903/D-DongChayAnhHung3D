using UnityEngine;

public class EnemyChase3D : MonoBehaviour
{
    public Transform target;

    public float moveSpeed = 3f;
    public float chaseRange = 20f;
    public float attackRange = 1.5f;
    public int damage = 10;
    public float attackCooldown = 1f;

    private float lastAttackTime;

    private void Update()
    {
        if (target == null)
            return;

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= chaseRange && distance > attackRange)
        {
            ChaseTarget();
        }

        if (distance <= attackRange)
        {
            AttackTarget();
        }
    }

    private void ChaseTarget()
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        direction.Normalize();

        transform.position += direction * moveSpeed * Time.deltaTime;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void AttackTarget()
    {
        if (Time.time < lastAttackTime + attackCooldown)
            return;

        lastAttackTime = Time.time;

        PlayerHealth3D playerHealth = target.GetComponent<PlayerHealth3D>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }

        Debug.Log("Hắc Tinh tấn công Player.");
    }
}