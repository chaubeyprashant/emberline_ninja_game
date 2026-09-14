using UnityEditor;
using UnityEngine;
using System.Linq;

public class EmberBuild
{
    [MenuItem("Emberline/Build Android APK")]
    public static void BuildAndroid()
    {
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        
        // Ensure Builds directory exists
        if (!System.IO.Directory.Exists("Builds"))
        {
            System.IO.Directory.CreateDirectory("Builds");
        }
        
        string buildPath = "Builds/Emberline.apk";

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;

        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Icon.png");
        if (icon != null) {
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        }

        // Our logo on navy, never the "Made with Unity" card.
        Emberline.EditorTools.EmberSplash.Apply();

        Debug.Log("[Emberline] Starting Android Build...");
        UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        UnityEditor.Build.Reporting.BuildSummary summary = report.summary;

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("[Emberline] Build succeeded! Total size: " + (summary.totalSize / 1024 / 1024) + " MB. Output: " + buildPath);
        }
        else if (summary.result == UnityEditor.Build.Reporting.BuildResult.Failed)
        {
            Debug.LogError("[Emberline] Build failed.");
        }
    }
}
