using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class FBLinkCopier
{
    private const string UPM_ROOT = "Packages/facebook-unity-sdk-upm";
    private const string ASSETS_FB_ROOT = "Assets/Facebook";

    [InitializeOnLoadMethod]
    private static void CopyLinkXML()
    {
        if (!Directory.Exists(ASSETS_FB_ROOT)) Directory.CreateDirectory(ASSETS_FB_ROOT);

        var upmLink = Path.Combine(UPM_ROOT, "link.xml");
        var assetsLink = Path.Combine(ASSETS_FB_ROOT, "link.xml");
        if (File.Exists(upmLink) && !File.Exists(assetsLink))
        {
            File.Copy(upmLink, assetsLink, true);
            AssetDatabase.ImportAsset(assetsLink);
            Debug.Log("FB UPM: Copied link.xml to Assets/Facebook/—IL2CPP stripping fixed. Forcing Resolver.");
        }
		else if (!File.Exists(upmLink))
        {
            Debug.LogWarning("FB UPM: link.xml missing from UPM package—IL2CPP stripping may occur.");
        }
		else if (File.Exists(assetsLink))
        {
            Debug.Log("FB UPM: link.xml already exists in Assets/Facebook/—skipping copy.");
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