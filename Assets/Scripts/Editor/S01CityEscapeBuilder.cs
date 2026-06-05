using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class S01CityEscapeBuilder
{
    private const string RootName = "S01_CityEscape_Generated";
    private const float FloorY = 0f;
    private const float BarrierHeight = 2.2f;

    private static Transform staticEnvironment;
    private static Transform routeBarriers;
    private static Transform waypointGuides;
    private static Transform interactiveObstacles;
    private static Transform dynamicZones;
    private static Transform chaseLaneTriggers;
    private static Transform collapseSequence;
    private static TMP_Text interactionText;
    private static S01WarningTextUI warningUI;

    public static Vector3[] GetChaseWaypointPositions()
    {
        return new[]
        {
            new Vector3(0f, 1f, -8f),
            new Vector3(0f, 1f, 12f),
            new Vector3(0f, 1f, 32f),
            new Vector3(0f, 1f, 45f),
            new Vector3(18f, 1f, 45f),
            new Vector3(36f, 1f, 45f),
            new Vector3(45f, 1f, 45f),
            new Vector3(45f, 1f, 66f),
            new Vector3(45f, 1f, 88f),
            new Vector3(45f, 1f, 100f),
            new Vector3(25f, 1f, 100f),
            new Vector3(5f, 1f, 100f),
            new Vector3(5f, 1f, 128f),
            new Vector3(5f, 1f, 155f),
            new Vector3(-18f, 1f, 155f),
            new Vector3(-40f, 1f, 155f),
            new Vector3(-40f, 1f, 185f),
            new Vector3(-40f, 1f, 215f),
            new Vector3(-15f, 1f, 215f),
            new Vector3(10f, 1f, 215f),
            new Vector3(10f, 1f, 240f),
            new Vector3(10f, 1f, 265f)
        };
    }

    [MenuItem("Tools/Dong Chay Anh Hung/Rebuild S01 City Escape Zigzag")]
    public static void BuildScene()
    {
        CleanupOldS01();

        GameObject root = new GameObject(RootName);
        staticEnvironment = CreateGroup(root.transform, "Static_Environment");
        routeBarriers = CreateGroup(root.transform, "Route_Barriers");
        waypointGuides = CreateGroup(root.transform, "Waypoint_Guides");
        interactiveObstacles = CreateGroup(root.transform, "Interactive_Obstacles");
        dynamicZones = CreateGroup(root.transform, "Dynamic_Zones");
        chaseLaneTriggers = CreateGroup(root.transform, "Chase_Lane_Triggers");
        collapseSequence = CreateGroup(root.transform, "Collapse_Sequence");

        Material roadMat = CreateMaterial("S01_Road", new Color32(42, 45, 50, 255));
        Material dirtMat = CreateMaterial("S01_Construction_Path", new Color32(112, 85, 52, 255));
        Material edgeMat = CreateMaterial("S01_Path_Edge", new Color32(62, 86, 54, 255));
        Material concreteMat = CreateMaterial("S01_Concrete", new Color32(92, 96, 100, 255));
        Material fenceMat = CreateMaterial("S01_Fence", new Color32(45, 48, 54, 255));
        Material warningMat = CreateMaterial("S01_Warning_Yellow", new Color32(242, 190, 42, 255));
        Material orangeMat = CreateMaterial("S01_Warning_Orange", new Color32(230, 92, 32, 255));
        Material mudMat = CreateMaterial("S01_Mud", new Color32(72, 49, 31, 255));
        Material woodMat = CreateMaterial("S01_Wood", new Color32(104, 70, 39, 255));
        Material bronzeMat = CreateMaterial("S01_Museum_Bronze", new Color32(166, 117, 48, 255));
        Material crackMat = CreateMaterial("S01_Crack", new Color32(12, 11, 12, 255));
        Material collapseMat = CreateMaterial("S01_Collapse", new Color32(130, 38, 42, 255));

        SetupUI();
        SetupPlayer();
        CreateSafetyFloor();
        BuildLongRoute(roadMat, dirtMat, fenceMat);
        BuildMuseumStart(concreteMat, bronzeMat, warningMat);
        BuildBlockedMainRoad(concreteMat, warningMat, orangeMat);
        BuildWheelbarrowDelayTrap(concreteMat, warningMat, woodMat);
        BuildConstructionFence(concreteMat, fenceMat, warningMat);
        BuildFallingDebrisArea(concreteMat, woodMat, orangeMat, crackMat);
        BuildMudZone(mudMat, warningMat);
        BuildNarrowPassage(concreteMat, fenceMat);
        BuildRouteGuidance(warningMat, orangeMat);
        BuildStoryTriggers();
        BuildCollapse(collapseMat, crackMat, warningMat);

        CreateEmpty(chaseLaneTriggers, "EnemySpawn_ChaseStart", new Vector3(0f, 1f, -8f));

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("S01 rebuilt from scratch with clean long route.");
    }

    private static void BuildLongRoute(Material roadMat, Material dirtMat, Material fenceMat)
    {
        RouteSegment[] segments =
        {
            new RouteSegment("MuseumStreet_Start", new Vector3(0f, 0f, -6f), new Vector3(0f, 0f, 45f), 14f, roadMat),
            new RouteSegment("ConstructionDetour_East", new Vector3(0f, 0f, 45f), new Vector3(45f, 0f, 45f), 10f, dirtMat),
            new RouteSegment("ConstructionRun_North", new Vector3(45f, 0f, 45f), new Vector3(45f, 0f, 100f), 8f, dirtMat),
            new RouteSegment("DebrisRun_West", new Vector3(45f, 0f, 100f), new Vector3(5f, 0f, 100f), 8f, dirtMat),
            new RouteSegment("MudRun_North", new Vector3(5f, 0f, 100f), new Vector3(5f, 0f, 155f), 8f, dirtMat),
            new RouteSegment("NarrowRun_West", new Vector3(5f, 0f, 155f), new Vector3(-40f, 0f, 155f), 8f, dirtMat),
            new RouteSegment("LongEscape_North", new Vector3(-40f, 0f, 155f), new Vector3(-40f, 0f, 215f), 8f, dirtMat),
            new RouteSegment("LongEscape_East", new Vector3(-40f, 0f, 215f), new Vector3(10f, 0f, 215f), 8f, dirtMat),
            new RouteSegment("CollapseApproach_North", new Vector3(10f, 0f, 215f), new Vector3(10f, 0f, 265f), 8f, dirtMat)
        };

        foreach (RouteSegment segment in segments)
            CreateRouteSegment(segment, fenceMat);

        Vector3[] corners =
        {
            new Vector3(0f, FloorY, 45f),
            new Vector3(45f, FloorY, 45f),
            new Vector3(45f, FloorY, 100f),
            new Vector3(5f, FloorY, 100f),
            new Vector3(5f, FloorY, 155f),
            new Vector3(-40f, FloorY, 155f),
            new Vector3(-40f, FloorY, 215f),
            new Vector3(10f, FloorY, 215f)
        };

        for (int i = 0; i < corners.Length; i++)
            CreateCube(staticEnvironment, "RouteCorner_" + (i + 1).ToString("00"), corners[i], Vector3.zero, new Vector3(12f, 0.32f, 12f), dirtMat);
    }

    private static void BuildMuseumStart(Material concreteMat, Material bronzeMat, Material warningMat)
    {
        GameObject museum = CreateParent(staticEnvironment, "Museum_Facade", new Vector3(-15f, 0f, 10f), Vector3.zero);
        CreateChildCube(museum.transform, "Museum_Block", new Vector3(0f, 4f, 0f), new Vector3(12f, 8f, 8f), concreteMat);
        CreateChildCube(museum.transform, "Museum_Entrance", new Vector3(6.2f, 2f, 0f), new Vector3(0.35f, 4f, 4f), bronzeMat);

        CreateStreetLamp(new Vector3(9f, 0f, 8f), warningMat);
        CreateStreetLamp(new Vector3(-9f, 0f, 26f), warningMat);
        CreateStreetLamp(new Vector3(9f, 0f, 39f), warningMat);
    }

    private static void BuildBlockedMainRoad(Material concreteMat, Material warningMat, Material orangeMat)
    {
        GameObject truck = CreateParent(staticEnvironment, "ConstructionTruck_BlockingMainRoad", new Vector3(0f, 0f, 55f), Vector3.zero);
        CreateChildCube(truck.transform, "Truck_Body", new Vector3(0f, 1.2f, 0f), new Vector3(12f, 2.4f, 4f), concreteMat);
        CreateChildCube(truck.transform, "Truck_Cabin", new Vector3(4.2f, 1.7f, 0f), new Vector3(3.2f, 3f, 3.8f), warningMat);
        CreateConcreteBlock(new Vector3(-6f, 0.5f, 50f), concreteMat);
        CreateConcreteBlock(new Vector3(6f, 0.5f, 50f), concreteMat);
        CreateConeRow(new Vector3(4f, 0f, 40f), Vector3.right, 5, orangeMat);
    }

    private static void BuildWheelbarrowDelayTrap(Material concreteMat, Material warningMat, Material woodMat)
    {
        GameObject delayObstacle = CreateParent(chaseLaneTriggers, "HacTinhBreakableDelayObstacle", new Vector3(45f, 0f, 65f), Vector3.zero);
        GameObject scatterRoot = CreateParent(delayObstacle.transform, "DelayScatterPieces", Vector3.zero, Vector3.zero);
        RemoveCollider(CreateChildPrimitive(scatterRoot.transform, "Broken_Frame", PrimitiveType.Cube, new Vector3(0f, 0.5f, 0f), Vector3.zero, new Vector3(2.5f, 0.3f, 1.1f), concreteMat));
        RemoveCollider(CreateChildPrimitive(scatterRoot.transform, "Brick_01", PrimitiveType.Cube, new Vector3(-1f, 0.3f, -0.8f), Vector3.zero, new Vector3(0.5f, 0.4f, 0.5f), warningMat));
        RemoveCollider(CreateChildPrimitive(scatterRoot.transform, "Brick_02", PrimitiveType.Cube, new Vector3(0f, 0.3f, -1f), Vector3.zero, new Vector3(0.5f, 0.4f, 0.5f), warningMat));
        RemoveCollider(CreateChildPrimitive(scatterRoot.transform, "Brick_03", PrimitiveType.Cube, new Vector3(1f, 0.3f, -0.8f), Vector3.zero, new Vector3(0.5f, 0.4f, 0.5f), warningMat));
        BoxCollider delayCollider = delayObstacle.AddComponent<BoxCollider>();
        delayCollider.center = new Vector3(0f, 1f, 0f);
        delayCollider.size = new Vector3(7f, 2f, 6f);
        delayCollider.isTrigger = true;
        Rigidbody body = delayObstacle.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        AddBreakableComponent(delayObstacle, scatterRoot.transform, delayCollider);
        delayObstacle.SetActive(false);

        GameObject wheelbarrow = CreateParent(interactiveObstacles, "QTE_Wheelbarrow_DelayTrap", new Vector3(41.5f, 0f, 70f), Vector3.zero);
        CreateChildCube(wheelbarrow.transform, "Bucket", new Vector3(0f, 0.55f, 0f), new Vector3(1.2f, 0.6f, 0.65f), concreteMat);
        CreateChildCube(wheelbarrow.transform, "Handles", new Vector3(-0.85f, 0.55f, 0f), new Vector3(0.9f, 0.12f, 0.55f), woodMat);
        CreateChildPrimitive(wheelbarrow.transform, "Wheel", PrimitiveType.Cylinder, new Vector3(0.75f, 0.25f, 0f), new Vector3(90f, 0f, 0f), new Vector3(0.28f, 0.16f, 0.28f), warningMat);

        CreateHoldTrigger(
            "Wheelbarrow_HoldTrigger",
            wheelbarrow.transform,
            new Vector3(0f, 1f, 0f),
            new Vector3(4f, 2.5f, 4f),
            "Giữ E để lật xe rùa cản Hắc Tinh",
            "Đang lật xe rùa: X%",
            1f,
            wheelbarrow.transform,
            new Vector3(3.5f, 0f, -2f),
            new Vector3(-70f, 20f, 20f),
            null,
            delayObstacle,
            "Cản được nó một chút thôi! Chạy tiếp!",
            "",
            "Wheelbarrow delay trap activated.");
    }

    private static void BuildConstructionFence(Material concreteMat, Material fenceMat, Material warningMat)
    {
        CreateConcreteBlock(new Vector3(42f, 1f, 91f), concreteMat);
        CreateConcreteBlock(new Vector3(48f, 1f, 91f), concreteMat);

        GameObject fence = CreateParent(interactiveObstacles, "QTE_ConstructionFence", new Vector3(45f, 0f, 91f), Vector3.zero);
        CreateChildCube(fence.transform, "Fence_Panel", new Vector3(0f, 1.1f, 0f), new Vector3(3.8f, 2.2f, 0.2f), fenceMat);
        CreateChildCube(fence.transform, "Warning_Rail", new Vector3(0f, 1.1f, -0.14f), new Vector3(3.4f, 0.25f, 0.08f), warningMat);
        BoxCollider blockingCollider = fence.AddComponent<BoxCollider>();
        blockingCollider.center = new Vector3(0f, 1.1f, 0f);
        blockingCollider.size = new Vector3(4f, 2.2f, 0.45f);

        CreateHoldTrigger(
            "ConstructionFence_HoldTrigger",
            fence.transform,
            new Vector3(0f, 1.2f, -2.4f),
            new Vector3(5f, 2.8f, 4f),
            "Giữ E để kéo rào chắn",
            "Đang kéo rào chắn: X%",
            1.8f,
            fence.transform,
            new Vector3(4.5f, 0f, 0f),
            new Vector3(0f, 72f, 0f),
            blockingCollider,
            null,
            "Lối đi đã mở! Chạy tiếp!",
            "Construction fence hold prompt shown.",
            "Construction fence opened; path clear.");
    }

    private static void BuildFallingDebrisArea(Material concreteMat, Material woodMat, Material orangeMat, Material crackMat)
    {
        CreateDebrisWarningZone("FallingDebris_WarningZone_01", new Vector3(34f, 0f, 100f), "Coi chừng! Vật liệu đang rơi!", concreteMat, woodMat, orangeMat, crackMat);
        CreateDebrisWarningZone("FallingDebris_WarningZone_02", new Vector3(20f, 0f, 100f), "Tránh khu vực có đá vụn phía trước!", concreteMat, woodMat, orangeMat, crackMat);
        CreateDebrisWarningZone("FallingDebris_WarningZone_03", new Vector3(-40f, 0f, 185f), "Coi chừng! Vật liệu đang rơi!", concreteMat, woodMat, orangeMat, crackMat);
    }

    private static void BuildMudZone(Material mudMat, Material warningMat)
    {
        GameObject mud = CreateCube(dynamicZones, "SlowZone_Mud", new Vector3(3.6f, 0.22f, 130f), Vector3.zero, new Vector3(5.2f, 0.12f, 16f), mudMat);
        BoxCollider collider = mud.GetComponent<BoxCollider>();
        collider.isTrigger = true;
        SlowZone slowZone = mud.AddComponent<SlowZone>();
        slowZone.slowMoveSpeed = 1.5f;
        slowZone.slowRunSpeed = 2.2f;

        CreateWarningTrigger("WarningTrigger_SlowZone", new Vector3(5f, 1.2f, 119f), new Vector3(8f, 3f, 5f), "Bùn lầy làm bạn di chuyển chậm lại!", false);
        CreateGuideBeacon(new Vector3(8.3f, 0f, 123f), warningMat);
        CreateGuideBeacon(new Vector3(8.3f, 0f, 138f), warningMat);
    }

    private static void BuildNarrowPassage(Material concreteMat, Material fenceMat)
    {
        CreateCube(routeBarriers, "NarrowPassage_SouthWall", new Vector3(-18f, 1.2f, 152.4f), Vector3.zero, new Vector3(18f, 2.4f, 2.8f), concreteMat);
        CreateCube(routeBarriers, "NarrowPassage_NorthWall", new Vector3(-18f, 1.2f, 157.6f), Vector3.zero, new Vector3(18f, 2.4f, 2.8f), concreteMat);
        CreateWarningTrigger("NarrowPassage_ConstructionGap", new Vector3(-5f, 1.2f, 155f), new Vector3(8f, 3f, 5f), "Lách qua khe hẹp phía trước!", false);
        CreateGuideBeacon(new Vector3(-8f, 0f, 155f), fenceMat);
        CreateGuideBeacon(new Vector3(-28f, 0f, 155f), fenceMat);
    }

    private static void BuildRouteGuidance(Material warningMat, Material orangeMat)
    {
        Vector3[] positions =
        {
            new Vector3(0f, 0.38f, 28f),
            new Vector3(0f, 0.38f, 43f),
            new Vector3(20f, 0.38f, 45f),
            new Vector3(43f, 0.38f, 45f),
            new Vector3(45f, 0.38f, 74f),
            new Vector3(45f, 0.38f, 98f),
            new Vector3(28f, 0.38f, 100f),
            new Vector3(7f, 0.38f, 100f),
            new Vector3(5f, 0.38f, 142f),
            new Vector3(-8f, 0.38f, 155f),
            new Vector3(-38f, 0.38f, 155f),
            new Vector3(-40f, 0.38f, 195f),
            new Vector3(-38f, 0.38f, 215f),
            new Vector3(-10f, 0.38f, 215f),
            new Vector3(8f, 0.38f, 215f),
            new Vector3(10f, 0.38f, 245f)
        };

        float[] yaws = { 0f, 90f, 90f, 0f, 0f, 0f, -90f, -90f, 0f, -90f, 0f, 0f, 90f, 90f, 0f, 0f };

        for (int i = 0; i < positions.Length; i++)
            CreateRouteGuide("RouteGuide_" + (i + 1).ToString("00"), positions[i], yaws[i], warningMat);

        CreateGuideBeacon(new Vector3(8f, 0f, 45f), orangeMat);
        CreateGuideBeacon(new Vector3(45f, 0f, 48f), orangeMat);
        CreateGuideBeacon(new Vector3(42f, 0f, 100f), orangeMat);
        CreateGuideBeacon(new Vector3(8f, 0f, 103f), orangeMat);
        CreateGuideBeacon(new Vector3(2f, 0f, 155f), orangeMat);
        CreateGuideBeacon(new Vector3(-40f, 0f, 160f), orangeMat);
        CreateGuideBeacon(new Vector3(-36f, 0f, 215f), orangeMat);
        CreateGuideBeacon(new Vector3(10f, 0f, 220f), orangeMat);
    }

    private static void BuildStoryTriggers()
    {
        CreateWarningTrigger("TutorialTrigger_Controls", new Vector3(0f, 1.2f, 6f), new Vector3(14f, 3f, 5f), "WASD để di chuyển. Giữ Shift để chạy.", false);
        CreateWarningTrigger("WarningTrigger_ChaseStart", new Vector3(0f, 1.2f, 24f), new Vector3(14f, 3f, 5f), "Chạy! Đừng để Hắc Tinh chạm vào bạn.", false);
        CreateWarningTrigger("WarningTrigger_Detour", new Vector3(0f, 1.2f, 40f), new Vector3(14f, 3f, 5f), "Đường chính bị chặn rồi! Rẽ vào công trường!", false);
        CreateWarningTrigger("WarningTrigger_Fence", new Vector3(45f, 1.2f, 84f), new Vector3(8f, 3f, 5f), "Giữ E để kéo rào chắn mở đường!", false);
        CreateWarningTrigger("WarningTrigger_LongEscape", new Vector3(-40f, 1.2f, 170f), new Vector3(8f, 3f, 6f), "Đừng dừng lại! Tiếp tục theo đèn vàng!", false);
    }

    private static void BuildCollapse(Material collapseMat, Material crackMat, Material warningMat)
    {
        CreateCube(collapseSequence, "Collapse_Zone", new Vector3(10f, 0.18f, 260f), Vector3.zero, new Vector3(14f, 0.16f, 16f), collapseMat);
        CreateCube(collapseSequence, "Collapse_Crack_Mark", new Vector3(10f, 0.31f, 260f), Vector3.zero, new Vector3(9f, 0.05f, 10f), crackMat);
        CreateGuideBeacon(new Vector3(6f, 0f, 252f), warningMat);
        CreateGuideBeacon(new Vector3(14f, 0f, 252f), warningMat);
        CreateWarningTrigger("StoryTrigger_Collapse", new Vector3(10f, 1.2f, 250f), new Vector3(8f, 3f, 6f), "Mặt đất đang nứt ra!", true);
        CreateWarningTrigger("WarningTrigger_Final", new Vector3(10f, 1.2f, 258f), new Vector3(8f, 3f, 5f), "Không phải nó đuổi theo... nó đang lùa chúng ta tới đây!", false);

        GameObject exit = CreateCube(collapseSequence, "ExitTrigger_Test", new Vector3(10f, 1.5f, 264f), Vector3.zero, new Vector3(8f, 3f, 4f), null);
        BoxCollider collider = exit.GetComponent<BoxCollider>();
        collider.isTrigger = true;

        if (IsSceneInBuildSettings("S02_UndergroundCave"))
        {
            SceneTransitionTrigger transition = exit.AddComponent<SceneTransitionTrigger>();
            transition.nextSceneName = "S02_UndergroundCave";
            transition.delayBeforeLoad = 1.5f;
        }
        else
        {
            Debug.LogWarning("S02_UndergroundCave is not in Build Settings. ExitTrigger_Test was created without scene loading.");
        }
    }

    private static void CreateRouteSegment(RouteSegment segment, Material barrierMat)
    {
        Vector3 delta = segment.end - segment.start;
        bool alongZ = Mathf.Abs(delta.z) >= Mathf.Abs(delta.x);
        float length = alongZ ? Mathf.Abs(delta.z) : Mathf.Abs(delta.x);
        Vector3 center = (segment.start + segment.end) * 0.5f;
        center.y = FloorY;

        Vector3 floorScale = alongZ
            ? new Vector3(segment.width, 0.32f, length + 1f)
            : new Vector3(length + 1f, 0.32f, segment.width);
        CreateCube(staticEnvironment, segment.name + "_Floor", center, Vector3.zero, floorScale, segment.material);

        float barrierLength = Mathf.Max(1f, length - 10f);
        Vector3 barrierScale = alongZ
            ? new Vector3(0.45f, BarrierHeight, barrierLength)
            : new Vector3(barrierLength, BarrierHeight, 0.45f);
        Vector3 side = alongZ ? Vector3.right : Vector3.forward;
        float offset = segment.width * 0.5f + 0.25f;

        CreateCube(routeBarriers, segment.name + "_Barrier_A", center + side * offset + Vector3.up * (BarrierHeight * 0.5f), Vector3.zero, barrierScale, barrierMat);
        CreateCube(routeBarriers, segment.name + "_Barrier_B", center - side * offset + Vector3.up * (BarrierHeight * 0.5f), Vector3.zero, barrierScale, barrierMat);
    }

    private static void CreateDebrisWarningZone(string name, Vector3 position, string message, Material concreteMat, Material woodMat, Material orangeMat, Material crackMat)
    {
        GameObject marker = CreateCube(dynamicZones, name + "_GroundMarker", position + Vector3.up * 0.24f, Vector3.zero, new Vector3(5f, 0.05f, 5f), orangeMat);
        RemoveCollider(marker);
        RemoveCollider(CreatePrimitive(dynamicZones, name + "_Rubble_A", PrimitiveType.Sphere, position + new Vector3(-2.2f, 0.35f, 1.8f), Vector3.zero, new Vector3(1f, 0.6f, 0.8f), concreteMat));
        RemoveCollider(CreatePrimitive(dynamicZones, name + "_Rubble_B", PrimitiveType.Sphere, position + new Vector3(2.1f, 0.35f, -1.7f), Vector3.zero, new Vector3(0.9f, 0.55f, 0.9f), concreteMat));
        RemoveCollider(CreateCube(dynamicZones, name + "_TiltedBeam", position + new Vector3(2.8f, 1.1f, 1.8f), new Vector3(0f, 18f, 55f), new Vector3(0.25f, 2.4f, 0.25f), woodMat));
        RemoveCollider(CreateCube(dynamicZones, name + "_Crack", position + new Vector3(0f, 0.28f, 0f), new Vector3(0f, 28f, 0f), new Vector3(0.15f, 0.04f, 4f), crackMat));
        CreateWarningTrigger(name, position + Vector3.up * 1.2f, new Vector3(7f, 3f, 7f), message, false);
    }

    private static void CreateHoldTrigger(string name, Transform parent, Vector3 localPosition, Vector3 size, string prompt, string progress, float duration, Transform target, Vector3 moveOffset, Vector3 rotationOffset, Collider colliderToDisable, GameObject activateOnComplete, string completionMessage, string promptLog, string completeLog)
    {
        GameObject trigger = new GameObject(name);
        trigger.transform.SetParent(parent, false);
        trigger.transform.localPosition = localPosition;
        BoxCollider triggerCollider = trigger.AddComponent<BoxCollider>();
        triggerCollider.size = size;
        triggerCollider.isTrigger = true;

        Type holdType = Type.GetType("HoldInteractionPrompt, Assembly-CSharp");
        if (holdType == null)
        {
            Debug.LogWarning("HoldInteractionPrompt is not compiled yet. Let Unity compile scripts, then rebuild S01.");
            return;
        }

        Component hold = trigger.AddComponent(holdType);
        SetField(holdType, hold, "interactionText", interactionText);
        SetField(holdType, hold, "warningUI", warningUI);
        SetField(holdType, hold, "promptText", prompt);
        SetField(holdType, hold, "progressText", progress);
        SetField(holdType, hold, "holdDuration", duration);
        SetField(holdType, hold, "targetTransform", target);
        SetField(holdType, hold, "completedLocalMoveOffset", moveOffset);
        SetField(holdType, hold, "completedLocalRotationOffset", rotationOffset);
        SetField(holdType, hold, "colliderToDisable", colliderToDisable);
        SetField(holdType, hold, "activateOnComplete", activateOnComplete);
        SetField(holdType, hold, "completionMessage", completionMessage);
        SetField(holdType, hold, "promptShownLogMessage", promptLog);
        SetField(holdType, hold, "completedLogMessage", completeLog);
    }

    private static void AddBreakableComponent(GameObject obstacle, Transform scatterRoot, Collider delayCollider)
    {
        Type breakableType = Type.GetType("BreakableChaseObstacle, Assembly-CSharp");
        if (breakableType == null)
        {
            Debug.LogWarning("BreakableChaseObstacle is not compiled yet. Let Unity compile scripts, then rebuild S01.");
            return;
        }

        Component breakable = obstacle.AddComponent(breakableType);
        SetField(breakableType, breakable, "scatterRoot", scatterRoot);
        SetField(breakableType, breakable, "delayCollider", delayCollider);
        SetField(breakableType, breakable, "slowDuration", 2.5f);
        SetField(breakableType, breakable, "slowMultiplier", 0.04f);
    }

    private static void SetField(Type type, Component component, string fieldName, object value)
    {
        System.Reflection.FieldInfo field = type.GetField(fieldName);
        if (field != null)
            field.SetValue(component, value);
    }

    private static void CreateWarningTrigger(string name, Vector3 position, Vector3 size, string message, bool story)
    {
        GameObject trigger = CreateCube(dynamicZones, name, position, Vector3.zero, size, null);
        BoxCollider collider = trigger.GetComponent<BoxCollider>();
        collider.isTrigger = true;
        S01WarningTrigger warningTrigger = trigger.AddComponent<S01WarningTrigger>();
        warningTrigger.warningUI = warningUI;
        warningTrigger.message = message;
        warningTrigger.showAsStory = story;
        warningTrigger.duration = story ? 6f : 5f;
    }

    private static void CreateRouteGuide(string name, Vector3 position, float yaw, Material material)
    {
        GameObject guide = CreateParent(waypointGuides, name, position, new Vector3(0f, yaw, 0f));
        RemoveCollider(CreateChildPrimitive(guide.transform, "Stem", PrimitiveType.Cube, Vector3.zero, Vector3.zero, new Vector3(0.8f, 0.08f, 3.2f), material));
        RemoveCollider(CreateChildPrimitive(guide.transform, "Head", PrimitiveType.Cube, new Vector3(0f, 0f, 1.9f), new Vector3(0f, 45f, 0f), new Vector3(1.4f, 0.08f, 1.4f), material));
    }

    private static void CreateGuideBeacon(Vector3 position, Material material)
    {
        GameObject beacon = CreateParent(waypointGuides, "GuideBeacon_" + Mathf.RoundToInt(position.x) + "_" + Mathf.RoundToInt(position.z), position, Vector3.zero);
        RemoveCollider(CreateChildPrimitive(beacon.transform, "Base", PrimitiveType.Cylinder, new Vector3(0f, 0.25f, 0f), Vector3.zero, new Vector3(0.45f, 0.5f, 0.45f), material));
        RemoveCollider(CreateChildPrimitive(beacon.transform, "Pole", PrimitiveType.Cube, new Vector3(0f, 1.05f, 0f), Vector3.zero, new Vector3(0.14f, 1.6f, 0.14f), material));
        RemoveCollider(CreateChildPrimitive(beacon.transform, "Lamp", PrimitiveType.Sphere, new Vector3(0f, 1.95f, 0f), Vector3.zero, new Vector3(0.45f, 0.45f, 0.45f), material));
        GameObject lightObject = new GameObject("GuideLight");
        lightObject.transform.SetParent(beacon.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 1.9f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.82f, 0.25f);
        light.intensity = 1.5f;
        light.range = 7f;
    }

    private static void CreateStreetLamp(Vector3 position, Material material)
    {
        GameObject lamp = CreateParent(staticEnvironment, "StreetLamp_" + Mathf.RoundToInt(position.z), position, Vector3.zero);
        CreateChildCube(lamp.transform, "Pole", new Vector3(0f, 2f, 0f), new Vector3(0.18f, 4f, 0.18f), material);
        RemoveCollider(CreateChildPrimitive(lamp.transform, "Lamp", PrimitiveType.Sphere, new Vector3(0f, 4.1f, 0f), Vector3.zero, new Vector3(0.45f, 0.45f, 0.45f), material));
        GameObject lightObject = new GameObject("PointLight");
        lightObject.transform.SetParent(lamp.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 4f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.82f, 0.45f);
        light.intensity = 1.4f;
        light.range = 10f;
    }

    private static void CreateConcreteBlock(Vector3 position, Material material)
    {
        CreateCube(routeBarriers, "ConcreteBlock_" + Mathf.RoundToInt(position.x) + "_" + Mathf.RoundToInt(position.z), position, Vector3.zero, new Vector3(2f, 2f, 1f), material);
    }

    private static void CreateConeRow(Vector3 start, Vector3 direction, int count, Material material)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject cone = CreatePrimitive(staticEnvironment, "Cone_" + i + "_" + Mathf.RoundToInt(start.z), PrimitiveType.Cylinder, start + direction * (i * 1.5f) + Vector3.up * 0.35f, Vector3.zero, new Vector3(0.35f, 0.7f, 0.35f), material);
            RemoveCollider(cone);
        }
    }

    private static void SetupPlayer()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
            return;

        CharacterController characterController = player.GetComponent<CharacterController>();
        bool wasEnabled = characterController != null && characterController.enabled;
        if (wasEnabled)
            characterController.enabled = false;

        player.transform.position = new Vector3(0f, 2f, 0f);
        player.transform.rotation = Quaternion.identity;
        player.tag = "Player";

        if (wasEnabled)
            characterController.enabled = true;

        PlayerCombat3D combat = player.GetComponent<PlayerCombat3D>();
        if (combat != null)
            combat.enabled = false;

        PlayerController3D controller = player.GetComponent<PlayerController3D>();
        if (controller != null)
        {
            controller.moveSpeed = 5f;
            controller.runSpeed = 8f;
        }
    }

    private static void SetupUI()
    {
        Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
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

        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        interactionText = EnsureUniqueText(canvas.transform, "InteractionText", new Vector2(0.5f, 0f), new Vector2(0f, 100f), 28);
        TMP_Text warningText = EnsureUniqueText(canvas.transform, "WarningText", new Vector2(0.5f, 1f), new Vector2(0f, -120f), 30);
        TMP_Text storyText = EnsureUniqueText(canvas.transform, "StoryText", new Vector2(0.5f, 1f), new Vector2(0f, -70f), 26);

        S01WarningTextUI[] warningUis = Resources.FindObjectsOfTypeAll<S01WarningTextUI>();
        warningUI = canvas.GetComponent<S01WarningTextUI>();
        if (warningUI == null)
            warningUI = canvas.gameObject.AddComponent<S01WarningTextUI>();

        foreach (S01WarningTextUI other in warningUis)
        {
            if (other != null && other != warningUI && other.gameObject.scene.IsValid())
                UnityEngine.Object.DestroyImmediate(other);
        }

        warningUI.warningText = warningText;
        warningUI.storyText = storyText;
        warningUI.defaultDuration = 5f;
        interactionText.gameObject.SetActive(false);
        warningText.gameObject.SetActive(false);
        storyText.gameObject.SetActive(false);
    }

    private static TMP_Text EnsureUniqueText(Transform canvas, string name, Vector2 anchor, Vector2 anchoredPosition, int fontSize)
    {
        TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
        TMP_Text keep = null;

        foreach (TMP_Text text in texts)
        {
            if (text.name != name || !text.gameObject.scene.IsValid())
                continue;

            if (keep == null)
                keep = text;
            else
                UnityEngine.Object.DestroyImmediate(text.gameObject);
        }

        if (keep == null)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(canvas, false);
            keep = textObject.AddComponent<TextMeshProUGUI>();
        }
        else
        {
            keep.transform.SetParent(canvas, false);
        }

        keep.fontSize = fontSize;
        keep.alignment = TextAlignmentOptions.Center;
        keep.color = Color.white;
        keep.raycastTarget = false;
        keep.text = string.Empty;

        RectTransform rect = keep.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(1100f, 90f);
        return keep;
    }

    private static void CreateSafetyFloor()
    {
        GameObject floor = CreateCube(staticEnvironment, "Safety_Floor_S01", new Vector3(0f, -1f, 130f), Vector3.zero, new Vector3(180f, 0.2f, 330f), null);
        Renderer renderer = floor.GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = false;
    }

    private static Transform CreateGroup(Transform parent, string name)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    private static GameObject CreateEmpty(Transform parent, string name, Vector3 position)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.position = position;
        return obj;
    }

    private static GameObject CreateParent(Transform parent, string name, Vector3 position, Vector3 rotation)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.position = position;
        obj.transform.eulerAngles = rotation;
        return obj;
    }

    private static GameObject CreateCube(Transform parent, string name, Vector3 position, Vector3 rotation, Vector3 scale, Material material)
    {
        return CreatePrimitive(parent, name, PrimitiveType.Cube, position, rotation, scale, material);
    }

    private static GameObject CreatePrimitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 rotation, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.position = position;
        obj.transform.eulerAngles = rotation;
        obj.transform.localScale = scale;
        SetMaterial(obj, material);
        return obj;
    }

    private static void CreateChildCube(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material)
    {
        CreateChildPrimitive(parent, name, PrimitiveType.Cube, localPosition, Vector3.zero, scale, material);
    }

    private static GameObject CreateChildPrimitive(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localRotation, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localEulerAngles = localRotation;
        obj.transform.localScale = scale;
        SetMaterial(obj, material);
        return obj;
    }

    private static void SetMaterial(GameObject obj, Material material)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null)
            return;

        if (material == null)
            renderer.enabled = false;
        else
            renderer.sharedMaterial = material;
    }

    private static void RemoveCollider(GameObject obj)
    {
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null)
            UnityEngine.Object.DestroyImmediate(collider);
    }

    private static Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader)
        {
            name = name,
            color = color
        };
        return material;
    }

    private static bool IsSceneInBuildSettings(string sceneName)
    {
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled)
                continue;

            string pathWithoutExtension = System.IO.Path.ChangeExtension(scene.path, null);
            if (System.IO.Path.GetFileName(pathWithoutExtension) == sceneName)
                return true;
        }

        return false;
    }

    private static void CleanupOldS01()
    {
        string[] legacyNames =
        {
            RootName,
            "Road", "Road_01", "Road_02", "Road_03", "Road_04", "Road_05",
            "MetalGate_01", "MetalGate_02",
            "QTE_ConstructionFence", "QTE_Wheelbarrow_Block", "QTE_Wheelbarrow_DelayTrap", "QTE_FallenTree", "QTE_BrokenFence",
            "HacTinhBreakableDelayObstacle",
            "SlowZone_Electric", "SlowZone_Mud", "SlowZone_Debris",
            "ExitTrigger_Test", "Collapse_Crack_Mark", "Collapse_Zone", "Safety_Floor_S01",
            "S01_ChaseThreat", "S01_ChaseWaypoints", "EnemySpawn_ChaseStart", "S01_EventController",
            "TutorialTrigger_Controls", "StoryTrigger_Signal", "WarningTrigger_ChaseStart", "WarningTrigger_Detour",
            "WarningTrigger_SlowZone", "StoryTrigger_Collapse", "WarningTrigger_Final"
        };

        foreach (string name in legacyNames)
            DeleteSceneObject(name);

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform sceneTransform in transforms)
        {
            if (!sceneTransform.gameObject.scene.IsValid())
                continue;

            string name = sceneTransform.name;
            if (name.StartsWith("RouteGuide_") ||
                name.StartsWith("GuideBeacon_") ||
                name.StartsWith("FallingDebris_") ||
                name.StartsWith("RouteHint_"))
            {
                UnityEngine.Object.DestroyImmediate(sceneTransform.gameObject);
            }
        }
    }

    private static void DeleteSceneObject(string name)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform sceneTransform in transforms)
        {
            if (sceneTransform.name == name && sceneTransform.gameObject.scene.IsValid())
            {
                UnityEngine.Object.DestroyImmediate(sceneTransform.gameObject);
                return;
            }
        }
    }

    private readonly struct RouteSegment
    {
        public readonly string name;
        public readonly Vector3 start;
        public readonly Vector3 end;
        public readonly float width;
        public readonly Material material;

        public RouteSegment(string name, Vector3 start, Vector3 end, float width, Material material)
        {
            this.name = name;
            this.start = start;
            this.end = end;
            this.width = width;
            this.material = material;
        }
    }
}
