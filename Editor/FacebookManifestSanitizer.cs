using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace Facebook.Unity.Editor
{
    public class FacebookManifestSanitizer : AssetPostprocessor
    {
        // This is the file the FB SDK keeps generating
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

        [MenuItem("Facebook/Fix Android Manifest (XML Patch)")]
        public static void SanitizeManifest()
        {
            if (!File.Exists(MANIFEST_PATH)) return;

            try
            {
                XDocument doc = XDocument.Load(MANIFEST_PATH);
                XNamespace android = "http://schemas.android.com/apk/res/android";
                
                // We also need the 'tools' namespace to remove the dangerous 'node="replace"' 
                // attribute if Facebook adds it (they often do).
                XNamespace tools = "http://schemas.android.com/tools";

                var application = doc.Root?.Element("application");
                if (application == null) return;

                // 1. Find the "Bad" Activity (UnityPlayerActivity) OR the "Good" one (if we already fixed it but need to check attributes)
                var activity = application.Elements("activity")
                    .FirstOrDefault(e => 
                        (string)e.Attribute(android + "name") == "com.unity3d.player.UnityPlayerActivity" || 
                        (string)e.Attribute(android + "name") == "com.unity3d.player.UnityPlayerGameActivity"
                    );

                if (activity != null)
                {
                    bool changed = false;

                    // TRANSFORMATION 1: Rename to GameActivity
                    if ((string)activity.Attribute(android + "name") != "com.unity3d.player.UnityPlayerGameActivity")
                    {
                        activity.SetAttributeValue(android + "name", "com.unity3d.player.UnityPlayerGameActivity");
                        changed = true;
                    }

                    // TRANSFORMATION 2: Force the Unity Theme (Crucial for crash prevention)
                    if ((string)activity.Attribute(android + "theme") != "@style/BaseUnityGameActivityTheme")
                    {
                        activity.SetAttributeValue(android + "theme", "@style/BaseUnityGameActivityTheme");
                        changed = true;
                    }

                    // TRANSFORMATION 3: Force Exported="true" (Crucial for Android 12+)
                    if ((string)activity.Attribute(android + "exported") != "true")
                    {
                        activity.SetAttributeValue(android + "exported", "true");
                        changed = true;
                    }

                    // TRANSFORMATION 4: Inject 'android.app.lib_name' meta-data
                    if (!activity.Elements("meta-data").Any(x => (string)x.Attribute(android + "name") == "android.app.lib_name"))
                    {
                        activity.Add(new XElement("meta-data", 
                            new XAttribute(android + "name", "android.app.lib_name"),
                            new XAttribute(android + "value", "game")
                        ));
                        changed = true;
                    }

                    // SAFETY: Remove 'tools:node="replace"' if present. 
                    // This ensures our changes aren't wiped out by the Manifest Merger later.
                    var toolsNode = activity.Attributes(tools + "node").FirstOrDefault();
                    if (toolsNode != null)
                    {
                        toolsNode.Remove();
                        changed = true;
                    }

                    if (changed)
                    {
                        Debug.Log("[Facebook SDK UPM] Patched AndroidManifest.xml: Upgraded to UnityPlayerGameActivity.");
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