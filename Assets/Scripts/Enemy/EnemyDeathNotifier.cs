using UnityEngine;

public class EnemyDeathNotifier : MonoBehaviour
{
    public EnemySpawner3D spawner;

    private void OnDestroy()
    {
        if (spawner != null)
        {
            spawner.NotifyEnemyDied();
        }
    }
}