#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// S03CoLoaArenaBuilder.cs
/// Editor script giúp tự động hóa quá trình tích hợp S03 Arena vào map Cổ Loa.
/// 
/// Chức năng:
/// 1. Tạo Director, Player, Minion Spawners.
/// 2. Tạo toàn bộ hệ thống UI cho Thanh Máu và Chọn Blessing.
/// 3. Sinh dữ liệu ScriptableObject cho các Blessing của 4 nhánh Anh hùng.
/// 4. Dây dợ (wire up) toàn bộ Component lại với nhau.
/// </summary>
public static class S03CoLoaArenaBuilder
{
    private const string MenuPath = "Cổ Loa/Tạo S03 Combat Arena";

    [MenuItem(MenuPath, false, 300)]
    public static void BuildCoLoaArena()
    {
        Debug.Log("Bắt đầu khởi tạo hệ thống S03 Combat Arena cho map Cổ Loa...");

        // Xóa hệ thống cũ nếu có
        CleanupOldArena();

        // 1. Tạo Arena Root
        GameObject rootObj = new GameObject("S03_CoLoa_Arena");
        Undo.RegisterCreatedObjectUndo(rootObj, "Build CoLoa Arena");

        // 2. Tạo hệ thống Blessing UI và Manager
        GameObject uiRoot = CreateBlessingUI(rootObj.transform, out BlessingChoiceUI[] choiceCards, out TMP_Text titleText, out TMP_Text resultText, out Slider healthSlider, out TMP_Text healthText, out Image healthFill);
        
        GameObject managerObj = new GameObject("S03_BlessingManager");
        managerObj.transform.SetParent(rootObj.transform);
        BlessingManager blessingManager = managerObj.AddComponent<BlessingManager>();

        // 3. Tạo Player (với đầy đủ Controller, Animator, Health, Combat, BlessingRuntime)
        GameObject playerObj = CreatePlayer(rootObj.transform, healthSlider, healthText, healthFill);
        BlessingRuntimeController blessingRuntime = playerObj.GetComponent<BlessingRuntimeController>();

        // 4. Tạo Dữ liệu Blessing (ScriptableObjects)
        List<BlessingDefinition> definitions = CreateBlessingDefinitions();

        // 5. Cấu hình BlessingManager
        blessingManager.Configure(definitions, blessingRuntime, uiRoot, choiceCards, titleText, resultText);

        // 6. Tạo Director và Minion Spawners
        GameObject directorObj = new GameObject("S03_ArenaDirector");
        directorObj.transform.SetParent(rootObj.transform);
        S03ArenaDirector director = directorObj.AddComponent<S03ArenaDirector>();
        Transform[] spawnPoints = CreateSpawnPoints(directorObj.transform);
        
        director.Configure(playerObj.transform, null, spawnPoints, blessingManager, blessingRuntime, null, null);

        // 7. Tạo hệ thống EventSystem cho UI
        CreateEventSystem();

        Debug.Log("Hoàn tất! Hãy chỉnh sửa giao diện UI và gán Minion Prefab vào S03_ArenaDirector.");
    }

    private static void CleanupOldArena()
    {
        GameObject oldRoot = GameObject.Find("S03_CoLoa_Arena");
        if (oldRoot != null) Undo.DestroyObjectImmediate(oldRoot);
        
        GameObject oldEventSystem = GameObject.Find("EventSystem");
        if (oldEventSystem != null) Undo.DestroyObjectImmediate(oldEventSystem);
    }

    private static GameObject CreatePlayer(Transform parent, Slider slider, TMP_Text hpText, Image hpFill)
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            playerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObj.name = "Player_CoLoa";
            playerObj.tag = "Player";
            playerObj.transform.position = Vector3.zero;
        }

        playerObj.transform.SetParent(parent);

        // Đảm bảo có các Component cơ bản
        if (!playerObj.GetComponent<CharacterController>()) playerObj.AddComponent<CharacterController>();
        if (!playerObj.GetComponent<PlayerController3D>()) playerObj.AddComponent<PlayerController3D>();
        if (!playerObj.GetComponent<InputSettingsManager>()) playerObj.AddComponent<InputSettingsManager>();

        // Thêm hệ thống máu
        PlayerHealth3D health = playerObj.GetComponent<PlayerHealth3D>();
        if (health == null) health = playerObj.AddComponent<PlayerHealth3D>();
        health.maxHP = 100;
        health.currentHP = 100;

        // Thêm combat
        PlayerCombat3D combat = playerObj.GetComponent<PlayerCombat3D>();
        if (combat == null) combat = playerObj.AddComponent<PlayerCombat3D>();

        // Thêm Blessing Runtime
        BlessingRuntimeController runtime = playerObj.GetComponent<BlessingRuntimeController>();
        if (runtime == null) runtime = playerObj.AddComponent<BlessingRuntimeController>();
        runtime.Configure(playerObj.GetComponent<PlayerController3D>(), combat, health);

        // Thêm Animator Driver
        if (!playerObj.GetComponent<PlayerAnimatorDriver>()) playerObj.AddComponent<PlayerAnimatorDriver>();

        // UI máu
        PlayerHealthUI healthUI = playerObj.GetComponent<PlayerHealthUI>();
        if (healthUI == null) healthUI = playerObj.AddComponent<PlayerHealthUI>();
        healthUI.playerHealth = health;
        healthUI.healthSlider = slider;
        healthUI.healthText = hpText;
        healthUI.fillImage = hpFill;

        return playerObj;
    }

    private static Transform[] CreateSpawnPoints(Transform parent)
    {
        int count = 6;
        float radius = 15f;
        Transform[] spawners = new Transform[count];

        for (int i = 0; i < count; i++)
        {
            GameObject spawner = new GameObject("SpawnPoint_" + i);
            spawner.transform.SetParent(parent);
            float angle = i * Mathf.PI * 2 / count;
            spawner.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            spawners[i] = spawner.transform;
        }

        return spawners;
    }

    private static GameObject CreateBlessingUI(Transform parent, out BlessingChoiceUI[] choiceCards, out TMP_Text titleText, out TMP_Text resultText, out Slider healthSlider, out TMP_Text hpText, out Image hpFill)
    {
        // 1. Tạo Canvas
        GameObject canvasObj = new GameObject("S03_UICanvas");
        canvasObj.transform.SetParent(parent);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();

        // 2. Giao diện Thanh Máu (góc trái trên)
        GameObject healthRoot = new GameObject("HealthBar");
        healthRoot.transform.SetParent(canvasObj.transform, false);
        RectTransform hpRect = healthRoot.AddComponent<RectTransform>();
        hpRect.anchorMin = new Vector2(0, 1);
        hpRect.anchorMax = new Vector2(0, 1);
        hpRect.pivot = new Vector2(0, 1);
        hpRect.anchoredPosition = new Vector2(20, -20);
        hpRect.sizeDelta = new Vector2(300, 30);

        // Tạo Background cho Slider
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(healthRoot.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.5f);
        RectTransform bgRect = bgImg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Tạo Fill Area
        GameObject fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(healthRoot.transform, false);
        RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero; fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = new Vector2(-10, -10); // Margin

        // Tạo Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        hpFill = fillObj.AddComponent<Image>();
        hpFill.color = Color.red;
        RectTransform fillRect = hpFill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        // Slider Component
        healthSlider = healthRoot.AddComponent<Slider>();
        healthSlider.interactable = false;
        healthSlider.transition = Selectable.Transition.None;
        healthSlider.fillRect = fillRect;

        // HP Text
        GameObject textObj = new GameObject("HP_Text");
        textObj.transform.SetParent(healthRoot.transform, false);
        hpText = textObj.AddComponent<TextMeshProUGUI>();
        hpText.alignment = TextAlignmentOptions.Center;
        hpText.color = Color.white;
        RectTransform textRect = hpText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        // 3. Giao diện Chọn Blessing
        GameObject choiceRoot = new GameObject("BlessingChoiceUI");
        choiceRoot.transform.SetParent(canvasObj.transform, false);
        RectTransform choiceRect = choiceRoot.AddComponent<RectTransform>();
        choiceRect.anchorMin = Vector2.zero; choiceRect.anchorMax = Vector2.one;
        choiceRect.sizeDelta = Vector2.zero;
        Image dimBg = choiceRoot.AddComponent<Image>();
        dimBg.color = new Color(0, 0, 0, 0.75f);

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(choiceRoot.transform, false);
        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "CHỌN CHÚC PHÚC ANH LINH";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 40;
        RectTransform titleRt = titleText.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 0.8f);
        titleRt.anchorMax = new Vector2(1, 0.9f);
        titleRt.sizeDelta = Vector2.zero;

        // Result Text
        GameObject resultObj = new GameObject("ResultText");
        resultObj.transform.SetParent(choiceRoot.transform, false);
        resultText = resultObj.AddComponent<TextMeshProUGUI>();
        resultText.text = "";
        resultText.alignment = TextAlignmentOptions.Center;
        resultText.fontSize = 24;
        RectTransform resultRt = resultText.GetComponent<RectTransform>();
        resultRt.anchorMin = new Vector2(0, 0.1f);
        resultRt.anchorMax = new Vector2(1, 0.2f);
        resultRt.sizeDelta = Vector2.zero;

        // Cards Container
        GameObject containerObj = new GameObject("CardsContainer");
        containerObj.transform.SetParent(choiceRoot.transform, false);
        HorizontalLayoutGroup layout = containerObj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 30;
        layout.childAlignment = TextAnchor.MiddleCenter;
        RectTransform containerRt = containerObj.GetComponent<RectTransform>();
        containerRt.anchorMin = new Vector2(0.1f, 0.3f);
        containerRt.anchorMax = new Vector2(0.9f, 0.7f);
        containerRt.sizeDelta = Vector2.zero;

        // Create 3 Cards
        choiceCards = new BlessingChoiceUI[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject cardObj = new GameObject("Card_" + i);
            cardObj.transform.SetParent(containerObj.transform, false);
            
            Image frameImg = cardObj.AddComponent<Image>();
            Button btn = cardObj.AddComponent<Button>();
            
            LayoutElement layoutEl = cardObj.AddComponent<LayoutElement>();
            layoutEl.preferredWidth = 220;
            layoutEl.preferredHeight = 350;

            // Name
            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(cardObj.transform, false);
            TextMeshProUGUI nameT = nameObj.AddComponent<TextMeshProUGUI>();
            nameT.alignment = TextAlignmentOptions.Top;
            nameT.color = Color.black;
            RectTransform nRt = nameT.GetComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0, 0.8f); nRt.anchorMax = new Vector2(1, 1);
            nRt.sizeDelta = Vector2.zero;

            // Hero
            GameObject heroObj = new GameObject("HeroText");
            heroObj.transform.SetParent(cardObj.transform, false);
            TextMeshProUGUI heroT = heroObj.AddComponent<TextMeshProUGUI>();
            heroT.alignment = TextAlignmentOptions.Top;
            heroT.color = Color.black;
            heroT.fontSize = 14;
            RectTransform hRt = heroT.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0, 0.7f); hRt.anchorMax = new Vector2(1, 0.8f);
            hRt.sizeDelta = Vector2.zero;

            // Description
            GameObject descObj = new GameObject("DescText");
            descObj.transform.SetParent(cardObj.transform, false);
            TextMeshProUGUI descT = descObj.AddComponent<TextMeshProUGUI>();
            descT.alignment = TextAlignmentOptions.Center;
            descT.color = Color.black;
            descT.fontSize = 16;
            RectTransform dRt = descT.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0.05f, 0.2f); dRt.anchorMax = new Vector2(0.95f, 0.6f);
            dRt.sizeDelta = Vector2.zero;

            // Rarity
            GameObject rarityObj = new GameObject("RarityText");
            rarityObj.transform.SetParent(cardObj.transform, false);
            TextMeshProUGUI rarityT = rarityObj.AddComponent<TextMeshProUGUI>();
            rarityT.alignment = TextAlignmentOptions.BottomLeft;
            rarityT.color = Color.black;
            rarityT.fontSize = 14;
            RectTransform rRt = rarityT.GetComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0.05f, 0.05f); rRt.anchorMax = new Vector2(0.5f, 0.15f);
            rRt.sizeDelta = Vector2.zero;

            // Stack
            GameObject stackObj = new GameObject("StackText");
            stackObj.transform.SetParent(cardObj.transform, false);
            TextMeshProUGUI stackT = stackObj.AddComponent<TextMeshProUGUI>();
            stackT.alignment = TextAlignmentOptions.BottomRight;
            stackT.color = Color.black;
            stackT.fontSize = 14;
            RectTransform sRt = stackT.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.5f, 0.05f); sRt.anchorMax = new Vector2(0.95f, 0.15f);
            sRt.sizeDelta = Vector2.zero;

            BlessingChoiceUI ui = cardObj.AddComponent<BlessingChoiceUI>();
            ui.ConfigureReferences(btn, frameImg, null, heroT, nameT, descT, rarityT, stackT);
            choiceCards[i] = ui;
        }

        return choiceRoot;
    }

    private static void CreateEventSystem()
    {
        if (GameObject.Find("EventSystem") == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    private static List<BlessingDefinition> CreateBlessingDefinitions()
    {
        List<BlessingDefinition> results = new List<BlessingDefinition>();
        string path = "Assets/Resources/Blessings";
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder("Assets/Resources", "Blessings");

        // 1. An Dương Vương (Phòng thủ)
        results.Add(CreateSO(path, "adv_giap", "Thành Giáp Âu Lạc", HeroType.AnDuongVuong, "Giảm 7.5% sát thương nhận vào mỗi bậc.", BlessingRarity.Common, 3, BlessingEffectType.Armor, false));
        results.Add(CreateSO(path, "adv_nothan", "Nỏ Thần", HeroType.AnDuongVuong, "Mỗi 5 đòn đánh, tự động bắn thêm 3 mũi tên ánh sáng vào kẻ địch xung quanh.", BlessingRarity.Rare, 3, BlessingEffectType.DivineCrossbow, false));
        results.Add(CreateSO(path, "adv_tuongthanh", "Tường Thành", HeroType.AnDuongVuong, "Dash tạo ra một kết giới. Kẻ địch bước vào sẽ bị đẩy lùi và làm chậm.", BlessingRarity.Epic, 2, BlessingEffectType.DashBarrier, false));
        results.Add(CreateSO(path, "adv_canhgioi", "Cảnh Giới", HeroType.AnDuongVuong, "Làm chậm tốc độ xuất hiện của quái vật mới. Tăng tầm nhìn phát hiện địch.", BlessingRarity.Common, 2, BlessingEffectType.Awareness, false));
        results.Add(CreateSO(path, "adv_ultimate", "Thành Cổ Loa (Tối thượng)", HeroType.AnDuongVuong, "Định kỳ mỗi 11s, nhận một lớp lá chắn bằng 18 + 5*(Bậc Giáp). Tăng 4.5% miễn thương vĩnh viễn.", BlessingRarity.Legendary, 1, BlessingEffectType.CoLoaCitadel, true));

        // 2. Trưng Trắc (Ý chí / Hồi phục)
        results.Add(CreateSO(path, "tt_hieutrieu", "Hiệu Triệu", HeroType.TrungTrac, "Máu càng thấp, sát thương gây ra càng cao (lên đến 32% mỗi bậc).", BlessingRarity.Common, 3, BlessingEffectType.LowHealthDamage, false));
        results.Add(CreateSO(path, "tt_cokhoinghia", "Cờ Khởi Nghĩa", HeroType.TrungTrac, "Tăng 11% tốc độ đánh mỗi bậc.", BlessingRarity.Common, 3, BlessingEffectType.AttackSpeed, false));
        results.Add(CreateSO(path, "tt_khnhgia_melinh", "Khởi Nghĩa Mê Linh", HeroType.TrungTrac, "Tiêu diệt quái vật hồi lại một phần năng lượng Dash và Máu.", BlessingRarity.Epic, 3, BlessingEffectType.KillSkillEnergy, false));
        results.Add(CreateSO(path, "tt_nuvuong", "Nữ Vương", HeroType.TrungTrac, "Nhận 1 mạng hồi sinh với 50% máu khi chết.", BlessingRarity.Rare, 1, BlessingEffectType.Revive, false));
        results.Add(CreateSO(path, "tt_ultimate", "Hai Bà Khởi Nghĩa (Tối thượng)", HeroType.TrungTrac, "Sát thương tăng theo số lượng địch xung quanh (tối đa 75%).", BlessingRarity.Legendary, 1, BlessingEffectType.Uprising, true));

        // 3. Trưng Nhị (Tốc độ / Dash)
        results.Add(CreateSO(path, "tn_kytuong", "Kỵ Tướng", HeroType.TrungNhi, "Tăng 8% tốc độ di chuyển mỗi bậc.", BlessingRarity.Common, 3, BlessingEffectType.MoveSpeed, false));
        results.Add(CreateSO(path, "tn_xungphong", "Xung Phong", HeroType.TrungNhi, "Dash xuyên qua kẻ địch gây sát thương mạnh.", BlessingRarity.Rare, 3, BlessingEffectType.DashDamage, false));
        results.Add(CreateSO(path, "tn_truykich", "Truy Kích", HeroType.TrungNhi, "Đòn đánh thường ĐẦU TIÊN sau khi Dash gây thêm 35% sát thương.", BlessingRarity.Epic, 2, BlessingEffectType.PostDashDamage, false));
        results.Add(CreateSO(path, "tn_bongchientruong", "Bóng Chiến Trường", HeroType.TrungNhi, "Dash để lại một phân thân thu hút sự chú ý của kẻ địch trong giây lát.", BlessingRarity.Rare, 3, BlessingEffectType.DashDecoy, false));
        results.Add(CreateSO(path, "tn_ultimate", "Voi Chiến (Tối thượng)", HeroType.TrungNhi, "Dash biến thành đòn hất tung diện rộng khổng lồ, gây choáng và sát thương lớn.", BlessingRarity.Legendary, 1, BlessingEffectType.WarElephant, true));

        // 4. Quang Trung (Sát thương / Bùng nổ)
        results.Add(CreateSO(path, "qt_dongda", "Hào Khí Đống Đa", HeroType.QuangTrung, "Tăng tỉ lệ và sát thương chí mạng.", BlessingRarity.Epic, 3, BlessingEffectType.CriticalPower, false));
        results.Add(CreateSO(path, "qt_thantoc", "Thần Tốc Bắc Tiến", HeroType.QuangTrung, "Giảm 10% thời gian hồi chiêu Dash mỗi bậc.", BlessingRarity.Common, 3, BlessingEffectType.DashCooldown, false));
        results.Add(CreateSO(path, "qt_thienloi", "Thiên Lôi Tây Sơn", HeroType.QuangTrung, "Đòn đánh chí mạng có tỉ lệ giật sét lan sang quái vật xung quanh.", BlessingRarity.Rare, 3, BlessingEffectType.CriticalLightning, false));
        results.Add(CreateSO(path, "qt_ultimate", "Xuân Kỷ Dậu (Tối thượng)", HeroType.QuangTrung, "Khi bắt đầu Wave và khi nhận Blessing này: Vào trạng thái Cuồng Chiến (Tốc đánh, Tốc chạy, Sát thương tăng vọt) trong 7.5s.", BlessingRarity.Legendary, 1, BlessingEffectType.KyDauFrenzy, true));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return results;
    }

    private static BlessingDefinition CreateSO(string path, string id, string bName, HeroType hero, string desc, BlessingRarity rarity, int stack, BlessingEffectType effect, bool isUltimate)
    {
        string assetPath = $"{path}/{id}.asset";
        BlessingDefinition so = AssetDatabase.LoadAssetAtPath<BlessingDefinition>(assetPath);
        
        if (so == null)
        {
            so = ScriptableObject.CreateInstance<BlessingDefinition>();
            AssetDatabase.CreateAsset(so, assetPath);
        }

        so.Configure(id, bName, hero, desc, rarity, stack, effect, isUltimate);
        return so;
    }
}
#endif
