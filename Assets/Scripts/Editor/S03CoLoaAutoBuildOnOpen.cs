using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class S03CoLoaAutoBuildOnOpen
{
    private const string ScenePath = "Assets/Scenes/S03.unity";
    private const string SessionKey = "S03CoLoaAutoBuildOnOpen.CheckedThisSession";

    private static readonly string[] RequiredSceneMarkers =
    {
        "S03_CoLoaCombatIntegration",
        "S03_ArenaDirector",
        "S03_BlessingManager",
        "S03_BlessingChoiceRoot",
        "S03_PlayerHealthRoot",
        "S03_EnemySpawn_01"
    };

    static S03CoLoaAutoBuildOnOpen()
    {
        EditorApplication.delayCall += TryAutoBuildMissingScene;
    }

    private static void TryAutoBuildMissingScene()
    {
        if (SessionState.GetBool(SessionKey, false))
            return;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryAutoBuildMissingScene;
            return;
        }

        SessionState.SetBool(SessionKey, true);

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            return;

        if (SceneAlreadyContainsCombatLayer())
            return;

        try
        {
            Debug.Log("S03 Co Loa combat layer is missing. Auto rebuilding scene once for this editor session.");
            S03CoLoaCombatArenaBuilder.BuildScene();
            S03CoLoaCombatArenaBuilder.VerifyScene();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static bool SceneAlreadyContainsCombatLayer()
    {
        string sceneFullPath = Path.Combine(Directory.GetCurrentDirectory(), ScenePath);
        if (!File.Exists(sceneFullPath))
            return false;

        string sceneText = File.ReadAllText(sceneFullPath);
        foreach (string marker in RequiredSceneMarkers)
        {
            if (!sceneText.Contains(marker))
                return false;
        }

        return true;
    }
}