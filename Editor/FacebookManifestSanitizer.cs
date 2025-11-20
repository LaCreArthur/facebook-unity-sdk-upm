using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace Facebook.Unity.Editor
{
    public class FacebookManifestSanitizer : AssetPostprocessor
    {
        private const string MANIFEST_PATH = "Assets/Plugins/Android/AndroidManifest.xml";

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string assetPath in importedAssets)
            {
                if (assetPath == MANIFEST_PATH)
                {
                    SanitizeManifest();
                    return;
                }
            }
        }

        [MenuItem("Facebook/Fix Android Manifest (Smart Patch)")]
        public static void SanitizeManifest()
        {
            if (!File.Exists(MANIFEST_PATH)) return;

            // -----------------------------------------------------------
            // DETECT UNITY VERSION & DEFINE TARGETS
            // -----------------------------------------------------------
            
            // Unity 2023.1+ changed the default activity to GameActivity.
            // Unity 2022.3 and older use the classic UnityPlayerActivity.
#if UNITY_2023_1_OR_NEWER
            string targetActivityName = "com.unity3d.player.UnityPlayerGameActivity";
            string targetTheme = "@style/BaseUnityGameActivityTheme";
            bool requiresLibName = true;
#else
            string targetActivityName = "com.unity3d.player.UnityPlayerActivity";
            // Old Unity uses the standard Android full screen theme
            string targetTheme = "@android:style/Theme.NoTitleBar.Fullscreen"; 
            bool requiresLibName = false;
#endif

            try
            {
                XDocument doc = XDocument.Load(MANIFEST_PATH);
                XNamespace android = "http://schemas.android.com/apk/res/android";
                XNamespace tools = "http://schemas.android.com/tools";

                var application = doc.Root?.Element("application");
                if (application == null) return;

                // Find whatever main activity is currently there (Old or New)
                var activity = application.Elements("activity")
                    .FirstOrDefault(e => 
                        (string)e.Attribute(android + "name") == "com.unity3d.player.UnityPlayerActivity" || 
                        (string)e.Attribute(android + "name") == "com.unity3d.player.UnityPlayerGameActivity"
                    );

                if (activity != null)
                {
                    bool changed = false;

                    // 1. ENFORCE CORRECT CLASS NAME (Version Specific)
                    if ((string)activity.Attribute(android + "name") != targetActivityName)
                    {
                        activity.SetAttributeValue(android + "name", targetActivityName);
                        changed = true;
                    }

                    // 2. ENFORCE CORRECT THEME (Version Specific)
                    // Note: Only force theme if it's currently wrong or missing to avoid fighting custom themes in older Unity
                    if (requiresLibName) // GameActivity requires specific theme, stricter check
                    {
                        if ((string)activity.Attribute(android + "theme") != targetTheme)
                        {
                            activity.SetAttributeValue(android + "theme", targetTheme);
                            changed = true;
                        }
                    }

                    // 3. ENFORCE EXPORTED (Required for ALL Unity versions on Android 12+)
                    if ((string)activity.Attribute(android + "exported") != "true")
                    {
                        activity.SetAttributeValue(android + "exported", "true");
                        changed = true;
                    }

                    // 4. HANDLE LIB_NAME (Only for GameActivity/New Unity)
                    var libNameMeta = activity.Elements("meta-data").FirstOrDefault(x => (string)x.Attribute(android + "name") == "android.app.lib_name");
                    if (requiresLibName)
                    {
                        if (libNameMeta == null)
                        {
                            activity.Add(new XElement("meta-data", 
                                new XAttribute(android + "name", "android.app.lib_name"),
                                new XAttribute(android + "value", "game")
                            ));
                            changed = true;
                        }
                    }
                    else
                    {
                        // If we are on Old Unity, remove this if it accidentally got added, just to be clean
                        if (libNameMeta != null)
                        {
                            libNameMeta.Remove();
                            changed = true;
                        }
                    }

                    // 5. CLEANUP TOOLS:NODE
                    var toolsNode = activity.Attributes(tools + "node").FirstOrDefault();
                    if (toolsNode != null)
                    {
                        toolsNode.Remove();
                        changed = true;
                    }

                    if (changed)
                    {
                        Debug.Log($"[Facebook SDK UPM] Smart-Patched AndroidManifest for {Application.unityVersion}. Target: {targetActivityName}");
                        doc.Save(MANIFEST_PATH);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Facebook SDK UPM] Failed to sanitize manifest: {e.Message}");
            }
        }
    }
}