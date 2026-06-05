using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class S02CaveEventController : MonoBehaviour
{
    public Transform player;
    public PlayerCombat3D playerCombat;
    public S01WarningTextUI warningUI;
    public TMP_Text interactionText;
    public TMP_Text progressText;
    public GameObject blackStarEnemyPrefab;
    public Transform[] enemySpawnPoints;
    public Transform timeRift;
    public S02CutsceneController cutsceneController;

    public float stabilizeDuration = 30f;
    public float enemySpawnInterval = 6f;
    public int maxActiveEnemies = 4;
    public string nextSceneName = "S03_CoLoaArrival";

    private bool ancientSignsTriggered;
    private bool voicesTriggered;
    private bool descentTriggered;
    private bool playerNearTimeRift;
    private bool resonanceUnlocked;
    private bool stabilizationRunning;
    private bool overloadWarningShown;
    private bool friendsWarningShown;
    private bool warnedMissingEnemyPrefab;
    private bool warnedMissingInteractionText;
    private float stabilizationStartTime;
    private float nextEnemySpawnTime;

    private void Start()
    {
        FindReferencesIfNeeded();
        EnsureTimeRiftChamberWalkableSurface();
        DisableBlockingTimeRiftVisualColliders();
        SetPlayerCombat(false);
        HideInteractionText();
        HideProgressText();
        StartCoroutine(SceneStartSequence());
    }

    private void Update()
    {
        if (playerNearTimeRift && !resonanceUnlocked && Input.GetKeyDown(KeyCode.E))
            ActivateResonance();

        if (stabilizationRunning)
            UpdateStabilization();
    }

    public void TriggerAncientSigns()
    {
        if (ancientSignsTriggered)
            return;

        ancientSignsTriggered = true;
        StartCoroutine(ShowStorySequence(
            "Những hoa văn này... không giống thứ gì trong bảo tàng.",
            "Theo dấu ký hiệu phát sáng trên vách đá."));
    }

    public void TriggerVoices()
    {
        if (voicesTriggered)
            return;

        voicesTriggered = true;
        StartCoroutine(ShowStorySequence(
            "Minh: An! Cậu nghe thấy không?",
            "Giọng nói vọng ra từ sâu trong hang."));
    }

    public void TriggerBlackStarDescent()
    {
        if (descentTriggered)
            return;

        descentTriggered = true;
        StartCoroutine(BlackStarDescentSequence());
    }

    public void SetPlayerNearTimeRift(bool near)
    {
        playerNearTimeRift = near;

        if (resonanceUnlocked)
        {
            HideInteractionText();
            return;
        }

        if (near)
        {
            ShowStory("Khe nứt thời gian cộng hưởng khi Văn An đến gần.", 3.5f);
            ShowInteractionText("Nhấn E để cộng hưởng với khe nứt thời gian");
        }
        else
        {
            HideInteractionText();
        }
    }

    private IEnumerator SceneStartSequence()
    {
        if (cutsceneController != null)
        {
            yield return cutsceneController.PlayIntro();
            ShowStory("Ánh sáng xanh yếu ớt dẫn sâu vào lòng đất.", 4.5f);
            yield break;
        }

        yield return new WaitForSeconds(0.35f);
        ShowStory("Văn An: Mình... còn sống sao?", 4.5f);
        yield return new WaitForSeconds(4.7f);
        ShowWarning("Không thể tấn công. Tìm lối ra.", 4.5f);
        yield return new WaitForSeconds(4.7f);
        ShowStory("Ánh sáng xanh yếu ớt dẫn sâu vào lòng đất.", 4.5f);
    }

    private IEnumerator ShowStorySequence(string first, string second)
    {
        ShowStory(first, 3.7f);
        yield return new WaitForSeconds(3.9f);
        ShowStory(second, 3.7f);
    }

    private IEnumerator ShowDescentSequence()
    {
        ShowStory("Tiếng gầm vang xuống từ hố sụp phía trên.", 3.5f);
        yield return new WaitForSeconds(3.7f);
        ShowWarning("Hắc Tinh đã xuống hang. Chạy tới ánh sáng phía trước!", 4.8f);
    }

    private IEnumerator BlackStarDescentSequence()
    {
        if (cutsceneController != null)
            yield return cutsceneController.PlayBlackStarDescent();
        else
            yield return ShowDescentSequence();

        SpawnEnemyAtIndex(0, false);
    }

    private void ActivateResonance()
    {
        if (resonanceUnlocked)
            return;

        resonanceUnlocked = true;
        HideInteractionText();
        StartCoroutine(ResonanceSequence());
    }

    private IEnumerator ResonanceSequence()
    {
        if (cutsceneController != null)
        {
            yield return cutsceneController.PlayResonanceUnlock();
        }
        else
        {
            ShowStory("TimeRift phản ứng với Văn An.", 4f);
            yield return new WaitForSeconds(4.2f);
            ShowStory("Năng lượng cộng hưởng tạm thời mở khóa phản kích.", 4.2f);
        }

        SetPlayerCombat(true);
        if (cutsceneController == null)
            yield return new WaitForSeconds(4.4f);

        ShowWarning("Bấm chuột trái để đẩy lùi Hắc Tinh. Giữ TimeRift ổn định!", 5.5f);
        StartStabilization();
    }

    private void StartStabilization()
    {
        stabilizationRunning = true;
        stabilizationStartTime = Time.time;
        nextEnemySpawnTime = Time.time;
        ShowProgressText("Ổn định khe nứt: 0%");
        Debug.Log("S02 TimeRift stabilization started.");
    }

    private void UpdateStabilization()
    {
        float elapsed = Time.time - stabilizationStartTime;
        float normalized = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, stabilizeDuration));
        int percent = Mathf.RoundToInt(normalized * 100f);

        ShowProgressText("Ổn định khe nứt: " + percent + "%");

        if (Time.time >= nextEnemySpawnTime)
        {
            if (GetActivePressureEnemyCount() < maxActiveEnemies)
                SpawnEnemyAtIndex(Random.Range(0, GetSafeSpawnCount()), true);

            nextEnemySpawnTime = Time.time + Mathf.Max(1f, enemySpawnInterval);
        }

        if (!overloadWarningShown && normalized >= 0.58f)
        {
            overloadWarningShown = true;
            ShowWarning("Khe nứt đang quá tải!", 4.5f);
        }

        if (!friendsWarningShown && normalized >= 0.82f)
        {
            friendsWarningShown = true;
            ShowStory("Như Ý: An! Nó đang kéo tụi mình vào!", 4.8f);
        }

        if (elapsed >= stabilizeDuration)
            CompleteStabilization();
    }

    private void CompleteStabilization()
    {
        if (!stabilizationRunning)
            return;

        stabilizationRunning = false;
        HideProgressText();
        StartCoroutine(CompleteSequence());
    }

    private IEnumerator CompleteSequence()
    {
        SetPlayerCombat(false);

        if (cutsceneController != null)
        {
            yield return cutsceneController.PlayEnding(nextSceneName);
            yield break;
        }

        ShowStory("Khe nứt không ổn định nữa!", 3.8f);
        yield return new WaitForSeconds(4f);
        ShowStory("Tất cả bị kéo vào dòng chảy thời gian...", 4.5f);
        yield return new WaitForSeconds(2f);

        if (Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("S02 next scene is missing from Build Settings: " + nextSceneName);
        }
    }

    private void SpawnEnemyAtIndex(int spawnIndex, bool resonancePhase)
    {
        if (blackStarEnemyPrefab == null)
        {
            if (!warnedMissingEnemyPrefab)
            {
                warnedMissingEnemyPrefab = true;
                Debug.LogWarning("BlackStarEnemy prefab is missing. S02 continues without pressure enemies.");
            }

            return;
        }

        Transform spawnPoint = GetSpawnPointBehindPlayer(spawnIndex);
        Vector3 spawnPosition = GetSafeSpawnPosition(spawnPoint);
        Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
        GameObject enemy = Instantiate(blackStarEnemyPrefab, spawnPosition, spawnRotation);
        enemy.name = "S02_BlackStarEnemy";
        enemy.tag = "Enemy";

        EnemyHealth3D health = enemy.GetComponent<EnemyHealth3D>();
        if (health == null)
            health = enemy.AddComponent<EnemyHealth3D>();

        health.maxHP = resonancePhase ? 40 : 9999;

        EnemyChase3D chase = enemy.GetComponent<EnemyChase3D>();
        if (chase == null)
            chase = enemy.AddComponent<EnemyChase3D>();

        if (player != null)
            chase.target = player;

        chase.chaseRange = 120f;
        chase.attackRange = resonancePhase ? 1.7f : 1.35f;
        chase.moveSpeed = resonancePhase ? 3.4f : 2.8f;
        chase.damage = resonancePhase ? 14 : 9999;
        chase.attackCooldown = resonancePhase ? 1.3f : 1f;
    }

    private int GetActivePressureEnemyCount()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        int count = 0;

        foreach (GameObject enemy in enemies)
        {
            if (enemy != null && enemy.name.StartsWith("S02_BlackStarEnemy"))
                count++;
        }

        return count;
    }

    private int GetSafeSpawnCount()
    {
        if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
            return 1;

        return enemySpawnPoints.Length;
    }

    private Transform GetSpawnPointBehindPlayer(int spawnIndex)
    {
        if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
            return null;

        int safeIndex = Mathf.Clamp(spawnIndex, 0, enemySpawnPoints.Length - 1);
        Transform preferred = enemySpawnPoints[safeIndex];
        if (IsBehindPlayer(preferred))
            return preferred;

        foreach (Transform spawnPoint in enemySpawnPoints)
        {
            if (IsBehindPlayer(spawnPoint))
                return spawnPoint;
        }

        return preferred;
    }

    private Vector3 GetSafeSpawnPosition(Transform spawnPoint)
    {
        if (spawnPoint != null)
        {
            Vector3 spawnPosition = spawnPoint.position;
            if (player == null || spawnPosition.z <= player.position.z - 4f)
                return spawnPosition;
        }

        if (player != null)
            return player.position + new Vector3(0f, 0.4f, -12f);

        return transform.position;
    }

    private bool IsBehindPlayer(Transform spawnPoint)
    {
        return spawnPoint != null && (player == null || spawnPoint.position.z <= player.position.z - 4f);
    }

    private void FindReferencesIfNeeded()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (playerCombat == null && player != null)
            playerCombat = player.GetComponent<PlayerCombat3D>();

        if (warningUI == null)
            warningUI = FindAnyObjectByType<S01WarningTextUI>();

        if (interactionText == null)
            interactionText = FindTextInScene("InteractionText");

        if (progressText == null)
            progressText = FindTextInScene("WarningText");

        if (cutsceneController == null)
            cutsceneController = FindAnyObjectByType<S02CutsceneController>();

        if (cutsceneController == null)
            cutsceneController = gameObject.AddComponent<S02CutsceneController>();

        cutsceneController.player = player;
        cutsceneController.playerCombat = playerCombat;
        cutsceneController.warningUI = warningUI;
        cutsceneController.interactionText = interactionText;
        cutsceneController.timeRift = timeRift;
    }

    private void DisableBlockingTimeRiftVisualColliders()
    {
        Collider[] colliders = FindObjectsByType<Collider>(FindObjectsInactive.Include);
        foreach (Collider collider in colliders)
        {
            if (collider == null || collider.isTrigger)
                continue;

            if (!IsTimeRiftVisualCollider(collider.transform))
                continue;

            collider.enabled = false;
        }
    }

    private void EnsureTimeRiftChamberWalkableSurface()
    {
        CreateInvisibleWalkableBox(
            "S02_Runtime_TimeRift_PlayableFloor",
            new Vector3(0f, 0f, 164f),
            new Vector3(34f, 0.46f, 38f));

        CreateInvisibleWalkableBox(
            "S02_Runtime_TimeRift_EntryFiller",
            new Vector3(0f, 0f, 154f),
            new Vector3(14f, 0.46f, 16f));
    }

    private void CreateInvisibleWalkableBox(string objectName, Vector3 position, Vector3 scale)
    {
        if (GameObject.Find(objectName) != null)
            return;

        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = objectName;
        box.transform.position = position;
        box.transform.rotation = Quaternion.identity;
        box.transform.localScale = scale;

        Renderer renderer = box.GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = false;
    }

    private bool IsTimeRiftVisualCollider(Transform target)
    {
        while (target != null)
        {
            string objectName = target.name;
            if (objectName.StartsWith("Rift_Ring") ||
                objectName.StartsWith("TimeRift_InteractCircle") ||
                objectName.StartsWith("TimeRift_Core") ||
                objectName.StartsWith("TimeRift_InnerLight") ||
                objectName.StartsWith("TimeRiftChamber_MainFloor") ||
                objectName.StartsWith("S02_TimeRift_Core") ||
                objectName.StartsWith("S02_TimeRift_Ring") ||
                objectName.StartsWith("TimeRiftChamber_RaisedPlatform") ||
                IsOldTimeRiftEntranceWall(objectName))
            {
                return true;
            }

            target = target.parent;
        }

        return false;
    }

    private bool IsOldTimeRiftEntranceWall(string objectName)
    {
        return objectName.StartsWith("TimeRiftChamber_RoughWall_08") ||
               objectName.StartsWith("TimeRiftChamber_RoughWall_09") ||
               objectName.StartsWith("TimeRiftChamber_RoughWall_10") ||
               objectName.StartsWith("TimeRiftChamber_RoughWall_11") ||
               objectName.StartsWith("TimeRiftChamber_RoughWall_12");
    }

    private TMP_Text FindTextInScene(string objectName)
    {
        TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
        foreach (TMP_Text text in texts)
        {
            if (text.name == objectName && text.gameObject.scene.IsValid())
                return text;
        }

        return null;
    }

    private void SetPlayerCombat(bool enabled)
    {
        if (playerCombat == null)
            return;

        playerCombat.enabled = enabled;
        Debug.Log(enabled ? "S02 resonance combat enabled." : "S02 resonance combat disabled.");
    }

    private void ShowStory(string message, float duration)
    {
        if (warningUI != null)
            warningUI.ShowStory(message, duration);
    }

    private void ShowWarning(string message, float duration)
    {
        if (warningUI != null)
            warningUI.ShowWarning(message, duration);
    }

    private void ShowInteractionText(string message)
    {
        if (interactionText == null)
        {
            FindReferencesIfNeeded();
            if (interactionText == null)
            {
                if (!warnedMissingInteractionText)
                {
                    warnedMissingInteractionText = true;
                    Debug.LogWarning("InteractionText not found for S02 TimeRift prompt.");
                }

                return;
            }
        }

        interactionText.text = message;
        interactionText.gameObject.SetActive(true);
    }

    private void HideInteractionText()
    {
        if (interactionText != null)
            interactionText.gameObject.SetActive(false);
    }

    private void ShowProgressText(string message)
    {
        if (progressText == null)
            return;

        progressText.text = message;
        progressText.gameObject.SetActive(true);
    }

    private void HideProgressText()
    {
        if (progressText != null)
            progressText.gameObject.SetActive(false);
    }
}
