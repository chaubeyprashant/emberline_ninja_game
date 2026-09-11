using UnityEditor;

public static class SetDefines
{
    public static void DoIt()
    {
        var defines = PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Android);
        if (!defines.Contains("FIREBASE_AUTH"))
        {
            defines += ";FIREBASE_AUTH";
            PlayerSettings.SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Android, defines);
            PlayerSettings.SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Standalone, defines);
            PlayerSettings.SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.iOS, defines);
        }
    }
}
