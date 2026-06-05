using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth3D : MonoBehaviour
{
    public int maxHP = 100;
    public int currentHP;

    public bool isDead = false;
    public GameObject gameOverUI;

    private PlayerController3D playerController;
    private PlayerCombat3D playerCombat;

    private void Awake()
    {
        currentHP = maxHP;

        playerController = GetComponent<PlayerController3D>();
        playerCombat = GetComponent<PlayerCombat3D>();

        if (gameOverUI != null)
            gameOverUI.SetActive(false);
    }

    private void Update()
    {
        if (isDead && Input.GetKeyDown(KeyCode.R))
        {
            RestartScene();
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        currentHP -= damage;

        if (currentHP < 0)
            currentHP = 0;

        Debug.Log("Player HP: " + currentHP);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        Debug.Log("Player đã bị hạ. Toàn bộ quái dừng lại. Bấm R để chơi lại.");

        // Tắt điều khiển player
        if (playerController != null)
            playerController.enabled = false;

        if (playerCombat != null)
            playerCombat.enabled = false;

        // Tắt toàn bộ spawner
        EnemySpawner3D[] spawners = FindObjectsByType<EnemySpawner3D>(FindObjectsInactive.Exclude);
        foreach (EnemySpawner3D spawner in spawners)
        {
            spawner.enabled = false;
        }

        // Tắt AI của toàn bộ quái đang tồn tại
        EnemyChase3D[] enemies = FindObjectsByType<EnemyChase3D>(FindObjectsInactive.Exclude);
        foreach (EnemyChase3D enemy in enemies)
        {
            enemy.enabled = false;
        }

        // Hiện Game Over
        if (gameOverUI != null)
            gameOverUI.SetActive(true);
    }

    private void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
