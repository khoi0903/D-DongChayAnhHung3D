using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class EnemyNpcAnimatorSetupBuilder
{
    private const string EnemyModelPath = "Assets/Models/Enemy/ennemy.fbx";
    private const string WalkingAnimationPath = "Assets/Animations/Enemy/Walking.fbx";
    private const string WalkingTurnAnimationPath = "Assets/Animations/Enemy/Walking Left Turn.fbx";
    private const string ControllerPath = "Assets/Animations/Enemy/Enemy_NPC.controller";
    private const string BlackStarEnemyPrefabPath = "Assets/Prefabs/BlackStarEnemy.prefab";

    [MenuItem("Tools/Dong Chay Anh Hung/Setup Enemy NPC Animator")]
    public static void SetupEnemyNpcAnimator()
    {
        Avatar enemyAvatar = EnsureEnemyAvatar();
        ConfigureAnimationImporter(WalkingAnimationPath, enemyAvatar);
        ConfigureAnimationImporter(WalkingTurnAnimationPath, enemyAvatar);

        AssetDatabase.Refresh();

        AnimationClip walkingClip = FindAnimationClip(WalkingAnimationPath);
        AnimationClip turningClip = FindAnimationClip(WalkingTurnAnimationPath);

        if (walkingClip == null)
        {
            Debug.LogError("EnemyNpcAnimatorSetupBuilder: Walking animation clip not found at " + WalkingAnimationPath);
            return;
        }

        AnimatorController controller = CreateOrResetController(walkingClip, turningClip);
        SetupBlackStarEnemyPrefab(controller, enemyAvatar);
        CleanupExtraSceneAnimators(controller);
        int assignedCount = AssignControllerToSceneEnemies(controller, enemyAvatar);
        WriteReport(controller, enemyAvatar, assignedCount, walkingClip, turningClip);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("EnemyNpcAnimatorSetupBuilder: Enemy_NPC animator setup completed.");
    }

    private static Avatar EnsureEnemyAvatar()
    {
        ModelImporter enemyImporter = AssetImporter.GetAtPath(EnemyModelPath) as ModelImporter;
        if (enemyImporter == null)
        {
            Debug.LogError("EnemyNpcAnimatorSetupBuilder: enemy model not found at " + EnemyModelPath);
            return null;
        }

        bool changed = false;
        if (enemyImporter.animationType != ModelImporterAnimationType.Human)
        {
            enemyImporter.animationType = ModelImporterAnimationType.Human;
            changed = true;
        }

        if (enemyImporter.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
        {
            enemyImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            changed = true;
        }

        if (changed)
            enemyImporter.SaveAndReimport();

        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(EnemyModelPath).OfType<Avatar>().FirstOrDefault();
        if (avatar == null)
            Debug.LogWarning("EnemyNpcAnimatorSetupBuilder: no Avatar found inside " + EnemyModelPath);
        else if (!avatar.isHuman || !avatar.isValid)
            Debug.LogWarning("EnemyNpcAnimatorSetupBuilder: enemy Avatar exists but is not a valid Humanoid Avatar.");

        return avatar;
    }

    private static void ConfigureAnimationImporter(string animationPath, Avatar sourceAvatar)
    {
        ModelImporter importer = AssetImporter.GetAtPath(animationPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning("EnemyNpcAnimatorSetupBuilder: animation FBX not found at " + animationPath);
            return;
        }

        bool changed = false;

        if (sourceAvatar != null)
        {
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                changed = true;
            }

            if (importer.avatarSetup != ModelImporterAvatarSetup.CopyFromOther)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                changed = true;
            }

            if (importer.sourceAvatar != sourceAvatar)
            {
                importer.sourceAvatar = sourceAvatar;
                changed = true;
            }
        }

        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
            clips = importer.defaultClipAnimations;

        for (int i = 0; i < clips.Length; i++)
        {
            if (!clips[i].loopTime)
            {
                clips[i].loopTime = true;
                changed = true;
            }

            if (clips[i].wrapMode != WrapMode.Loop)
            {
                clips[i].wrapMode = WrapMode.Loop;
                changed = true;
            }
        }

        importer.clipAnimations = clips;

        if (changed)
            importer.SaveAndReimport();
    }

    private static AnimationClip FindAnimationClip(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<AnimationClip>()
            .FirstOrDefault(clip => !clip.name.StartsWith("__preview__", System.StringComparison.OrdinalIgnoreCase));
    }

    private static AnimatorController CreateOrResetController(AnimationClip walkingClip, AnimationClip turningClip)
    {
        EnsureFolder(Path.GetDirectoryName(ControllerPath));

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState childState in stateMachine.states)
            stateMachine.RemoveState(childState.state);

        foreach (ChildAnimatorStateMachine childStateMachine in stateMachine.stateMachines)
            stateMachine.RemoveStateMachine(childStateMachine.stateMachine);

        AnimatorState walkingState = stateMachine.AddState("Walking", new Vector3(260f, 80f, 0f));
        walkingState.motion = walkingClip;
        walkingState.speed = 1f;
        stateMachine.defaultState = walkingState;

        if (turningClip != null)
        {
            AnimatorState turningState = stateMachine.AddState("Walking Left Turn", new Vector3(560f, 80f, 0f));
            turningState.motion = turningClip;
            turningState.speed = 1f;
        }

        return controller;
    }

    private static int AssignControllerToSceneEnemies(AnimatorController controller, Avatar avatar)
    {
        int assignedCount = 0;
        GameObject[] objects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);

        foreach (GameObject sceneObject in objects)
        {
            if (!IsEnemyModelInstance(sceneObject))
                continue;

            Animator animator = sceneObject.GetComponent<Animator>();
            if (animator == null)
                animator = sceneObject.AddComponent<Animator>();

            animator.runtimeAnimatorController = controller;
            if (avatar != null)
                animator.avatar = avatar;

            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            EditorUtility.SetDirty(sceneObject);
            EditorUtility.SetDirty(animator);
            assignedCount++;
        }

        if (assignedCount == 0)
            Debug.LogWarning("EnemyNpcAnimatorSetupBuilder: no enemy model instance was found in the active scene. Drag Enemy_NPC.controller into the NPC Animator Controller field manually.");
        else
            Debug.Log("EnemyNpcAnimatorSetupBuilder: assigned Enemy_NPC.controller to " + assignedCount + " scene enemy object(s).");

        return assignedCount;
    }

    private static void CleanupExtraSceneAnimators(AnimatorController controller)
    {
        Animator[] animators = Object.FindObjectsByType<Animator>(FindObjectsInactive.Include);
        int removedCount = 0;

        foreach (Animator animator in animators)
        {
            if (animator == null || animator.runtimeAnimatorController != controller)
                continue;

            if (IsEnemyModelInstance(animator.gameObject))
                continue;

            Object.DestroyImmediate(animator);
            removedCount++;
        }

        if (removedCount > 0)
            Debug.Log("EnemyNpcAnimatorSetupBuilder: removed " + removedCount + " extra Animator component(s) from child objects.");
    }

    private static void SetupBlackStarEnemyPrefab(AnimatorController controller, Avatar avatar)
    {
        GameObject enemyModel = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyModelPath);
        if (enemyModel == null)
        {
            Debug.LogWarning("EnemyNpcAnimatorSetupBuilder: cannot update BlackStarEnemy prefab because enemy model is missing.");
            return;
        }

        GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(BlackStarEnemyPrefabPath);
        GameObject prefabContents;

        if (prefabRoot == null)
        {
            EnsureFolder(Path.GetDirectoryName(BlackStarEnemyPrefabPath));
            prefabContents = new GameObject("BlackStarEnemy");
        }
        else
        {
            prefabContents = PrefabUtility.LoadPrefabContents(BlackStarEnemyPrefabPath);
        }

        prefabContents.name = "BlackStarEnemy";
        prefabContents.tag = "Enemy";
        prefabContents.transform.localPosition = Vector3.zero;
        prefabContents.transform.localRotation = Quaternion.identity;
        prefabContents.transform.localScale = Vector3.one;

        MeshFilter rootMesh = prefabContents.GetComponent<MeshFilter>();
        if (rootMesh != null)
            Object.DestroyImmediate(rootMesh);

        MeshRenderer rootRenderer = prefabContents.GetComponent<MeshRenderer>();
        if (rootRenderer != null)
            Object.DestroyImmediate(rootRenderer);

        CapsuleCollider collider = prefabContents.GetComponent<CapsuleCollider>();
        if (collider == null)
            collider = prefabContents.AddComponent<CapsuleCollider>();

        collider.center = new Vector3(0f, 1.05f, 0f);
        collider.radius = 0.45f;
        collider.height = 2.1f;

        EnemyHealth3D health = prefabContents.GetComponent<EnemyHealth3D>();
        if (health == null)
            health = prefabContents.AddComponent<EnemyHealth3D>();

        EnemyChase3D chase = prefabContents.GetComponent<EnemyChase3D>();
        if (chase == null)
            chase = prefabContents.AddComponent<EnemyChase3D>();

        chase.groundSnapHeight = 8f;
        chase.groundSnapOffset = 0.02f;

        Transform oldVisual = prefabContents.transform.Find("AnimatedEnemyVisual");
        if (oldVisual != null)
            Object.DestroyImmediate(oldVisual.gameObject);

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(enemyModel, prefabContents.transform);
        visual.name = "AnimatedEnemyVisual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        NormalizeVisualScale(visual.transform, 1.9f);

        Animator animator = visual.GetComponent<Animator>();
        if (animator == null)
            animator = visual.AddComponent<Animator>();

        animator.runtimeAnimatorController = controller;
        if (avatar != null)
            animator.avatar = avatar;

        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.updateMode = AnimatorUpdateMode.Normal;
        animator.speed = 1f;

        PrefabUtility.SaveAsPrefabAsset(prefabContents, BlackStarEnemyPrefabPath);
        if (prefabRoot != null)
            PrefabUtility.UnloadPrefabContents(prefabContents);
        else
            Object.DestroyImmediate(prefabContents);

        Debug.Log("EnemyNpcAnimatorSetupBuilder: updated BlackStarEnemy prefab with animated enemy model visual.");
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

        float scaleMultiplier = targetHeight / bounds.size.y;
        visual.localScale *= scaleMultiplier;

        renderers = visual.GetComponentsInChildren<Renderer>(true);
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        visual.localPosition += new Vector3(0f, -bounds.min.y, 0f);
    }

    private static bool IsEnemyModelInstance(GameObject sceneObject)
    {
        GameObject nearestPrefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(sceneObject);
        if (nearestPrefabRoot != null)
        {
            if (nearestPrefabRoot != sceneObject)
                return false;

            GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(nearestPrefabRoot);
            string sourcePath = AssetDatabase.GetAssetPath(prefabSource);
            return sourcePath == EnemyModelPath;
        }

        if (sceneObject.transform.parent != null)
            return false;

        string lowerName = sceneObject.name.ToLowerInvariant();
        if (lowerName.Contains("ennemy") || lowerName.Contains("enemy") || lowerName.Contains("npc"))
            return sceneObject.GetComponentInChildren<Renderer>(true) != null;

        return false;
    }

    private static void EnsureFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || AssetDatabase.IsValidFolder(folder))
            return;

        string parent = Path.GetDirectoryName(folder).Replace("\\", "/");
        string child = Path.GetFileName(folder);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, child);
    }

    private static void WriteReport(AnimatorController controller, Avatar avatar, int assignedCount, AnimationClip walkingClip, AnimationClip turningClip)
    {
        string reportPath = Path.Combine(Directory.GetCurrentDirectory(), "Library", "CodexBridge", "enemy_npc_animator_report.json");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath));

        string json =
            "{\n" +
            "  \"controllerPath\": \"" + ControllerPath + "\",\n" +
            "  \"controllerCreated\": " + (controller != null ? "true" : "false") + ",\n" +
            "  \"avatarFound\": " + (avatar != null ? "true" : "false") + ",\n" +
            "  \"avatarIsHuman\": " + (avatar != null && avatar.isHuman ? "true" : "false") + ",\n" +
            "  \"avatarIsValid\": " + (avatar != null && avatar.isValid ? "true" : "false") + ",\n" +
            "  \"walkingClip\": \"" + (walkingClip != null ? walkingClip.name : string.Empty) + "\",\n" +
            "  \"turningClip\": \"" + (turningClip != null ? turningClip.name : string.Empty) + "\",\n" +
            "  \"assignedSceneObjects\": " + assignedCount + "\n" +
            "}\n";

        File.WriteAllText(reportPath, json);
    }
}
