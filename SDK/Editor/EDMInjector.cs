using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Facebook.Unity.Editor
{
    public class EDMInjector
    {
        private const string EDM_PACKAGE_ID = "com.google.external-dependency-manager";
        private const string EDM_GIT_URL = "https://github.com/googlesamples/unity-jar-resolver.git?path=upm";

        [InitializeOnLoadMethod]
        private static void InjectEDM()
        {
            FBLog.Log("<b>[FB Installer]</b> Checking for Google EDM dependency...");
            var manifestPath = Path.Combine(Application.dataPath, "..", "Packages", "manifest.json");
            if (!File.Exists(manifestPath))
            {
                FBLog.LogWarning($"[FB Installer] manifest.json not found at: {manifestPath}");
                return;
            }

            try
            {
                var jsonText = File.ReadAllText(manifestPath);
                var manifest = JObject.Parse(jsonText);
                var dependencies = manifest["dependencies"] as JObject;

                if (dependencies != null && !dependencies.ContainsKey(EDM_PACKAGE_ID))
                {
                    FBLog.Log("<b>[FB Installer]</b> Injecting Google EDM to manifest.json...");
                    dependencies.Add(EDM_PACKAGE_ID, EDM_GIT_URL);
                    File.WriteAllText(manifestPath, manifest.ToString());
                    FBLog.Log("<b>[FB Installer]</b> Successfully injected EDM. Forcing AssetDatabase refresh.");
                    AssetDatabase.Refresh(); // Force Unity to resolve
                }
                else if (dependencies == null)
                {
                    FBLog.LogWarning("[FB Installer] Could not find 'dependencies' block in manifest.json.");
                }
                else
                {
                    FBLog.Log("<b>[FB Installer]</b> Google EDM dependency already exists.");
                }
            }
            catch (Exception e)
            {
                FBLog.LogError($"[FB Installer] Manifest inject failed: {e.Message}");
            }
        }
    }
}