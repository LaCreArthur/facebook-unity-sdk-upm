using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public class EDMInjector
{
    private const string EDM_DEP = "com.google.external-dependency-manager";
    private const string EDM_GIT = "https://github.com/googlesamples/unity-jar-resolver.git?path=upm";

    [InitializeOnLoadMethod]
    private static void InjectEDMDependency()
    {
        var manifestPath = "Packages/manifest.json";
        if (!File.Exists(manifestPath)) return;

        try
        {
            var manifestJson = File.ReadAllText(manifestPath);
            var json = JObject.Parse(manifestJson);

            // Add dependency if missing
            var deps = json["dependencies"] as JObject ?? new JObject();
            if (deps[EDM_DEP] == null)
            {
                deps[EDM_DEP] = EDM_GIT;
                json["dependencies"] = deps;
            }

            // Write back (pretty-print)
            File.WriteAllText(manifestPath, json.ToString(Formatting.Indented));
            AssetDatabase.Refresh();

            // Trigger resolve
            TriggerEDMResolve();

            Debug.Log("FB UPM: Injected EDM Git dep to manifest.json. Refresh Package Manager.");
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"FB UPM: EDM injection failed: {e.Message}. Manual add to manifest.json: \"{EDM_DEP}\": \"{EDM_GIT}\"");
        }
    }

    private static void TriggerEDMResolve()
    {
        var resolverType = Type.GetType("Google.JarResolver.AndroidResolver, Google.JarResolver");
        resolverType?.GetMethod("ForceResolve", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);
    }
}