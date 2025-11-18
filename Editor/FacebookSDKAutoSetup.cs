using System.IO;
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
}