using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using Debug = UnityEngine.Debug;

namespace Facebook.Unity.Editor
{
    public static class FBLog
    {
        private const string FB_SDK_DEBUG_SYMBOL = "FB_SDK_DEBUG";

        [MenuItem("Facebook/Debug/Enable Logging")]
        private static void EnableLogging()
        {
            AddDefineSymbol(FB_SDK_DEBUG_SYMBOL);
        }

        [MenuItem("Facebook/Debug/Disable Logging")]
        private static void DisableLogging()
        {
            RemoveDefineSymbol(FB_SDK_DEBUG_SYMBOL);
        }

        [Conditional(FB_SDK_DEBUG_SYMBOL)]
        public static void Log(string message)
        {
            Debug.Log(message);
        }

        [Conditional(FB_SDK_DEBUG_SYMBOL)]
        public static void LogWarning(string message)
        {
            Debug.LogWarning(message);
        }

        public static void LogError(string message)
        {
            Debug.LogError(message);
        }

        private static void AddDefineSymbol(string symbol)
        {
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            var defines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
            if (defines.Contains(symbol)) return;
            PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, defines + ";" + symbol);
            Debug.Log($"<b>[FBLog]</b> Enabled '{symbol}' for {buildTargetGroup}.");
        }

        private static void RemoveDefineSymbol(string symbol)
        {
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            var defines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget).Split(';').ToList();
            defines.Remove(symbol);
            PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, string.Join(";", defines.ToArray()));
            Debug.Log($"<b>[FBLog]</b> Disabled '{symbol}' for {buildTargetGroup}.");
        }
    }
}