using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// S03ArenaDirector.cs
/// Bộ điều phối chính của S03 – Đấu Trường Cổ Loa.
/// Quản lý toàn bộ flow wave:
///   1. Đợi thời gian khởi động ngắn.
///   2. Spawn wave Hác Tinh với số lượng và chỉ số tăng dần.
///   3. Chờ player hạ hết quái.
///   4. Mở UI chọn Chúc Phúc Anh Linh (tạm dừng timeScale = 0).
///   5. Lặp lại đến khi hết maxWaves hoặc player chết.
/// Hỗ trợ cả Inspector drag-and-drop và Configure() từ builder.
/// </summary>
public sealed class S03ArenaDirector : MonoBehaviour
{
    // ──────────────────────────────────────────────────────────────────
    //  Inspector Fields
    // ──────────────────────────────────────────────────────────────────
    [Header("References")]
    [Tooltip("Transform của Player (tự resolve nếu để trống).")]
    [SerializeField] private Transform       player;
    [Tooltip("Prefab của Minion (Hác Tinh). Nếu null sẽ dùng primitive capsule.")]
    [SerializeField] private GameObject      minionPrefab;
    [Tooltip("Danh sách điểm spawn địch xung quanh đấu trường.")]
    [SerializeField] private Transform[]     spawnPoints;
    [Tooltip("BlessingManager để mở UI chọn sau mỗi wave.")]
    [SerializeField] private BlessingManager blessingManager;
    [Tooltip("BlessingRuntimeController trên Player để áp dụng Awareness, CoLoa, v.v.")]
    [SerializeField] private BlessingRuntimeController blessingRuntime;
    [Tooltip("Text hiển thị 'Wave X'.")]
    [SerializeField] private TMP_Text        waveText;
    [Tooltip("Text hiển thị trạng thái hiện tại (số địch còn lại, v.v.).")]
    [SerializeField] private TMP_Text        statusText;

    [Header("Wave Tuning")]
    [Tooltip("Số địch ở wave đầu tiên.")]
    [SerializeField] private int   firstWaveEnemyCount = 3;
    [Tooltip("Số địch thêm vào mỗi wave tiếp theo.")]
    [SerializeField] private int   enemiesAddedPerWave = 1;
    [Tooltip("Số địch tối đa trong 1 wave.")]
    [SerializeField] private int   maxEnemiesPerWave   = 12;
    [Tooltip("Số wave tối đa (0 = vô hạn).")]
    [SerializeField] private int   maxWaves;
    [Tooltip("Bán kính đấu trường – dùng khi không có SpawnPoints.")]
    [SerializeField] private float arenaRadius          = 18f;
    [Tooltip("Thời gian chờ trước wave đầu tiên (giây).")]
    [SerializeField] private float timeBeforeFirstWave  = 1.2f;
    [Tooltip("Thời gian chờ giữa các wave (giây).")]
    [SerializeField] private float timeBetweenWaves     = 1.15f;
    [Tooltip("Lượng HP địch tăng thêm mỗi wave.")]
    [SerializeField] private float enemyHealthPerWave   = 8f;
    [Tooltip("Sát thương cơ bản của địch (wave 1).")]
    [SerializeField] private int   baseEnemyDamage      = 12;
    [Tooltip("Lượng sát thương địch tăng mỗi wave.")]
    [SerializeField] private float enemyDamagePerWave   = 0f;

    // ──────────────────────────────────────────────────────────────────
    //  Internal State
    // ──────────────────────────────────────────────────────────────────
    private readonly List<MinionHealth3D> activeEnemies = new List<MinionHealth3D>();
    private int      waveIndex;
    private bool     running;
    private Coroutine flowRoutine;

    // ──────────────────────────────────────────────────────────────────
    //  Lifecycle
    // ──────────────────────────────────────────────────────────────────
    private void Start()
    {
        ResolveReferences();
        flowRoutine = StartCoroutine(ArenaFlow());
    }

    // ──────────────────────────────────────────────────────────────────
    //  Public Config (gọi từ S03CoLoaArenaBuilder)
    // ──────────────────────────────────────────────────────────────────
    public void Configure(
        Transform                  playerTransform,
        GameObject                 enemyPrefab,
        Transform[]                enemySpawnPoints,
        BlessingManager            manager,
        BlessingRuntimeController  runtime,
        TMP_Text                   waveLabel,
        TMP_Text                   statusLabel)
    {
        player          = playerTransform;
        minionPrefab    = enemyPrefab;
        spawnPoints     = enemySpawnPoints;
        blessingManager = manager;
        blessingRuntime = runtime;
        waveText        = waveLabel;
        statusText      = statusLabel;
    }

    // ──────────────────────────────────────────────────────────────────
    //  Main Flow Coroutine
    // ──────────────────────────────────────────────────────────────────
    private IEnumerator ArenaFlow()
    {
        running = true;
        SetStatus("Đấu trường Cổ Loa đã sẵn sàng. Tiêu diệt toàn bộ Hác Tinh.");
        yield return new WaitForSeconds(timeBeforeFirstWave);

        while (running)
        {
            // Kiểm tra player đã chết
            if (IsPlayerDead()) yield break;

            // Kiểm tra đã hết số wave quy định
            if (maxWaves > 0 && waveIndex >= maxWaves)
            {
                SetStatus("Hoàn thành toàn bộ wave S03. Build của bạn đã thành hình.");
                yield break;
            }

            // Chạy wave mới
            waveIndex++;
            yield return StartCoroutine(RunWave(waveIndex));
            if (IsPlayerDead()) yield break;

            // Mở UI chọn Blessing
            yield return StartCoroutine(OpenBlessingChoice());
            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Wave Coroutine
    // ──────────────────────────────────────────────────────────────────
    private IEnumerator RunWave(int currentWave)
    {
        activeEnemies.Clear();

        // Thông báo cho BlessingRuntime biết wave mới bắt đầu (CoLoa shield, KyDau frenzy, v.v.)
        blessingRuntime?.OnWaveStarted(currentWave);

        // Cảnh Giới: báo trước và delay spawn
        float awarenessDelay = blessingRuntime != null ? blessingRuntime.GetAwarenessSpawnDelay() : 0f;
        if (awarenessDelay > 0f)
        {
            SetStatus("Cảnh Giới: đợt Hác Tinh tiếp theo đang áp sát...");
            yield return new WaitForSeconds(awarenessDelay);
        }

        // Tính số địch wave này
        int enemyCount = Mathf.Clamp(
            firstWaveEnemyCount + (currentWave - 1) * enemiesAddedPerWave,
            1,
            Mathf.Max(1, maxEnemiesPerWave));

        SetWaveText("Wave " + currentWave);
        SetStatus("Wave " + currentWave + ": hạ " + enemyCount + " kẻ địch.");

        // Spawn từng địch (cách nhau 0.18s để tránh lag spike)
        for (int i = 0; i < enemyCount; i++)
        {
            MinionHealth3D enemy = SpawnEnemy(currentWave, i);
            if (enemy != null)
                activeEnemies.Add(enemy);

            yield return new WaitForSeconds(0.18f);
        }

        // Chờ đến khi wave được dọn sạch
        while (!IsWaveCleared())
        {
            if (IsPlayerDead()) yield break;
            SetStatus("Còn lại: " + CountAliveEnemies() + " Hác Tinh.");
            yield return new WaitForSeconds(0.35f);
        }

        SetStatus("Wave " + currentWave + " đã sạch. Chuẩn bị nhận Chúc Phúc Anh Linh.");
        yield return new WaitForSeconds(0.55f);
    }

    // ──────────────────────────────────────────────────────────────────
    //  Blessing Choice Coroutine
    // ──────────────────────────────────────────────────────────────────
    private IEnumerator OpenBlessingChoice()
    {
        if (blessingManager == null) yield break;

        bool     choiceComplete    = false;
        float    oldFixedDeltaTime = Time.fixedDeltaTime;

        // Tạm dừng game để player chọn Blessing
        Time.timeScale = 0f;

        blessingManager.PresentChoices(() => choiceComplete = true);

        // Chờ theo unscaled time (timeScale = 0 nên không thể yield WaitForSeconds)
        while (!choiceComplete)
            yield return null;

        // Khôi phục thời gian
        Time.timeScale    = 1f;
        Time.fixedDeltaTime = oldFixedDeltaTime > 0f ? oldFixedDeltaTime : 0.02f;

        SetStatus("Chúc phúc đã áp dụng. Đợt tiếp theo sắp bắt đầu.");
    }

    // ──────────────────────────────────────────────────────────────────
    //  Enemy Spawning
    // ──────────────────────────────────────────────────────────────────
    private MinionHealth3D SpawnEnemy(int currentWave, int spawnIndex)
    {
        // Tạo instance từ prefab hoặc primitive
        GameObject enemy = minionPrefab != null
            ? Instantiate(minionPrefab)
            : GameObject.CreatePrimitive(PrimitiveType.Capsule);

        enemy.name = "S03_Wave" + currentWave.ToString("00") + "_Enemy" + (spawnIndex + 1).ToString("00");
        enemy.tag  = "Enemy";
        SetLayerRecursively(enemy, LayerMask.NameToLayer("Enemy"));

        enemy.transform.position = GetSpawnPosition(spawnIndex);
        enemy.transform.rotation = Quaternion.LookRotation(GetDirectionToPlayer(enemy.transform.position));

        // ── Cấu hình MinionHealth3D ──────────────────────────────────
        MinionHealth3D health = enemy.GetComponent<MinionHealth3D>()
                             ?? enemy.AddComponent<MinionHealth3D>();

        health.maxHP          = Mathf.RoundToInt(health.maxHP + (currentWave - 1) * enemyHealthPerWave);
        health.currentHP      = health.maxHP;
        health.destroyOnDeath = true;
        health.deathDelay     = 0.12f;

        // ── Cấu hình MinionChase3D ───────────────────────────────────
        MinionChase3D chase = enemy.GetComponent<MinionChase3D>()
                           ?? enemy.AddComponent<MinionChase3D>();

        chase.ResetForSpawn(player);
        chase.target     = player;
        chase.chaseRange = arenaRadius * 2f + (blessingRuntime != null ? blessingRuntime.GetEnemyAwarenessRangeBonus() : 0f);
        chase.damage     = Mathf.Max(1, Mathf.RoundToInt(baseEnemyDamage + (currentWave - 1) * enemyDamagePerWave));

        // Tăng tốc độ theo wave, nhưng cap để tránh quá mạnh
        chase.moveSpeed += Mathf.Min(1.8f, currentWave * 0.08f);

        return health;
    }

    // ──────────────────────────────────────────────────────────────────
    //  Spawn Position Helpers
    // ──────────────────────────────────────────────────────────────────
    private Vector3 GetSpawnPosition(int index)
    {
        // Ưu tiên dùng SpawnPoints được đặt trong scene
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform point = spawnPoints[index % spawnPoints.Length];
            if (point != null) return point.position;
        }

        // Fallback: phân bổ vòng tròn theo golden angle (137.5°) để spawn trải đều
        float angle = index * 137.5f * Mathf.Deg2Rad;
        return transform.position + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * arenaRadius;
    }

    private Vector3 GetDirectionToPlayer(Vector3 position)
    {
        if (player == null) return Vector3.forward;
        Vector3 dir = player.position - position;
        dir.y = 0f;
        return dir.sqrMagnitude <= 0.001f ? Vector3.forward : dir.normalized;
    }

    // ──────────────────────────────────────────────────────────────────
    //  Wave Clear Checks
    // ──────────────────────────────────────────────────────────────────
    private bool IsWaveCleared() => CountAliveEnemies() <= 0;

    private int CountAliveEnemies()
    {
        int count = 0;
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            MinionHealth3D enemy = activeEnemies[i];
            if (enemy == null)
            {
                activeEnemies.RemoveAt(i);
                continue;
            }
            if (!enemy.IsDead) count++;
        }
        return count;
    }

    private bool IsPlayerDead()
    {
        if (player == null) return false;
        PlayerHealth3D health = player.GetComponent<PlayerHealth3D>();
        return health != null && health.isDead;
    }

    // ──────────────────────────────────────────────────────────────────
    //  Reference Resolution
    // ──────────────────────────────────────────────────────────────────
    private void ResolveReferences()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        if (blessingRuntime == null && player != null)
            blessingRuntime = player.GetComponent<BlessingRuntimeController>();

        if (blessingManager == null)
            blessingManager = FindAnyObjectByType<BlessingManager>();
    }

    // ──────────────────────────────────────────────────────────────────
    //  UI Helpers
    // ──────────────────────────────────────────────────────────────────
    private void SetWaveText(string message)
    {
        if (waveText != null) waveText.text = message;
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    // ──────────────────────────────────────────────────────────────────
    //  Utility
    // ──────────────────────────────────────────────────────────────────
    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null || layer < 0) return;
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    // ──────────────────────────────────────────────────────────────────
    //  Gizmos (Editor debug)
    // ──────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        // Vẽ bán kính đấu trường
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, arenaRadius);

        // Vẽ các spawn point
        if (spawnPoints == null) return;
        Gizmos.color = Color.red;
        foreach (Transform sp in spawnPoints)
        {
            if (sp != null) Gizmos.DrawWireSphere(sp.position, 0.8f);
        }
    }
}
