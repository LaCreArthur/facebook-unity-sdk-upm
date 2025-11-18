using Facebook.Unity;
using UnityEngine;

/// <summary>
/// Drop this component on any GameObject (e.g. in your first scene) to automatically:
/// 1. Initialize the Facebook SDK on startup
/// 2. Call FB.ActivateApp on resume (required for accurate analytics & install attribution)
/// 3. Show clear status on screen (great for debugging on device)
/// 
/// This is the recommended way to initialize the Facebook SDK in Unity 2025+.
/// </summary>
[HelpURL("https://developers.facebook.com/docs/unity/")]
public sealed class FacebookSDKInitializer : MonoBehaviour
{
    [Header("Debug Options")]
    [Tooltip("Shows large status text in the center of the screen (only in builds, not Editor)")]
    [SerializeField] private bool showOnScreenStatus = true;

    private string statusText = "Initializing Facebook SDK...";
    private bool initAttempted = false;

    private void Awake()
    {
        // Prevent duplicate initializers (safety if user adds prefab twice)
        var existing = FindObjectsByType<FacebookSDKInitializer>(FindObjectsSortMode.None);
        if (existing.Length > 1)
        {
            if (existing[0] != this) Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        // Ensure Facebook DLL is loadable (helps catch IL2CPP stripping early)
        if (!TryLoadFacebookAndroidAssembly())
            return;

        InitializeFacebookSDK();
    }

    private bool TryLoadFacebookAndroidAssembly()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            System.Reflection.Assembly.Load("Facebook.Unity.Android");
            return true;
        }
        catch (System.Exception e)
        {
            statusText = "ERROR: Facebook Android DLL failed to load!\nCheck link.xml";
            Debug.LogError($"[Facebook SDK] DLL Load Failed: {e}");
            return false;
        }
#else
        return true;
#endif
    }

    private void InitializeFacebookSDK()
    {
        if (initAttempted) return;

        initAttempted = true;

        if (FB.IsInitialized)
        {
            OnFacebookAlreadyInitialized();
        }
        else
        {
            FB.Init(OnInitComplete);
        }
    }

    private void OnInitComplete()
    {
        if (FB.IsInitialized)
        {
            statusText = "Facebook SDK Ready ✓";
            Debug.Log("[Facebook SDK] Initialized successfully!");
            ActivateAppSafely();
        }
        else
        {
            statusText = "Facebook Init Failed!";
            Debug.LogError("[Facebook SDK] Initialization failed!");
        }
    }

    private void OnFacebookAlreadyInitialized()
    {
        statusText = "Facebook SDK Already Active";
        Debug.Log("[Facebook SDK] Already initialized — activating app.");
        ActivateAppSafely();
    }

    private void ActivateAppSafely()
    {
        if (FB.IsInitialized)
        {
            FB.ActivateApp();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus) // resuming
        {
            ActivateAppSafely();
        }
    }

    private void OnGUI()
    {
        if (!showOnScreenStatus || Application.isEditor) return;

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 64,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.cyan }
        };

        var rect = new Rect(0, Screen.height * 0.35f, Screen.width, 200);
        GUI.Label(rect, statusText, style);
    }
}