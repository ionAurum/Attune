using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Attune
{
    /// <summary>
    /// Editor only (ruling A19): makes whichever of the Hub scene, the demo stub's scene, each built
    /// demo's scene and AttuneTuning.asset is missing, never overwriting one, and sets Build Settings
    /// to start on the Hub. A scene holds a camera and a light; its systems are built in code (SceneBootstrap).
    /// Attune > Make Scenes, or headless while no Editor has the project open:
    /// -executeMethod Attune.SceneMaker.MakeAndExit
    /// </summary>
    public static class SceneMaker
    {
        private const string SceneFolder = "Assets/Attune/Scenes";
        private const string TuningPath = "Assets/Attune/Resources/AttuneTuning.asset";

        private static readonly Color _backdrop = new(0.06f, 0.07f, 0.09f);

        /// <summary>The scenes the game is built from: the hub first, the stub, then each built demo's in the hub's order.</summary>
        public static readonly string[] Scenes = new[] { DemoCatalogue.HubScene, DemoCatalogue.StubScene }
            .Concat(DemoCatalogue.All.Where(demo => !demo.IsStub).Select(demo => demo.Scene))
            .Select(scene => SceneFolder + "/" + scene + ".unity")
            .ToArray();

        /// <summary>Makes what is missing and sets Build Settings.</summary>
        [MenuItem("Attune/Make Scenes")]
        public static void Make()
        {
            foreach (string path in Scenes)
            {
                MakeScene(path);
            }

            MakeTuning();
            SetBuildScenes();
            AssetDatabase.SaveAssets();
        }

        /// <summary>For batch mode: Make, then quit with 0, or 1 if anything went wrong.</summary>
        public static void MakeAndExit()
        {
            try
            {
                Make();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void MakeScene(string path)
        {
            if (File.Exists(path))
            {
                Debug.Log($"SceneMaker: {path} is there already, and is left as it is.");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Camera camera = UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = _backdrop;
            }

            if (!EditorSceneManager.SaveScene(scene, path))
            {
                throw new IOException("SceneMaker couldn't save " + path);
            }

            Debug.Log("SceneMaker: made " + path);
        }

        private static void MakeTuning()
        {
            if (File.Exists(TuningPath))
            {
                Debug.Log($"SceneMaker: {TuningPath} is there already, and is left as it is.");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(TuningPath));
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<AttuneTuning>(), TuningPath);
            Debug.Log("SceneMaker: made " + TuningPath);
        }

        private static void SetBuildScenes()
        {
            string[] current = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (current.SequenceEqual(Scenes))
            {
                return;
            }

            EditorBuildSettings.scenes = Scenes.Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
            Debug.Log("SceneMaker: Build Settings now start on the Hub: " + string.Join(", ", Scenes));
        }
    }
}
