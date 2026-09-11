using UnityEditor;
using UnityEngine;

namespace Emberline.Editor
{
    public static class BuildAndRun
    {
        [MenuItem("Emberline/Build/Build and Run Android")]
        public static void PerformBuildAndRun()
        {
            string[] scenes = new string[]
            {
                "Assets/Scenes/Opening.unity",
                "Assets/Scenes/Rooftop.unity",
                "Assets/Scenes/Marsh.unity",
                "Assets/Scenes/Zone.unity"
            };

            string apkPath = "Builds/Emberline.apk";

            // Ensure the Builds directory exists
            System.IO.Directory.CreateDirectory("Builds");

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.AutoRunPlayer // Build and auto-run on connected device
            };

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);

            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log("Build and Run succeeded: " + apkPath);
            }
            else
            {
                Debug.LogError("Build failed: " + report.summary.result);
            }
        }
    }
}
