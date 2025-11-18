using System;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class FacebookEdmFirstResolveRetry
{
    static FacebookEdmFirstResolveRetry()
    {
        EditorApplication.delayCall += () =>
        {
            // Wait 10 seconds after import, then force one clean resolve
            // This guarantees the embedded OpenJDK path is "seen" by EDM
            EditorApplication.delayCall += () =>
            {
                Debug.Log(
                    "<b>[Facebook SDK]</b> Performing one-time EDM auto-resolve to ensure embedded Java is detected...");
                var t = Type.GetType("GooglePlayServices.PlayServicesResolver, Google.JarResolver");
                t?.GetMethod("AutoResolve")?.Invoke(null, null);
            };
        };
    }
}