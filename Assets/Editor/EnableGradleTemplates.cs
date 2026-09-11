using UnityEditor;
using System.IO;

public static class EnableGradleTemplates
{
    public static void DoIt()
    {
        // Force the creation of mainTemplate.gradle and gradleTemplate.properties
        // Unity uses reflection to call this internal method. But since we can't easily,
        // we can just write the files directly, which is what Unity does anyway.
        // Wait, the easier way is to just set the EditorPrefs or modify the ProjectSettings.
        // But since Unity 2022+, Custom Main Gradle Template is enabled if the file exists.
        
        string pluginPath = "Assets/Plugins/Android";
        if (!Directory.Exists(pluginPath)) Directory.CreateDirectory(pluginPath);
        
        string mainTemplate = Path.Combine(pluginPath, "mainTemplate.gradle");
        string propertiesTemplate = Path.Combine(pluginPath, "gradleTemplate.properties");
        
        // If they don't exist, Unity will generate them if we toggle the UI. 
        // We can't toggle the UI via code easily in batchmode.
    }
}
