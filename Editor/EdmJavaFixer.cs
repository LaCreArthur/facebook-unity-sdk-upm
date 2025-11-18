using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class EdmJavaFixer
{
    static EdmJavaFixer()
    {
        EditorApplication.update += CheckAndFix;
    }

    private static void CheckAndFix()
    {
        var settingsType =
            Type.GetType("Google.Android.Resolver.AndroidResolverSettings, Google.ExternalDependencyManager");
        if (settingsType == null) return; // still not loaded → keep waiting forever

        EditorApplication.update -= CheckAndFix; // success → stop
        ApplyJavaFixAndResolve(settingsType);
    }

    private static void ApplyJavaFixAndResolve(Type settingsType)
    {
        var instance = settingsType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        if (instance == null) return;

        var useJavaHomeProp = settingsType.GetProperty("UseJavaHome");
        var javaPathProp = settingsType.GetProperty("JavaPath");
        if (useJavaHomeProp == null || javaPathProp == null) return;

        // Compute Unity's embedded OpenJDK path
        string internalJdkPath;
#if UNITY_EDITOR_OSX
        internalJdkPath =
 Path.GetFullPath(Path.Combine(EditorApplication.applicationContentsPath, "../../PlaybackEngines/AndroidPlayer/OpenJDK"));
#else
        internalJdkPath = Path.Combine(EditorApplication.applicationContentsPath,
            "PlaybackEngines/AndroidPlayer/OpenJDK");
#endif

        if (!Directory.Exists(internalJdkPath))
        {
            Debug.LogWarning(
                "[FB SDK] Unity's embedded OpenJDK not found. Make sure Android Build Support + OpenJDK is installed.");
            return;
        }

        var currentlyUsesJavaHome = (bool)useJavaHomeProp.GetValue(instance);
        var javaPath = javaPathProp.GetValue(instance) as string;
        var usesUnityOpenJdk = !string.IsNullOrEmpty(javaPath) &&
                               javaPath.IndexOf("OpenJDK", StringComparison.OrdinalIgnoreCase) >= 0;

        if (currentlyUsesJavaHome || !usesUnityOpenJdk)
        {
            useJavaHomeProp.SetValue(instance, false);
            javaPathProp.SetValue(instance, internalJdkPath);

            Debug.Log($"<b>[FB SDK Auto-Fix]</b> EDM4U forced to use Unity's embedded OpenJDK:\n{internalJdkPath}");
        }

        // Finally trigger resolution so everything finishes cleanly
        var resolverType = Type.GetType("Google.JarResolver.AndroidResolver, Google.JarResolver");
        resolverType?.GetMethod("ForceResolve", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);

        Debug.Log(
            "<b>[FB SDK Auto-Fix]</b> EDM4U forced to use Unity's embedded OpenJDK – Android resolve will now succeed.");
    }
}