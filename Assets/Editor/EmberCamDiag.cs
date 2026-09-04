#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>Toggles the camera's per-second diagnostic define for a device build.</summary>
    public static class EmberCamDiag
    {
        private const string Sym = "EMBER_CAMDIAG";

        public static void On() => Set(true);
        public static void Off() => Set(false);

        private static void Set(bool on)
        {
            var t = NamedBuildTarget.Android;
            PlayerSettings.GetScriptingDefineSymbols(t, out var defs);
            var list = new System.Collections.Generic.List<string>(defs);
            list.Remove(Sym);
            if (on) list.Add(Sym);
            PlayerSettings.SetScriptingDefineSymbols(t, list.ToArray());
            Debug.Log($"[CamDiag] {Sym} {(on ? "enabled" : "disabled")}");
        }
    }
}
#endif
