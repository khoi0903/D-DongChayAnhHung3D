using UnityEngine;

public class EnemyHealth3D : MonoBehaviour
{
    public int maxHP = 50;
    public int currentHP;
    public bool destroyOnDeath = true;
    public float deathDelay = 0.05f;

    public bool IsDead { get; private set; }

    private void Start()
    {
        if (currentHP <= 0)
            currentHP = maxHP;
    }

    public void TakeDamage(int damage)
    {
        if (IsDead)
            return;

        currentHP -= damage;
        currentHP = Mathf.Max(0, currentHP);

        Debug.Log(gameObject.name + " HP: " + currentHP);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (IsDead)
            return;

        IsDead = true;
        Debug.Log(gameObject.name + " bị tiêu diệt.");

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider enemyCollider in colliders)
            enemyCollider.enabled = false;

        EnemyChase3D chase = GetComponent<EnemyChase3D>();
        if (chase != null)
            chase.enabled = false;

        if (destroyOnDeath)
            Destroy(gameObject, deathDelay);
    }
}
