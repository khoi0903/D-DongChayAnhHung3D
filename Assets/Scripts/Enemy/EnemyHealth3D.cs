using UnityEngine;

public class EnemyHealth3D : MonoBehaviour
{
    public int maxHP = 50;
    public int currentHP;

    private void Start()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;

        Debug.Log(gameObject.name + " HP: " + currentHP);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log(gameObject.name + " bị tiêu diệt.");
        Destroy(gameObject);
    }
}