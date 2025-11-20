# Facebook SDK for Unity (UPM Fork) v18.0.0

[![GitHub stars](https://img.shields.io/github/stars/LaCreArthur/facebook-unity-sdk-upm?style=social)](https://github.com/LaCreArthur/facebook-unity-sdk-upm/stargazers)
[![OpenUPM](https://img.shields.io/npm/v/com.lacrearthur.facebook-sdk-for-unity?label=OpenUPM&registry=https%3A%2F%2Fpackage.openupm.com)](https://openupm.com/packages/com.lacrearthur.facebook-sdk-for-unity/)

A **fixed and modernized** UPM-compatible fork of the official [Meta Facebook SDK for Unity v18.0.0](https://developers.facebook.com/docs/unity/downloads).

This package solves the "Manifest Merger" and "GameActivity" crashes that plague the official SDK. It includes a **Smart Android Manifest Sanitizer** that automatically patches your build for Android 12+ (API 31) and properly handles the transition between Unity 2022 and Unity 6.

**License**: [Facebook Platform License](https://github.com/LaCreArthur/facebook-unity-sdk-upm/blob/master/LICENSE.md).

## Why this fork?
- **Auto-Fixes Android Manifest**: Automatically detects Unity version and applies the correct Activity (`UnityPlayerActivity` vs `UnityPlayerGameActivity`).
- **Android 12+ Ready**: Automatically injects `android:exported="true"` to prevent crashes on modern devices.
- **No "Link.xml" Hassle**: Auto-injects preservation rules to prevent IL2CPP stripping.
- **UPM Native**: clean installation without `.unitypackage` clutter.

## Requirements
- **Unity 2022.3 LTS** (Uses legacy `UnityPlayerActivity`).
- **Unity 6000.x+** (Uses modern `UnityPlayerGameActivity`).
- Android SDK 34+ (Min API 21).
- iOS 12+ (Xcode 15+).

## Installation

### Via Unity Package Manager (Recommended)
1. Window > Package Manager > + > Add package from git URL.
2. Enter: `https://github.com/LaCreArthur/facebook-unity-sdk-upm.git`.
3. Import > Wait for resolution (EDM auto-handles dependencies).

### Via OpenUPM
1. CLI: `openupm add com.lacrearthur.facebook-sdk-for-unity`.
2. Or add registry to `Packages/manifest.json`:
   ```json
   "scopedRegistries": [
     {
       "name": "OpenUPM",
       "url": "[https://package.openupm.com](https://package.openupm.com)",
       "scopes": ["com.lacrearthur"]
     }
   ],
   "dependencies": {
     "com.lacrearthur.facebook-sdk-for-unity": "18.0.0"
   }
   ```

### Quick Setup
**Meta App:**
- developers.facebook.com/apps > Create App (Gaming) > Add Android/iOS platforms.
- Copy App ID/Client Token.

**Unity Settings:**
- Facebook > Edit Settings > Enter App ID, App Name, Client Token.
- Android: Package Name matches Player Settings (e.g., com.sorolla.test).
- Generate Key Hashes (requires JDK/OpenSSL in PATH, see Troubleshooting).

**Player Settings (Android):**
- Package Name: e.g., com.sorolla.test.
- Min API 21, Target 34, IL2CPP, ARM64.
- Publishing: Custom Main Manifest (auto-merges FB activities).

**Build & Test:**
- Build APK > Install on device.
- Logcat: adb logcat | grep Facebook, expect "Init Success."
- Meta Events Manager > Test Events: fb_mobile_activate_app appears.

Full API: [Meta Docs](https://developers.facebook.com/docs/unity/).

## Troubleshooting
- **DLL Load Failed**: Ensure link.xml in Assets/Facebook/ (preserves IL2CPP types). Reimport.
- **Key Hashes Error**: Generate via keytool | openssl (JDK/OpenSSL in PATH). Add to dashboard.
- **Manifest Collision**: Force Resolve EDM; check custom manifest for FBUnityActivity launcher.
- **No Events**: Clear app data on device; verify App ID (no "fb" prefix in manifest).
- **iOS Pods Fail**: EDM > iOS Resolver > Install Pods.
- **UPM Import Hangs**: Delete Library/ > Refresh.

## License
Facebook Platform License © Meta (forked). See Developer Terms: developers.facebook.com/terms.