using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

[InitializeOnLoad]
public static class FacebookSDKAutoSetup
{
    private const string PACKAGE_NAME = "com.lacrearthur.facebook-sdk-for-unity";
    private const string ASSETS_FB_ROOT = "Assets/Facebook";
    private const string ASSETS_RESOURCES = "Assets/Resources";

    static FacebookSDKAutoSetup()
    {
        EditorApplication.delayCall += () =>
        {
            CopyLinkXmlIfNeeded();
            CopyFacebookSettingsAssetIfNeeded();
            EditorApplication.delayCall += FixEdmJavaPathAndResolve; // second delayCall → guarantees EDM is loaded
        };
    }

    // ──────────────────────────────────────────────────────────────
    // 1. Copy link.xml to Assets/Facebook (prevents IL2CPP stripping)
    // ──────────────────────────────────────────────────────────────
    private static void CopyLinkXmlIfNeeded()
    {
        if (!Directory.Exists(ASSETS_FB_ROOT))
            Directory.CreateDirectory(ASSETS_FB_ROOT);

        var packageInfo = PackageInfo.FindForAssetPath($"Packages/{PACKAGE_NAME}");
        if (packageInfo == null)
        {
            Debug.LogWarning(
                $"[FB SDK] Package '{PACKAGE_NAME}' not found via PackageManager. Skipping auto-setup (might be embedded).");
            return;
        }

        var sourceLink = Path.Combine(packageInfo.resolvedPath, "link.xml");
        var destLink = Path.Combine(ASSETS_FB_ROOT, "link.xml");

        if (File.Exists(sourceLink) && !File.Exists(destLink))
        {
            File.Copy(sourceLink, destLink, true);
            AssetDatabase.ImportAsset(destLink);
            Debug.Log(
                $"<b>[FB SDK Auto-Setup]</b> Copied link.xml → {destLink}\nIL2CPP code stripping is now prevented.");
        }
        else if (!File.Exists(sourceLink))
        {
            Debug.LogWarning("FB UPM: link.xml missing from UPM package, IL2CPP stripping may occur.");
        }
    }

    // ──────────────────────────────────────────────────────────────
    // 2. Copy FacebookSettings.asset to Assets/Resources (editable)
    // ──────────────────────────────────────────────────────────────
    private static void CopyFacebookSettingsAssetIfNeeded()
    {
        var targetPath = ASSETS_RESOURCES + "/FacebookSettings.asset";

        if (File.Exists(targetPath)) return; // user already has one

        var packageInfo = PackageInfo.FindForAssetPath($"Packages/{PACKAGE_NAME}");
        if (packageInfo == null) return;

        var sourcePath = Path.Combine(packageInfo.resolvedPath, "Runtime/Resources/FacebookSettings.asset");
        if (!File.Exists(sourcePath))
        {
            Debug.LogWarning("[FB SDK] Could not find FacebookSettings.asset template in package.");
            return;
        }

        if (!Directory.Exists(ASSETS_RESOURCES))
            Directory.CreateDirectory(ASSETS_RESOURCES);

        File.Copy(sourcePath, targetPath, true);
        AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);

        Debug.Log($"<b>[FB SDK Auto-Setup]</b> Copied editable FacebookSettings.asset → {targetPath}\n" +
                  "Go to Assets/Resources/FacebookSettings.asset to enter your App ID & Client Token.");
    }

    // ──────────────────────────────────────────────────────────────
    // 3. Fix EDM4U Java path + force resolve (runs after everything else)
    // ──────────────────────────────────────────────────────────────
    private static void FixEdmJavaPathAndResolve()
    {
        const int maxWaitSeconds = 30;
        var attempts = 0;

        EditorApplication.update += WaitAndFix;

        void WaitAndFix()
        {
            attempts++;
            var settingsType =
                Type.GetType("Google.Android.Resolver.AndroidResolverSettings, Google.ExternalDependencyManager");
            if (settingsType != null)
            {
                EditorApplication.update -= WaitAndFix;
                ApplyJavaFixAndResolve(settingsType);
                return;
            }

            if (attempts * EditorApplication.timeSinceStartup > maxWaitSeconds)
            {
                EditorApplication.update -= WaitAndFix;
                Debug.LogWarning("[FB SDK] Timed out waiting for External Dependency Manager to load.");
            }
        }
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
    }
}