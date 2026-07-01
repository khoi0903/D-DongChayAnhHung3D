using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

public static class S03SinglePlayerArenaBuilder
{
    private const string RootName = "S03_SinglePlayerArena_Generated";
    private const string ScenePath = "Assets/Scenes/S03.unity";
    private const string BlessingFolder = "Assets/Blessings/S03";
    private const string MinionPrefabPath = "Assets/Prefabs/Minion.prefab";
    private const string PlayerModelPath = "Assets/Models/Player/Action_Anh_Thu/Action_Anh_Thu/Anh_Thu@Model.fbx";
    private const string PlayerAnimatorControllerPath = "Assets/Animations/Player/AnhThu.controller";
    private const string PlayerMaterialPath = "Assets/Models/Player/Materials/AnhThu_Player_Color.mat";

    [MenuItem("Tools/Dong Chay Anh Hung/Rebuild S03 Combat Arena")]
    public static void BuildScene()
    {
        OpenS03Scene();
        DeleteOldGeneratedObjects();
        List<BlessingDefinition> blessings = CreateBlessingAssets();

        GameObject root = new GameObject(RootName);
        Material floorMat = CreateMaterial("S03_Arena_Floor_Mat", new Color32(84, 88, 96, 255), 0.25f);
        Material wallMat = CreateMaterial("S03_Arena_Wall_Mat", new Color32(62, 66, 74, 255), 0.18f);
        Material accentMat = CreateMaterial("S03_Arena_Bronze_Mat", new Color32(184, 122, 40, 255), 0.35f);
        Material blueMat = CreateMaterial("S03_Arena_BlueGlow_Mat", new Color32(40, 180, 255, 255), 0.55f, new Color(0.05f, 0.7f, 1.3f));
        Material redMat = CreateMaterial("S03_Arena_RedGlow_Mat", new Color32(210, 48, 60, 255), 0.55f, new Color(1.2f, 0.08f, 0.08f));

        BuildArena(root, floorMat, wallMat, accentMat, blueMat, redMat, out Transform[] spawnPoints);
        SetupLighting();

        Camera mainCamera = SetupCamera();
        Transform player = SetupPlayer(mainCamera);
        Canvas canvas = EnsureCanvas();
        BuildHud(canvas, out TMP_Text waveText, out TMP_Text statusText, out GameObject choiceRoot, out BlessingChoiceUI[] choiceCards, out TMP_Text choiceTitle, out TMP_Text choiceResult);
        CreatePlayerHealthBar(canvas.transform, player.GetComponent<PlayerHealth3D>());

        BlessingRuntimeController runtime = player.GetComponent<BlessingRuntimeController>();
        if (runtime == null)
            runtime = player.gameObject.AddComponent<BlessingRuntimeController>();

        PlayerController3D controller = player.GetComponent<PlayerController3D>();
        PlayerCombat3D combat = player.GetComponent<PlayerCombat3D>();
        PlayerHealth3D health = player.GetComponent<PlayerHealth3D>();
        runtime.Configure(controller, combat, health);

        GameObject managerObject = new GameObject("S03_BlessingManager");
        managerObject.transform.SetParent(root.transform, false);
        BlessingManager blessingManager = managerObject.AddComponent<BlessingManager>();
        blessingManager.Configure(blessings, runtime, choiceRoot, choiceCards, choiceTitle, choiceResult);

        GameObject directorObject = new GameObject("S03_ArenaDirector");
        directorObject.transform.SetParent(root.transform, false);
        S03ArenaDirector director = directorObject.AddComponent<S03ArenaDirector>();
        director.Configure(
            player,
            AssetDatabase.LoadAssetAtPath<GameObject>(MinionPrefabPath),
            spawnPoints,
            blessingManager,
            runtime,
            waveText,
            statusText);

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        Debug.Log("S03 Combat Arena rebuilt with single-player wave combat and Blessing choices.");
    }

    public static void VerifyScene()
    {
        OpenS03Scene();

        RequireSceneObject(RootName);
        RequireSceneObject("S03_BlessingManager");
        RequireSceneObject("S03_ArenaDirector");
        RequireSceneObject("S03_BlessingChoiceRoot");

        GameObject player = GameObject.Find("Player");
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
            throw new UnityException("S03 verify failed: Player was not found.");

        RequireComponent<PlayerController3D>(player);
        RequireComponent<PlayerCombat3D>(player);
        RequireComponent<PlayerHealth3D>(player);
        RequireComponent<BlessingRuntimeController>(player);
        if (player.transform.Find("PlayerVisual") == null)
            throw new UnityException("S03 verify failed: PlayerVisual was not found on Player.");
        RequireSceneObject("S03_PlayerHealthRoot");

        if (Object.FindAnyObjectByType<S03ArenaDirector>() == null)
            throw new UnityException("S03 verify failed: S03ArenaDirector component was not found.");
        if (Object.FindAnyObjectByType<BlessingManager>() == null)
            throw new UnityException("S03 verify failed: BlessingManager component was not found.");
        if (Object.FindObjectsByType<BlessingChoiceUI>(FindObjectsInactive.Include).Length < 3)
            throw new UnityException("S03 verify failed: not enough BlessingChoiceUI cards.");

        string[] blessingGuids = AssetDatabase.FindAssets("t:BlessingDefinition", new[] { BlessingFolder });
        if (blessingGuids.Length < 20)
            throw new UnityException("S03 verify failed: expected 20 BlessingDefinition assets, found " + blessingGuids.Length + ".");

        Debug.Log("S03 verification passed: scene, player, arena director, UI, and 20 Blessing assets are present.");
    }

    private static void OpenS03Scene()
    {
        if (EditorSceneManager.GetActiveScene().path == ScenePath)
            return;

        EditorSceneManager.OpenScene(ScenePath);
    }

    private static void BuildArena(GameObject root, Material floorMat, Material wallMat, Material accentMat, Material blueMat, Material redMat, out Transform[] spawnPoints)
    {
        CreateCube(root, "Arena_Floor", new Vector3(0f, -0.15f, 0f), Vector3.zero, new Vector3(46f, 0.3f, 46f), floorMat);
        CreateCube(root, "Arena_NorthWall", new Vector3(0f, 1.5f, 23f), Vector3.zero, new Vector3(48f, 3f, 1.2f), wallMat);
        CreateCube(root, "Arena_SouthWall", new Vector3(0f, 1.5f, -23f), Vector3.zero, new Vector3(48f, 3f, 1.2f), wallMat);
        CreateCube(root, "Arena_EastWall", new Vector3(23f, 1.5f, 0f), Vector3.zero, new Vector3(1.2f, 3f, 48f), wallMat);
        CreateCube(root, "Arena_WestWall", new Vector3(-23f, 1.5f, 0f), Vector3.zero, new Vector3(1.2f, 3f, 48f), wallMat);

        GameObject centerSeal = CreatePrimitive(root, "CoLoa_Arena_CenterSeal", PrimitiveType.Cylinder, new Vector3(0f, 0.03f, 0f), Vector3.zero, new Vector3(9f, 0.06f, 9f), accentMat);
        RemoveCollider(centerSeal);

        for (int i = 0; i < 12; i++)
        {
            float angle = i * 30f;
            float radius = i % 2 == 0 ? 13.5f : 17.5f;
            Vector3 position = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * radius, 0.24f, Mathf.Cos(angle * Mathf.Deg2Rad) * radius);
            GameObject marker = CreateCube(root, "Arena_PathRune_" + i.ToString("00"), position, new Vector3(0f, angle, 0f), new Vector3(0.35f, 0.08f, 2.1f), i % 2 == 0 ? blueMat : accentMat);
            RemoveCollider(marker);
        }

        CreateCube(root, "Arena_Cover_Block_A", new Vector3(-10f, 0.6f, 7f), new Vector3(0f, 25f, 0f), new Vector3(4.2f, 1.2f, 1.4f), wallMat);
        CreateCube(root, "Arena_Cover_Block_B", new Vector3(9f, 0.6f, -8f), new Vector3(0f, -20f, 0f), new Vector3(4.2f, 1.2f, 1.4f), wallMat);
        CreateCube(root, "Arena_Cover_Block_C", new Vector3(0f, 0.55f, 13.5f), new Vector3(0f, 90f, 0f), new Vector3(3.6f, 1.1f, 1.1f), wallMat);
        CreateCube(root, "Arena_Cover_Block_D", new Vector3(0f, 0.55f, -13.5f), new Vector3(0f, 90f, 0f), new Vector3(3.6f, 1.1f, 1.1f), wallMat);

        spawnPoints = new Transform[8];
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            float angle = i * 45f;
            Vector3 position = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * 18f, 0.35f, Mathf.Cos(angle * Mathf.Deg2Rad) * 18f);
            GameObject point = new GameObject("S03_EnemySpawn_" + (i + 1).ToString("00"));
            point.transform.SetParent(root.transform, false);
            point.transform.localPosition = position;
            spawnPoints[i] = point.transform;

            GameObject visual = CreatePrimitive(root, "S03_EnemySpawnMarker_" + (i + 1).ToString("00"), PrimitiveType.Cylinder, position, Vector3.zero, new Vector3(1.1f, 0.05f, 1.1f), redMat);
            RemoveCollider(visual);
        }
    }

    private static Transform SetupPlayer(Camera mainCamera)
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
        }

        TrySetTag(player, "Player");

        CharacterController characterController = player.GetComponent<CharacterController>();
        if (characterController == null)
            characterController = player.AddComponent<CharacterController>();

        bool wasEnabled = characterController.enabled;
        characterController.enabled = false;
        player.transform.position = new Vector3(0f, 1.05f, 0f);
        player.transform.rotation = Quaternion.identity;
        characterController.height = Mathf.Max(characterController.height, 1.8f);
        characterController.radius = Mathf.Max(characterController.radius, 0.32f);
        characterController.center = new Vector3(0f, 0f, 0f);
        characterController.enabled = wasEnabled;

        PlayerController3D controller = player.GetComponent<PlayerController3D>();
        if (controller == null)
            controller = player.AddComponent<PlayerController3D>();

        controller.moveSpeed = 8.2f;
        controller.dashSpeed = 17f;
        controller.dashDuration = 0.18f;
        controller.dashCooldown = 0.4f;
        controller.dashTowardsMouse = true;
        controller.cameraTransform = mainCamera != null ? mainCamera.transform : null;

        PlayerHealth3D health = player.GetComponent<PlayerHealth3D>();
        if (health == null)
            health = player.AddComponent<PlayerHealth3D>();

        health.maxHP = 100;
        health.currentHP = health.maxHP;
        health.isDead = false;

        PlayerCombat3D combat = player.GetComponent<PlayerCombat3D>();
        if (combat == null)
            combat = player.AddComponent<PlayerCombat3D>();

        combat.enabled = true;
        combat.damage = 28;
        combat.attackRange = 4.8f;
        combat.attackAngle = 105f;
        combat.closeHitRadius = 1.45f;
        combat.attackCooldown = 0.55f;
        combat.knockbackForce = 6.8f;
        combat.enemyStunDuration = 0.36f;
        combat.heavyDamage = 70;
        combat.heavyAttackRange = 6.2f;
        combat.heavyAttackAngle = 128f;
        combat.heavyCloseHitRadius = 1.8f;
        combat.heavyAttackCooldown = 1.05f;
        combat.heavyWindup = 0.18f;
        combat.heavyKnockbackForce = 11.5f;
        combat.heavyEnemyStunDuration = 0.7f;
        combat.aimCamera = mainCamera;

        BlessingRuntimeController runtime = player.GetComponent<BlessingRuntimeController>();
        if (runtime == null)
            runtime = player.AddComponent<BlessingRuntimeController>();

        runtime.Configure(controller, combat, health);
        SetupPlayerVisual(player);

        if (mainCamera != null)
        {
            ThirdPersonCamera followCamera = mainCamera.GetComponent<ThirdPersonCamera>();
            if (followCamera == null)
                followCamera = mainCamera.gameObject.AddComponent<ThirdPersonCamera>();

            followCamera.target = player.transform;
            followCamera.distance = 8.8f;
            followCamera.height = 3.1f;
            followCamera.fixedAngle = true;
            followCamera.fixedYaw = 45f;
            followCamera.fixedPitch = 42f;
            followCamera.lockCursor = false;
        }

        return player.transform;
    }

    private static void SetupPlayerVisual(GameObject player)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerModelPath);
        if (model == null)
        {
            Debug.LogWarning("S03 builder could not find player model at " + PlayerModelPath);
            return;
        }

        Transform oldVisual = player.transform.Find("PlayerVisual");
        if (oldVisual != null)
            Object.DestroyImmediate(oldVisual.gameObject);

        GameObject visual = PrefabUtility.InstantiatePrefab(model, player.transform) as GameObject;
        if (visual == null)
            visual = Object.Instantiate(model, player.transform);

        visual.name = "PlayerVisual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        NormalizeVisualScale(visual.transform, 1.75f);
        AlignVisualToControllerFeet(player, visual.transform);
        ApplyPlayerMaterial(visual);
        ConfigureVisualAnimator(visual);
        RemovePrimitivePlayerBody(player);

        PlayerAnimatorDriver driver = player.GetComponent<PlayerAnimatorDriver>();
        if (driver == null)
            driver = player.AddComponent<PlayerAnimatorDriver>();

        EditorUtility.SetDirty(driver);
        EditorUtility.SetDirty(visual);
        EditorUtility.SetDirty(player);
    }

    private static void ConfigureVisualAnimator(GameObject visual)
    {
        Animator animator = visual.GetComponent<Animator>();
        if (animator == null)
            animator = visual.AddComponent<Animator>();

        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerAnimatorControllerPath);
        if (controller != null)
            animator.runtimeAnimatorController = controller;

        Avatar avatar = FindPlayerAvatar();
        if (avatar != null)
            animator.avatar = avatar;

        animator.applyRootMotion = false;
        animator.updateMode = AnimatorUpdateMode.Normal;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        EditorUtility.SetDirty(animator);
    }

    private static Avatar FindPlayerAvatar()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(PlayerModelPath);
        foreach (Object asset in assets)
        {
            Avatar avatar = asset as Avatar;
            if (avatar != null)
                return avatar;
        }

        return null;
    }

    private static void ApplyPlayerMaterial(GameObject visual)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(PlayerMaterialPath);
        if (material == null)
            return;

        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            int materialCount = Mathf.Max(1, renderer.sharedMaterials.Length);
            Material[] materials = new Material[materialCount];
            for (int i = 0; i < materials.Length; i++)
                materials[i] = material;

            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);
        }
    }

    private static void AlignVisualToControllerFeet(GameObject player, Transform visual)
    {
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller == null || visual == null)
            return;

        Vector3 localPosition = visual.localPosition;
        localPosition.y = controller.center.y - controller.height * 0.5f;
        visual.localPosition = localPosition;
    }

    private static void NormalizeVisualScale(Transform visual, float targetHeight)
    {
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        if (bounds.size.y <= 0.01f)
            return;

        visual.localScale *= targetHeight / bounds.size.y;
    }

    private static void RemovePrimitivePlayerBody(GameObject player)
    {
        MeshRenderer meshRenderer = player.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
            Object.DestroyImmediate(meshRenderer);

        MeshFilter meshFilter = player.GetComponent<MeshFilter>();
        if (meshFilter != null)
            Object.DestroyImmediate(meshFilter);

        CapsuleCollider capsuleCollider = player.GetComponent<CapsuleCollider>();
        if (capsuleCollider != null)
            Object.DestroyImmediate(capsuleCollider);
    }

    private static Camera SetupCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObject = GameObject.Find("Main Camera");
            if (cameraObject == null)
                cameraObject = new GameObject("Main Camera");

            mainCamera = cameraObject.GetComponent<Camera>();
            if (mainCamera == null)
                mainCamera = cameraObject.AddComponent<Camera>();

            TrySetTag(cameraObject, "MainCamera");
        }

        mainCamera.transform.position = new Vector3(-8.4f, 5.9f, -8.4f);
        mainCamera.transform.rotation = Quaternion.Euler(42f, 45f, 0f);
        mainCamera.fieldOfView = 46f;
        mainCamera.orthographic = false;
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 180f;
        return mainCamera;
    }

    private static void SetupLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.22f, 0.24f, 0.28f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.16f, 0.17f, 0.2f);
        RenderSettings.fogDensity = 0.009f;

        Light directional = Object.FindAnyObjectByType<Light>();
        if (directional == null || directional.type != LightType.Directional)
        {
            GameObject lightObject = new GameObject("Directional Light");
            directional = lightObject.AddComponent<Light>();
            directional.type = LightType.Directional;
        }

        directional.intensity = 1.05f;
        directional.color = new Color(0.92f, 0.94f, 1f);
        directional.transform.rotation = Quaternion.Euler(54f, -38f, 0f);
    }

    private static void BuildHud(Canvas canvas, out TMP_Text waveText, out TMP_Text statusText, out GameObject choiceRoot, out BlessingChoiceUI[] choiceCards, out TMP_Text choiceTitle, out TMP_Text choiceResult)
    {
        waveText = CreateText(canvas.transform, "S03_WaveText", new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(520f, 52f), 34, TextAlignmentOptions.Center);
        waveText.text = "S03 ARENA";

        statusText = CreateText(canvas.transform, "S03_StatusText", new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(980f, 54f), 24, TextAlignmentOptions.Center);
        statusText.text = "WASD di chuyen | Mouse0 danh thuong | Mouse1 heavy | Shift Dash";

        TMP_Text hintText = CreateText(canvas.transform, "S03_ControlHintText", new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(1200f, 48f), 22, TextAlignmentOptions.Center);
        hintText.text = "Sau moi wave, chon 1 trong 3 Chuc Phuc Anh Linh de tao build rieng.";

        choiceRoot = new GameObject("S03_BlessingChoiceRoot");
        choiceRoot.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = choiceRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image backdrop = choiceRoot.AddComponent<Image>();
        backdrop.color = new Color(0.02f, 0.025f, 0.035f, 0.86f);

        choiceTitle = CreateText(choiceRoot.transform, "S03_BlessingChoiceTitle", new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(900f, 72f), 42, TextAlignmentOptions.Center);
        choiceTitle.text = "CHON CHUC PHUC ANH LINH";

        choiceResult = CreateText(choiceRoot.transform, "S03_BlessingChoiceResult", new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(1100f, 80f), 24, TextAlignmentOptions.Center);
        choiceResult.text = string.Empty;

        choiceCards = new BlessingChoiceUI[3];
        float[] xOffsets = { -390f, 0f, 390f };
        for (int i = 0; i < choiceCards.Length; i++)
            choiceCards[i] = CreateBlessingCard(choiceRoot.transform, i, xOffsets[i]);

        choiceRoot.SetActive(false);
    }

    private static void CreatePlayerHealthBar(Transform parent, PlayerHealth3D health)
    {
        GameObject root = new GameObject("S03_PlayerHealthRoot");
        root.transform.SetParent(parent, false);
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = new Vector2(34f, -34f);
        rootRect.sizeDelta = new Vector2(390f, 82f);

        Image panel = root.AddComponent<Image>();
        panel.color = new Color(0.035f, 0.025f, 0.028f, 0.82f);

        TMP_Text nameText = CreateText(root.transform, "S03_PlayerHealthName", new Vector2(0f, 1f), new Vector2(92f, -21f), new Vector2(180f, 26f), 22, TextAlignmentOptions.Left);
        nameText.text = "Anh Thu";

        TMP_Text valueText = CreateText(root.transform, "S03_PlayerHealthValue", new Vector2(1f, 1f), new Vector2(-78f, -21f), new Vector2(140f, 26f), 20, TextAlignmentOptions.Right);
        valueText.text = health != null ? health.currentHP + " / " + health.maxHP : "100 / 100";

        GameObject sliderObject = new GameObject("S03_PlayerHealthSlider");
        sliderObject.transform.SetParent(root.transform, false);
        RectTransform sliderRect = sliderObject.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0f);
        sliderRect.anchorMax = new Vector2(1f, 0f);
        sliderRect.pivot = new Vector2(0.5f, 0f);
        sliderRect.anchoredPosition = new Vector2(0f, 17f);
        sliderRect.sizeDelta = new Vector2(-30f, 26f);

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = health != null ? health.maxHP : 100f;
        slider.value = health != null ? health.currentHP : 100f;

        Image background = CreatePanelImage(sliderObject.transform, "Background", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.18f, 0.025f, 0.035f, 1f));
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(4f, 4f);
        fillAreaRect.offsetMax = new Vector2(-4f, -4f);

        Image fill = CreatePanelImage(fillArea.transform, "Fill", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.86f, 0.1f, 0.13f, 1f));
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        slider.fillRect = fillRect;
        slider.targetGraphic = fill;

        PlayerHealthUI healthUI = root.AddComponent<PlayerHealthUI>();
        healthUI.playerHealth = health;
        healthUI.healthSlider = slider;
        healthUI.healthText = valueText;
        healthUI.fillImage = fill;
    }

    private static BlessingChoiceUI CreateBlessingCard(Transform parent, int index, float xOffset)
    {
        GameObject card = new GameObject("S03_BlessingCard_" + (index + 1));
        card.transform.SetParent(parent, false);
        RectTransform rect = card.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(xOffset, 0f);
        rect.sizeDelta = new Vector2(330f, 430f);

        Image frame = card.AddComponent<Image>();
        frame.color = new Color(0.12f, 0.14f, 0.18f, 0.96f);
        Button button = card.AddComponent<Button>();
        button.targetGraphic = frame;

        Image icon = CreatePanelImage(card.transform, "Icon", new Vector2(0.5f, 0.78f), new Vector2(0f, 0f), new Vector2(96f, 96f), new Color(1f, 1f, 1f, 0.2f));
        TMP_Text hero = CreateText(card.transform, "Hero", new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(290f, 38f), 18, TextAlignmentOptions.Center);
        TMP_Text name = CreateText(card.transform, "Name", new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(290f, 62f), 26, TextAlignmentOptions.Center);
        TMP_Text description = CreateText(card.transform, "Description", new Vector2(0.5f, 0.34f), Vector2.zero, new Vector2(280f, 116f), 20, TextAlignmentOptions.Center);
        TMP_Text rarity = CreateText(card.transform, "Rarity", new Vector2(0.5f, 0.16f), Vector2.zero, new Vector2(280f, 36f), 20, TextAlignmentOptions.Center);
        TMP_Text stack = CreateText(card.transform, "Stack", new Vector2(0.5f, 0.08f), Vector2.zero, new Vector2(280f, 32f), 18, TextAlignmentOptions.Center);

        BlessingChoiceUI cardUI = card.AddComponent<BlessingChoiceUI>();
        cardUI.ConfigureReferences(button, frame, icon, hero, name, description, rarity, stack);
        return cardUI;
    }

    private static Image CreatePanelImage(Transform parent, string name, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = obj.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static TMP_Text CreateText(Transform parent, string name, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private static void RequireSceneObject(string objectName)
    {
        if (FindSceneObject(objectName) == null)
            throw new UnityException("S03 verify failed: missing scene object " + objectName + ".");
    }

    private static GameObject FindSceneObject(string objectName)
    {
        GameObject activeObject = GameObject.Find(objectName);
        if (activeObject != null)
            return activeObject;

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform sceneTransform in transforms)
        {
            if (sceneTransform != null &&
                sceneTransform.name == objectName &&
                sceneTransform.gameObject.scene.IsValid())
            {
                return sceneTransform.gameObject;
            }
        }

        return null;
    }

    private static void RequireComponent<T>(GameObject obj) where T : Component
    {
        if (obj.GetComponent<T>() == null)
            throw new UnityException("S03 verify failed: " + obj.name + " is missing " + typeof(T).Name + ".");
    }

    private static Canvas EnsureCanvas()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        return canvas;
    }

    private static List<BlessingDefinition> CreateBlessingAssets()
    {
        EnsureFolder("Assets", "Blessings");
        EnsureFolder("Assets/Blessings", "S03");

        List<BlessingDefinition> blessings = new List<BlessingDefinition>
        {
            CreateOrUpdateBlessing("ADV_ThanhGiapAuLac", "adv_thanh_giap_au_lac", "Th\u00e0nh Gi\u00e1p \u00c2u L\u1ea1c", HeroType.AnDuongVuong, "Tang giap va giam sat thuong nhan vao.", BlessingRarity.Common, 3, BlessingEffectType.Armor, false),
            CreateOrUpdateBlessing("ADV_NoThan", "adv_no_than", "N\u1ecf Th\u1ea7n", HeroType.AnDuongVuong, "Moi don danh thu 5 ban them 3 mui ten nang luong.", BlessingRarity.Rare, 3, BlessingEffectType.DivineCrossbow, false),
            CreateOrUpdateBlessing("ADV_TuongThanh", "adv_tuong_thanh", "T\u01b0\u1eddng Th\u00e0nh", HeroType.AnDuongVuong, "Dash tao ket gioi ngan, lam cham va chan nhip tan cong cua ke dich.", BlessingRarity.Epic, 3, BlessingEffectType.DashBarrier, false),
            CreateOrUpdateBlessing("ADV_CanhGioi", "adv_canh_gioi", "C\u1ea3nh Gi\u1edbi", HeroType.AnDuongVuong, "Bao som wave tiep theo va tang tam phat hien cua dau truong.", BlessingRarity.Common, 3, BlessingEffectType.Awareness, false),
            CreateOrUpdateBlessing("ADV_ThanhCoLoa", "adv_thanh_co_loa", "Th\u00e0nh C\u1ed5 Loa", HeroType.AnDuongVuong, "Ultimate: dinh ky tao la chan dien rong bao ve ban than.", BlessingRarity.Legendary, 1, BlessingEffectType.CoLoaCitadel, true),

            CreateOrUpdateBlessing("TT_HieuTrieu", "tt_hieu_trieu", "Hi\u1ec7u Tri\u1ec7u", HeroType.TrungTrac, "Mau cang thap, sat thuong cang cao.", BlessingRarity.Rare, 3, BlessingEffectType.LowHealthDamage, false),
            CreateOrUpdateBlessing("TT_CoKhoiNghia", "tt_co_khoi_nghia", "C\u1edd Kh\u1edfi Ngh\u0129a", HeroType.TrungTrac, "Tang toc do danh.", BlessingRarity.Common, 3, BlessingEffectType.AttackSpeed, false),
            CreateOrUpdateBlessing("TT_KhoiNghiaMeLinh", "tt_khoi_nghia_me_linh", "Kh\u1edfi Ngh\u0129a M\u00ea Linh", HeroType.TrungTrac, "Ha du so quai se hoi nang luong ky nang va mot it mau.", BlessingRarity.Epic, 3, BlessingEffectType.KillSkillEnergy, false),
            CreateOrUpdateBlessing("TT_NuVuong", "tt_nu_vuong", "N\u1eef V\u01b0\u01a1ng", HeroType.TrungTrac, "Nhan them mot lan hoi sinh trong luot choi.", BlessingRarity.Legendary, 1, BlessingEffectType.Revive, false),
            CreateOrUpdateBlessing("TT_HaiBaKhoiNghia", "tt_hai_ba_khoi_nghia", "Hai B\u00e0 Kh\u1edfi Ngh\u0129a", HeroType.TrungTrac, "Ultimate: sat thuong tang theo so ke dich xung quanh.", BlessingRarity.Legendary, 1, BlessingEffectType.Uprising, true),

            CreateOrUpdateBlessing("TN_KyTuong", "tn_ky_tuong", "K\u1ef5 T\u01b0\u1ee3ng", HeroType.TrungNhi, "Tang toc do di chuyen.", BlessingRarity.Common, 3, BlessingEffectType.MoveSpeed, false),
            CreateOrUpdateBlessing("TN_XungPhong", "tn_xung_phong", "Xung Phong", HeroType.TrungNhi, "Dash gay sat thuong len ke dich tren duong luot.", BlessingRarity.Rare, 3, BlessingEffectType.DashDamage, false),
            CreateOrUpdateBlessing("TN_TruyKich", "tn_truy_kich", "Truy K\u00edch", HeroType.TrungNhi, "Don danh dau tien sau Dash gay them sat thuong.", BlessingRarity.Epic, 3, BlessingEffectType.PostDashDamage, false),
            CreateOrUpdateBlessing("TN_BongChienTruong", "tn_bong_chien_truong", "B\u00f3ng Chi\u1ebfn Tr\u01b0\u1eddng", HeroType.TrungNhi, "Dash tao phan than ngan han lam roi loan ke dich.", BlessingRarity.Epic, 3, BlessingEffectType.DashDecoy, false),
            CreateOrUpdateBlessing("TN_VoiChien", "tn_voi_chien", "Voi Chi\u1ebfn", HeroType.TrungNhi, "Ultimate: Dash xuyen qua ke dich va gay sat thuong lon.", BlessingRarity.Legendary, 1, BlessingEffectType.WarElephant, true),

            CreateOrUpdateBlessing("QT_HanhQuanThanToc", "qt_hanh_quan_than_toc", "H\u00e0nh Qu\u00e2n Th\u1ea7n T\u1ed1c", HeroType.QuangTrung, "Tang toc do danh.", BlessingRarity.Common, 3, BlessingEffectType.AttackSpeed, false),
            CreateOrUpdateBlessing("QT_DongDa", "qt_dong_da", "\u0110\u1ed1ng \u0110a", HeroType.QuangTrung, "Tang sat thuong chi mang.", BlessingRarity.Rare, 3, BlessingEffectType.CriticalPower, false),
            CreateOrUpdateBlessing("QT_ThanTocBacTien", "qt_than_toc_bac_tien", "Th\u1ea7n T\u1ed1c B\u1eafc Ti\u1ebfn", HeroType.QuangTrung, "Giam thoi gian hoi Dash.", BlessingRarity.Common, 3, BlessingEffectType.DashCooldown, false),
            CreateOrUpdateBlessing("QT_ThienLoiTaySon", "qt_thien_loi_tay_son", "Thi\u00ean L\u00f4i T\u00e2y S\u01a1n", HeroType.QuangTrung, "Don chi mang co ti le goi set danh xuong muc tieu.", BlessingRarity.Epic, 3, BlessingEffectType.CriticalLightning, false),
            CreateOrUpdateBlessing("QT_XuanKyDau", "qt_xuan_ky_dau", "Xu\u00e2n K\u1ef7 D\u1eadu", HeroType.QuangTrung, "Ultimate: cuong chien trong vai giay, tang toc danh, sat thuong va giam hoi Dash.", BlessingRarity.Legendary, 1, BlessingEffectType.KyDauFrenzy, true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return blessings;
    }

    private static BlessingDefinition CreateOrUpdateBlessing(
        string assetName,
        string id,
        string displayName,
        HeroType hero,
        string description,
        BlessingRarity rarity,
        int maxStack,
        BlessingEffectType effect,
        bool ultimate)
    {
        string path = BlessingFolder + "/" + assetName + ".asset";
        BlessingDefinition blessing = AssetDatabase.LoadAssetAtPath<BlessingDefinition>(path);
        if (blessing == null)
        {
            blessing = ScriptableObject.CreateInstance<BlessingDefinition>();
            AssetDatabase.CreateAsset(blessing, path);
        }

        blessing.Configure(id, displayName, hero, description, rarity, maxStack, effect, ultimate);
        return blessing;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }

    private static GameObject CreatePrimitive(GameObject root, string name, PrimitiveType primitiveType, Vector3 position, Vector3 rotation, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(primitiveType);
        obj.name = name;
        obj.transform.SetParent(root.transform, false);
        obj.transform.localPosition = position;
        obj.transform.localEulerAngles = rotation;
        obj.transform.localScale = scale;
        SetMaterial(obj, material);
        return obj;
    }

    private static GameObject CreateCube(GameObject root, string name, Vector3 position, Vector3 rotation, Vector3 scale, Material material)
    {
        return CreatePrimitive(root, name, PrimitiveType.Cube, position, rotation, scale, material);
    }

    private static void SetMaterial(GameObject obj, Material material)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && material != null)
            renderer.sharedMaterial = material;
    }

    private static void RemoveCollider(GameObject obj)
    {
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);
    }

    private static Material CreateMaterial(string name, Color color, float smoothness, Color? emission = null)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader)
        {
            name = name,
            color = color
        };

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0f);

        if (emission.HasValue)
        {
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", emission.Value);
        }

        return material;
    }

    private static void DeleteOldGeneratedObjects()
    {
        string[] names =
        {
            RootName,
            "S03_BlessingChoiceRoot",
            "S03_WaveText",
            "S03_StatusText",
            "S03_ControlHintText",
            "S03_PlayerHealthRoot",
            "S03_BlessingManager",
            "S03_ArenaDirector"
        };

        foreach (string objectName in names)
            DeleteAllSceneObjectsNamed(objectName);

        DeleteAllSceneObjectsWithPrefix("S03_Wave");
        DeleteAllSceneObjectsWithPrefix("S03_EnemySpawnMarker");
        DeleteAllSceneObjectsWithPrefix("S03_EnemySpawn_");
        DeleteAllSceneObjectsWithPrefix("vietnam_city_game_map");
        DeleteAllSceneObjectsWithPrefix("Vietnam_City_Game_Map");
        DeleteAllSceneObjectsNamed("minion");
        DeleteAllSceneObjectsNamed("Minion");
    }

    private static void DeleteAllSceneObjectsNamed(string objectName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform sceneTransform in transforms)
        {
            if (sceneTransform == null ||
                sceneTransform.name != objectName ||
                !sceneTransform.gameObject.scene.IsValid())
            {
                continue;
            }

            Object.DestroyImmediate(sceneTransform.gameObject);
        }
    }

    private static void DeleteAllSceneObjectsWithPrefix(string namePrefix)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform sceneTransform in transforms)
        {
            if (sceneTransform == null ||
                !sceneTransform.name.StartsWith(namePrefix) ||
                !sceneTransform.gameObject.scene.IsValid())
            {
                continue;
            }

            Object.DestroyImmediate(sceneTransform.gameObject);
        }
    }

    private static void TrySetTag(GameObject obj, string tagName)
    {
        try
        {
            obj.tag = tagName;
        }
        catch (UnityException)
        {
            Debug.LogWarning("Tag not found: " + tagName + ". Please add it in Project Settings if needed.");
        }
    }
}
