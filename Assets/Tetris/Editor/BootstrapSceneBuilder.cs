using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scene = UnityEngine.SceneManagement.Scene;

/// <summary>
/// Creates <c>Bootstrap.unity</c> and puts it first in Build Settings. Scenes
/// are authored assets, so this is a menu item rather than something that
/// happens on import — running it is an explicit, repeatable step, and it is
/// safe to re-run.
/// </summary>
public static class BootstrapSceneBuilder
{
    private const string GameScenePath = "Assets/Tetris/Scenes/SampleScene.unity";

    [MenuItem("Tetris/Create Bootstrap Scene", priority = 0)]
    public static void Create()
    {
        if (File.Exists(GameBootstrap.BootstrapScenePath) &&
            !EditorUtility.DisplayDialog(
                "Create Bootstrap Scene",
                $"{GameBootstrap.BootstrapScenePath} already exists and will be replaced.\n\nContinue?",
                "Replace",
                "Cancel"))
        {
            return;
        }

        // Prompt before discarding unsaved work in whatever is currently open —
        // this closes the active scene to build the new one.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EnsureFolder(Path.GetDirectoryName(GameBootstrap.BootstrapScenePath).Replace('\\', '/'));

        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // NewScene(Single) makes the new scene active, so both objects land in it.
        GameObject host = new GameObject("Bootstrap");
        host.AddComponent<GameBootstrap>();

        // An empty scene renders nothing and Unity says so in the Game view
        // every run. One camera on the game's backdrop colour makes the handoff
        // an invisible beat rather than a warning and a flash of blue.
        GameObject cameraHost = new GameObject("Bootstrap Camera");
        Camera camera = cameraHost.AddComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = RetroPalette.CameraClear;
        cameraHost.tag = "MainCamera";

        if (!EditorSceneManager.SaveScene(scene, GameBootstrap.BootstrapScenePath))
        {
            Debug.LogError($"Could not save {GameBootstrap.BootstrapScenePath}.");
            return;
        }

        RegisterBuildScenes();
        Debug.Log(
            $"Created {GameBootstrap.BootstrapScenePath} and made it scene 0. " +
            "Play from here for the real startup path; the game scene still runs standalone.");
    }

    /// <summary>
    /// Puts bootstrap at index 0 and keeps the game scene in the list. Index 0
    /// is what a built player loads, so the order is the whole point.
    /// </summary>
    [MenuItem("Tetris/Fix Build Scene Order", priority = 1)]
    public static void RegisterBuildScenes()
    {
        List<EditorBuildSettingsScene> scenes = new()
        {
            new EditorBuildSettingsScene(GameBootstrap.BootstrapScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true)
        };

        // Preserve anything else already registered, minus the two above.
        foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
        {
            string path = existing.path;
            if (path == GameBootstrap.BootstrapScenePath || path == GameScenePath)
                continue;

            scenes.Add(existing);
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
