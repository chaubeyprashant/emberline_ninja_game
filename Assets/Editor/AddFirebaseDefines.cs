using UnityEditor;
using System.Linq;

public static class AddFirebaseDefines
{
    public static void DoIt()
    {
        var buildTargetGroups = new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone, BuildTargetGroup.iOS };
        foreach (var group in buildTargetGroups)
        {
            var definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            var defines = definesString.Split(';').ToList();
            bool modified = false;

            if (!defines.Contains("FIREBASE_AUTH"))
            {
                defines.Add("FIREBASE_AUTH");
                modified = true;
            }
            if (!defines.Contains("FIREBASE_FIRESTORE"))
            {
                defines.Add("FIREBASE_FIRESTORE");
                modified = true;
            }

            if (modified)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", defines));
            }
        }
    }
}
