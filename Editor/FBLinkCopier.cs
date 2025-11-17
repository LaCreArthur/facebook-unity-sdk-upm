using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class FBLinkCopier
{
    private const string PACKAGE_NAME = "com.lacrearthur.facebook-sdk-for-unity";
    private const string ASSETS_FB_ROOT = "Assets/Facebook";

    [InitializeOnLoadMethod]
    private static void CopyLinkXML()
    {
        if (!Directory.Exists(ASSETS_FB_ROOT)) Directory.CreateDirectory(ASSETS_FB_ROOT);

        // Dynamically resolve package path (works for git/OpenUPM/local)
        var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath($"Packages/{PACKAGE_NAME}");
        if (packageInfo == null)
        {
            Debug.LogWarning($"FB UPM: Package '{PACKAGE_NAME}' not found. Skipping link.xml copy.");
            return;
        }

        var upmLink = Path.Combine(packageInfo.resolvedPath, "link.xml");
        var assetsLink = Path.Combine(ASSETS_FB_ROOT, "link.xml");
        
        if (File.Exists(upmLink) && !File.Exists(assetsLink))
        {
            File.Copy(upmLink, assetsLink, true);
            AssetDatabase.ImportAsset(assetsLink);
            Debug.Log("FB UPM: Copied link.xml to Assets/Facebook/—IL2CPP stripping fixed. Forcing Resolver.");
            TriggerResolvers();
        }
        else if (!File.Exists(upmLink))
        {
            Debug.LogWarning("FB UPM: link.xml missing from UPM package—IL2CPP stripping may occur.");
        }

        // Optional: Trigger EDM for deps (if XML parse needed)
        TriggerResolvers();
    }

    private static void TriggerResolvers()
    {
        var resolverType = Type.GetType("Google.JarResolver.AndroidResolver, Google.JarResolver");
        resolverType?.GetMethod("ForceResolve", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);
    }
}