using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class S03ArenaDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private BlessingManager blessingManager;
    [SerializeField] private BlessingRuntimeController blessingRuntime;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private S03DefenseObjective defenseObjective;

    [Header("Wave Tuning")]
    [SerializeField] private int firstWaveEnemyCount = 3;
    [SerializeField] private int enemiesAddedPerWave = 1;
    [SerializeField] private int maxEnemiesPerWave = 12;
    [SerializeField] private int maxWaves;
    [SerializeField] private float arenaRadius = 18f;
    [SerializeField] private float timeBeforeFirstWave = 1.2f;
    [SerializeField] private float timeBetweenWaves = 1.15f;
    [SerializeField] private float enemyHealthPerWave = 8f;
    [SerializeField] private int baseEnemyDamage = 12;
    [SerializeField] private float enemyDamagePerWave = 0f;

    [Header("Enemy UI")]
    [SerializeField] private bool showEnemyHealthBars = true;
    [SerializeField] private Vector3 enemyHealthBarOffset = new Vector3(0f, 2.35f, 0f);
    private readonly List<MinionHealth3D> activeEnemies = new List<MinionHealth3D>();
    private int waveIndex;
    private bool running;
    private Coroutine flowRoutine;
    private const string EnemyHealthBarName = "S03_EnemyHealthBar";

    private void Start()
    {
        ResolveReferences();
        if (defenseObjective != null)
        {
            defenseObjective.Configure(statusText, null);
            defenseObjective.ResetObjective();
        }

        flowRoutine = StartCoroutine(ArenaFlow());
    }

    public void Configure(
        Transform playerTransform,
        GameObject enemyPrefab,
        Transform[] enemySpawnPoints,
        BlessingManager manager,
        BlessingRuntimeController runtime,
        TMP_Text waveLabel,
        TMP_Text statusLabel)
    {
        player = playerTransform;
        minionPrefab = enemyPrefab;
        spawnPoints = enemySpawnPoints;
        blessingManager = manager;
        blessingRuntime = runtime;
        waveText = waveLabel;
        statusText = statusLabel;
    }

    public void ConfigureWaveTuning(
        int firstWaveCount,
        int enemiesAddedEachWave,
        int maxEnemies,
        int waveLimit,
        float combatRadius,
        float firstWaveDelay,
        float betweenWaveDelay,
        float healthGrowthPerWave,
        int enemyBaseDamage,
        float damageGrowthPerWave,
        bool enableEnemyHealthBars)
    {
        firstWaveEnemyCount = Mathf.Max(1, firstWaveCount);
        enemiesAddedPerWave = Mathf.Max(0, enemiesAddedEachWave);
        maxEnemiesPerWave = Mathf.Max(firstWaveEnemyCount, maxEnemies);
        maxWaves = Mathf.Max(0, waveLimit);
        arenaRadius = Mathf.Max(4f, combatRadius);
        timeBeforeFirstWave = Mathf.Max(0f, firstWaveDelay);
        timeBetweenWaves = Mathf.Max(0f, betweenWaveDelay);
        enemyHealthPerWave = Mathf.Max(0f, healthGrowthPerWave);
        baseEnemyDamage = Mathf.Max(1, enemyBaseDamage);
        enemyDamagePerWave = Mathf.Max(0f, damageGrowthPerWave);
        showEnemyHealthBars = enableEnemyHealthBars;
    }

    private IEnumerator ArenaFlow()
    {
        running = true;
        SetWaveText("Defend Co Loa");
        SetStatus(FormatStatus("Enemies have breached the lower levels. Hold the last defense line."));
        yield return new WaitForSeconds(timeBeforeFirstWave);

        while (running)
        {
            if (TryFailForPlayerDeath() || IsDefenseFailed())
                yield break;

            if (maxWaves > 0 && waveIndex >= maxWaves)
            {
                CompleteMission();
                yield break;
            }

            waveIndex++;
            yield return StartCoroutine(RunWave(waveIndex));
            if (TryFailForPlayerDeath() || IsDefenseFailed())
                yield break;

            yield return StartCoroutine(OpenBlessingChoice());
            if (TryFailForPlayerDeath() || IsDefenseFailed())
                yield break;

            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    private IEnumerator RunWave(int currentWave)
    {
        activeEnemies.Clear();
        defenseObjective?.ClearTrackedEnemies();
        blessingRuntime?.OnWaveStarted(currentWave);

        float awarenessDelay = blessingRuntime != null ? blessingRuntime.GetAwarenessSpawnDelay() : 0f;
        if (awarenessDelay > 0f)
        {
            SetStatus(FormatStatus("Canh Gioi: Hac Tinh are advancing from the lower levels..."));
            yield return new WaitForSeconds(awarenessDelay);
        }

        int enemyCount = Mathf.Clamp(
            firstWaveEnemyCount + (currentWave - 1) * enemiesAddedPerWave,
            1,
            Mathf.Max(1, maxEnemiesPerWave));

        SetWaveText("Defense Wave " + currentWave);
        SetStatus(FormatStatus("Defense Wave " + currentWave + ": stop " + enemyCount + " Hac Tinh before they reach civilians."));

        for (int i = 0; i < enemyCount; i++)
        {
            MinionHealth3D enemy = SpawnEnemy(currentWave, i);
            if (enemy != null)
            {
                activeEnemies.Add(enemy);
                defenseObjective?.RegisterEnemy(enemy);
            }

            yield return new WaitForSeconds(0.18f);
        }

        while (!IsWaveCleared())
        {
            if (TryFailForPlayerDeath() || IsDefenseFailed())
                yield break;

            defenseObjective?.CheckForBreaches();
            if (IsDefenseFailed())
                yield break;

            SetStatus(FormatStatus("Remaining enemies: " + CountAliveEnemies() + ". Protect the upper civilian levels."));
            yield return new WaitForSeconds(0.35f);
        }

        SetStatus(FormatStatus("Defense Wave " + currentWave + " cleared. Choose 1 of 3 An Linh blessings."));
        yield return new WaitForSeconds(0.55f);
    }

    private IEnumerator OpenBlessingChoice()
    {
        if (blessingManager == null)
            yield break;

        bool choiceComplete = false;
        float oldFixedDeltaTime = Time.fixedDeltaTime;
        Time.timeScale = 0f;

        blessingManager.PresentChoices(() => choiceComplete = true);
        while (!choiceComplete)
            yield return null;

        Time.timeScale = 1f;
        Time.fixedDeltaTime = oldFixedDeltaTime > 0f ? oldFixedDeltaTime : 0.02f;
        SetStatus(FormatStatus("An Linh blessing applied. The next assault is forming."));
    }

    private MinionHealth3D SpawnEnemy(int currentWave, int spawnIndex)
    {
        GameObject enemy = minionPrefab != null
            ? Instantiate(minionPrefab)
            : GameObject.CreatePrimitive(PrimitiveType.Capsule);

        enemy.name = "S03_Wave" + currentWave.ToString("00") + "_Enemy" + (spawnIndex + 1).ToString("00");
        enemy.tag = "Enemy";
        SetLayerRecursively(enemy, LayerMask.NameToLayer("Enemy"));
        enemy.transform.position = GetSpawnPosition(spawnIndex);
        enemy.transform.rotation = Quaternion.LookRotation(GetDirectionToPlayer(enemy.transform.position));

        MinionHealth3D health = enemy.GetComponent<MinionHealth3D>();
        if (health == null)
            health = enemy.AddComponent<MinionHealth3D>();

        health.maxHP = Mathf.RoundToInt(health.maxHP + (currentWave - 1) * enemyHealthPerWave);
        health.currentHP = health.maxHP;
        health.destroyOnDeath = true;
        health.deathDelay = 0.12f;

        MinionChase3D chase = enemy.GetComponent<MinionChase3D>();
        if (chase == null)
            chase = enemy.AddComponent<MinionChase3D>();

        chase.ResetForSpawn(player);
        chase.target = player;
        chase.chaseRange = arenaRadius * 2f + (blessingRuntime != null ? blessingRuntime.GetEnemyAwarenessRangeBonus() : 0f);
        chase.damage = Mathf.Max(1, Mathf.RoundToInt(baseEnemyDamage + (currentWave - 1) * enemyDamagePerWave));
        chase.moveSpeed += Mathf.Min(1.8f, currentWave * 0.08f);

        if (showEnemyHealthBars)
            EnsureEnemyHealthBar(enemy, health);

        return health;
    }

    private Vector3 GetSpawnPosition(int index)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform point = spawnPoints[index % spawnPoints.Length];
            if (point != null)
                return point.position;
        }

        float angle = index * 137.5f * Mathf.Deg2Rad;
        return transform.position + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * arenaRadius;
    }

    private Vector3 GetDirectionToPlayer(Vector3 position)
    {
        if (player == null)
            return Vector3.forward;

        Vector3 direction = player.position - position;
        direction.y = 0f;
        return direction.sqrMagnitude <= 0.001f ? Vector3.forward : direction.normalized;
    }

    private bool IsWaveCleared()
    {
        return CountAliveEnemies() <= 0;
    }

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

            if (!enemy.IsDead)
                count++;
        }

        return count;
    }

    private bool IsPlayerDead()
    {
        if (player == null)
            return false;

        PlayerHealth3D health = player.GetComponent<PlayerHealth3D>();
        return health != null && health.isDead;
    }

    private bool TryFailForPlayerDeath()
    {
        if (!IsPlayerDead())
            return false;

        running = false;
        defenseObjective?.MarkFailed("The defender has fallen. Co Loa cannot hold.");
        return true;
    }

    private bool IsDefenseFailed()
    {
        if (defenseObjective == null || !defenseObjective.HasFailed)
            return false;

        running = false;
        return true;
    }

    private void CompleteMission()
    {
        running = false;
        SetWaveText("Defense Complete");
        if (defenseObjective != null)
            defenseObjective.MarkSucceeded("Co Loa holds. The upper levels are safe.");
        else
            SetStatus("Co Loa holds. The upper levels are safe.");
    }

    private void ResolveReferences()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (blessingRuntime == null && player != null)
            blessingRuntime = player.GetComponent<BlessingRuntimeController>();

        if (blessingManager == null)
            blessingManager = FindAnyObjectByType<BlessingManager>();

        if (defenseObjective == null)
            defenseObjective = FindAnyObjectByType<S03DefenseObjective>();
    }

    private void SetWaveText(string message)
    {
        if (waveText != null)
            waveText.text = message;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private string FormatStatus(string message)
    {
        return defenseObjective != null ? defenseObjective.FormatStatus(message) : message;
    }

    private void EnsureEnemyHealthBar(GameObject enemy, MinionHealth3D health)
    {
        if (enemy == null || health == null)
            return;

        Transform existingBar = enemy.transform.Find(EnemyHealthBarName);
        if (existingBar != null)
        {
            MinionHealthBarUI existingUI = existingBar.GetComponent<MinionHealthBarUI>();
            if (existingUI != null)
            {
                existingUI.minionHealth = health;
                existingUI.cameraTransform = Camera.main != null ? Camera.main.transform : null;
            }

            return;
        }

        GameObject bar = new GameObject(EnemyHealthBarName);
        bar.transform.SetParent(enemy.transform, false);
        bar.transform.localPosition = enemyHealthBarOffset;
        bar.transform.localRotation = Quaternion.identity;
        bar.transform.localScale = Vector3.one * 0.012f;

        RectTransform barRect = bar.AddComponent<RectTransform>();
        barRect.sizeDelta = new Vector2(140f, 18f);

        Canvas canvas = bar.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 40;

        CanvasScaler scaler = bar.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 28f;

        Slider slider = bar.AddComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = health.maxHP;
        slider.value = health.currentHP;

        Image background = CreateHealthBarImage(bar.transform, "Background", new Color(0.08f, 0.01f, 0.025f, 0.92f));
        Stretch(background.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(bar.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        Stretch(fillAreaRect, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));

        Image fill = CreateHealthBarImage(fillArea.transform, "Fill", new Color(0.86f, 0.08f, 0.1f, 1f));
        Stretch(fill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        slider.fillRect = fill.rectTransform;
        slider.targetGraphic = fill;

        MinionHealthBarUI healthBar = bar.AddComponent<MinionHealthBarUI>();
        healthBar.minionHealth = health;
        healthBar.healthSlider = slider;
        healthBar.cameraTransform = Camera.main != null ? Camera.main.transform : null;
    }

    private static Image CreateHealthBarImage(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        Image image = obj.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null || layer < 0)
            return;

        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
