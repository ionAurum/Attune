using System.IO;
using System.Linq;
using Aurumworks;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Attune
{
    /// <summary>
    /// Editor only, for batch mode: builds the Windows player that the menus' self-test runs
    /// (MenuCheck), from the scenes enabled in Build Settings.
    /// -executeMethod Attune.PlayerBuild.BuildWindows -attuneBuildPath folder
    /// </summary>
    public static class PlayerBuild
    {
        public static void BuildWindows()
        {
            string folder = CommandLine.Value("-attuneBuildPath") ?? "Builds/Windows";
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            BuildReport report = BuildPipeline.BuildPlayer(scenes, Path.Combine(folder, PlayerSettings.productName + ".exe"),
                BuildTarget.StandaloneWindows64, BuildOptions.None);
            Debug.Log("Build result: " + report.summary.result);
            if (report.summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
