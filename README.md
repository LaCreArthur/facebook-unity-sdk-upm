# Facebook SDK for Unity (UPM) v18.0.0

[![GitHub stars](https://img.shields.io/github/stars/LaCreArthur/facebook-unity-sdk-upm?style=social)](https://github.com/LaCreArthur/facebook-unity-sdk-upm/stargazers)
[![OpenUPM](https://img.shields.io/npm/v/com.facebook.unity-sdk?label=OpenUPM&registry=https%3A%2F%2Fpackage.openupm.com)](https://openupm.com/packages/com.facebook.unity-sdk/)

A UPM-compatible fork of the official [Meta Facebook SDK for Unity v18.0.0](https://developers.facebook.com/docs/unity/downloads).

**License**: MIT (same as official).

## Features
- **Core**: FB.Init, ActivateApp (install/session tracking for attribution).
- **Analytics**: LogAppEvent (custom events, purchases).
- **Social**: Login, Share (link/feed).
- **Platforms**: Android/iOS/WebGL (Canvas partial).
- **Privacy**: ATT consent, SKAdNetwork v4+.

## Requirements
- Unity 2021.3+ (LTS recommended; tested on 6000.2).
- Android SDK 34+ (Min API 21).
- iOS 12+ (Xcode 15+).
- Meta Developer App (free; create at developers.facebook.com).

## Installation

### Via Unity Package Manager (Recommended)
1. Window > Package Manager > + > Add package from git URL.
2. Enter: `https://github.com/LaCreArthur/facebook-unity-sdk-upm.git`.
3. Import > Wait for resolution (EDM auto-handles deps).

### Via OpenUPM (Scoped Registry)
1. CLI: `openupm add com.facebook.unity-sdk`.
2. Or add registry to manifest.json:
   ```json
   "scopedRegistries": [
     {
       "name": "OpenUPM",
       "url": "https://package.openupm.com",
       "scopes": ["com.facebook.unity-sdk"]
     }
   ],
   "dependencies": {
     "com.facebook.unity-sdk": "18.0.0"
   }
   ```
Refresh Package Manager.

### Manual (Git Clone)
- git clone https://github.com/LaCreArthur/facebook-unity-sdk-upm.git Packages/com.facebook.unity-sdk.
- Refresh Assets.

### Post-install
Assets > External Dependency Manager > Android Resolver > Force Resolve (Android/iOS deps).

### Quick Setup
**Meta App:**
- developers.facebook.com/apps > Create App (Gaming) > Add Android/iOS platforms.
- Copy App ID/Client Token.

**Unity Settings:**
- Facebook > Edit Settings > Enter App ID, App Name, Client Token.
- Android: Package Name matches Player Settings (e.g., com.sorolla.test).
- Generate Key Hashes (requires JDK/OpenSSL in PATH—see Troubleshooting).

**Player Settings (Android):**
- Package Name: e.g., com.sorolla.test.
- Min API 21, Target 34, IL2CPP, ARM64.
- Publishing: Custom Main Manifest (auto-merges FB activities).

**Test Script** (Attach to GameObject):
```csharp
using Facebook.Unity;
using UnityEngine;

public class FBTest : MonoBehaviour
{
    void Awake()
    {
        if (!FB.IsInitialized) FB.Init(OnInit);
        else OnInit();
    }

    void OnInit()
    {
        if (FB.IsInitialized) FB.ActivateApp();
        Debug.Log("FB Ready!");
    }
}
```

**Build & Test:**
- Build APK > Install on device.
- Logcat: adb logcat | grep Facebook—expect "Init Success."
- Meta Events Manager > Test Events: fb_mobile_activate_app appears.

## Usage

### Basic Attribution (CPI/UA)
```csharp
FB.Init(() => {
    if (FB.IsInitialized) {
        FB.ActivateApp();  // Sends install signal
        FB.LogAppEvent("fb_mobile_level_achieved", new Dictionary<string, object> { {"level", 1} });
    }
});
```

### Login
```csharp
FB.LogInWithReadPermissions(new List<string> { "public_profile", "email" }, (result) => {
    if (FB.IsSuccess(result)) Debug.Log("Logged in!");
});
```
Full API: [Meta Docs](https://developers.facebook.com/docs/unity/).

## Troubleshooting
- **DLL Load Failed**: Ensure link.xml in Assets/Facebook/ (preserves IL2CPP types). Reimport.
- **Key Hashes Error**: Generate via keytool | openssl (JDK/OpenSSL in PATH). Add to dashboard.
- **Manifest Collision**: Force Resolve EDM; check custom manifest for FBUnityActivity launcher.
- **No Events**: Clear app data on device; verify App ID (no "fb" prefix in manifest).
- **iOS Pods Fail**: EDM > iOS Resolver > Install Pods.
- **UPM Import Hangs**: Delete Library/ > Refresh.

## License
MIT © Arthur Scheidel (forked from Meta).