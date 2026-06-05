using UnityEngine;

public class EnemySpawner3D : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform player;
    public Transform[] spawnPoints;

    public int maxEnemies = 3;
    public float spawnInterval = 4f;
    public int initialSpawnCount = 1;

    private float lastSpawnTime;
    private int currentEnemyCount;

    private void Start()
    {
        SpawnInitialEnemies();
        lastSpawnTime = Time.time;
    }

    private void Update()
    {
        if (enemyPrefab == null || player == null || spawnPoints == null || spawnPoints.Length == 0)
            return;

        if (currentEnemyCount >= maxEnemies)
            return;

        if (Time.time >= lastSpawnTime + spawnInterval)
        {
            SpawnEnemy();
            lastSpawnTime = Time.time;
        }
    }

    private void SpawnInitialEnemies()
    {
        for (int i = 0; i < initialSpawnCount; i++)
        {
            if (currentEnemyCount < maxEnemies)
            {
                SpawnEnemy();
            }
        }
    }

    private void SpawnEnemy()
    {
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        GameObject enemy = Instantiate(
            enemyPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        EnemyChase3D enemyChase = enemy.GetComponent<EnemyChase3D>();

        if (enemyChase != null)
        {
            enemyChase.target = player;
        }

        EnemyDeathNotifier deathNotifier = enemy.GetComponent<EnemyDeathNotifier>();

        if (deathNotifier == null)
        {
            deathNotifier = enemy.AddComponent<EnemyDeathNotifier>();
        }

        deathNotifier.spawner = this;

        currentEnemyCount++;

        Debug.Log("Spawn Hắc Tinh. Số quái hiện tại: " + currentEnemyCount);
    }

    public void NotifyEnemyDied()
    {
        currentEnemyCount--;

        if (currentEnemyCount < 0)
            currentEnemyCount = 0;

        Debug.Log("Một Hắc Tinh đã bị tiêu diệt. Còn lại: " + currentEnemyCount);
    }
}