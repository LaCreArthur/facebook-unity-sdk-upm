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
        ConfigureEDM();
        var resolverType = Type.GetType("Google.JarResolver.AndroidResolver, Google.JarResolver");
        resolverType?.GetMethod("ForceResolve", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);
    }
    
    private static void ConfigureEDM()
    {
        // Check if the EDM settings class exists.
        // This avoids errors if EDM isn't installed.
        Type settingsType = Type.GetType("Google.Android.Resolver.AndroidResolverSettings, Google.ExternalDependencyManager");

        if (settingsType == null)
        {
            // EDM might not be installed or is a different version.
            // You can add more specific logging if you want.
            return;
        }

        // --- 1. Get Unity's Internal JDK Path ---
        // EditorApplication.applicationContentsPath gives:
        // - (Windows) C:/Program Files/Unity/Hub/Editor/6000.2.12f1/Editor/Data
        // - (macOS)   /Applications/Unity/Hub/Editor/6000.2.12f1/Unity.app/Contents
        string unityDataPath = EditorApplication.applicationContentsPath;
        string internalJdkPath;

        #if UNITY_EDITOR_WIN
            internalJdkPath = Path.Combine(unityDataPath, "PlaybackEngines/AndroidPlayer/OpenJDK");
        #elif UNITY_EDITOR_OSX
            internalJdkPath = Path.Combine(unityDataPath, "../PlaybackEngines/AndroidPlayer/OpenJDK");
        #else
            // Fallback for Linux or other platforms
            internalJdkPath = Path.Combine(unityDataPath, "PlaybackEngines/AndroidPlayer/OpenJDK");
        #endif

        // Verify the path actually exists
        if (!Directory.Exists(internalJdkPath))
        {
            Debug.LogWarning("<b>EDM Java Fixer:</b> Could not find Unity's internal OpenJDK path. " +
                             "Ensure 'Android Build Support' with 'OpenJDK' is installed via Unity Hub.");
            return;
        }

        // --- 2. Get EDM Settings Properties ---
        // We use reflection to be safe, but you could also link the assembly.
        // This gets the static 'Instance' property of the settings class
        object settingsInstance = settingsType.GetProperty("Instance").GetValue(null);

        var useJavaHomeProperty = settingsType.GetProperty("UseJavaHome");
        var javaPathProperty = settingsType.GetProperty("JavaPath");

        bool useJavaHome = (bool)useJavaHomeProperty.GetValue(settingsInstance);

        // --- 3. Check and Fix ---
        if (useJavaHome)
        {
            // This is the problem! EDM is set to use JAVA_HOME.
            // Let's fix it.
            useJavaHomeProperty.SetValue(settingsInstance, false);
            javaPathProperty.SetValue(settingsInstance, internalJdkPath);

            // Log a very clear message to the user.
            Debug.LogWarning("<b>[Auto-Fix]</b>: Corrected External Dependency Manager to use Unity's internal OpenJDK.\n" +
                             "The 'Use JAVA_HOME' setting was enabled, which causes build errors.\n" +
                             $"It has been set to use: {internalJdkPath}");
        }
        else
        {
            // It's already configured correctly.
            Debug.Log("<b>EDM Java Fixer:</b> Settings are correct. No action needed.");
        }
    }
}