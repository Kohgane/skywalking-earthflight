using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SWEF.Build
{
    /// <summary>
    /// ScriptableObject holding all SWEF build configuration for iOS and Android store submissions.
    /// Create an instance via <b>Assets → Create → SWEF → Build Config</b>.
    /// The default asset lives at <c>Assets/SWEF/Config/SWEFBuildConfig.asset</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "SWEFBuildConfig", menuName = "SWEF/Build Config")]
    public class BuildConfig : ScriptableObject
    {
        // ── App Identity ─────────────────────────────────────────────────────
        [Header("App Identity")]
        [Tooltip("iOS/Android bundle/application identifier.")]
        public string bundleId = "com.kohgane.swef.earthflight";

        [Tooltip("Display name shown on the device home screen.")]
        public string appName = "Skywalking: Earth Flight";

        [Tooltip("Company name shown in PlayerSettings.")]
        public string companyName = "Kohgane";

        // ── Version ──────────────────────────────────────────────────────────
        [Header("Version")]
        [Tooltip("Semantic version string, e.g. 1.0.0.")]
        public string version = "1.0.0";

        [Tooltip("Monotonically increasing build number (iOS CFBundleVersion / Android versionCode).")]
        public int buildNumber = 1;

        // ── iOS ──────────────────────────────────────────────────────────────
        [Header("iOS")]
        [Tooltip("Apple Developer Team ID (10-character string).")]
        public string iosTeamId = "";

        [Tooltip("Provisioning profile name or UUID. Leave empty when iosAutoSign is true.")]
        public string iosProvisioningProfile = "";

        [Tooltip("When true, Xcode handles code signing automatically.")]
        public bool iosAutoSign = true;

        [Tooltip("Minimum iOS version required (e.g. 15.0).")]
        public string iosMinVersion = "15.0";

        [Tooltip("App Store URL for deep-linking and rate prompts.")]
        public string iosAppStoreUrl = "https://apps.apple.com/app/idXXXXXXXXXX";

        // ── Android ──────────────────────────────────────────────────────────
        [Header("Android")]
        [Tooltip("Absolute or project-relative path to the keystore file.")]
        public string androidKeystorePath = "";

        [Tooltip("Keystore password. Store securely; do not commit to source control.")]
        public string androidKeystorePass = "";

        [Tooltip("Key alias inside the keystore.")]
        public string androidKeyAlias = "swef";

        [Tooltip("Key password. Store securely; do not commit to source control.")]
        public string androidKeyPass = "";

        [Tooltip("Minimum Android API level (26 = Android 8.0 Oreo).")]
        public int androidMinSdk = 26;

        [Tooltip("Target Android API level (34 = Android 14).")]
        public int androidTargetSdk = 34;

        [Tooltip("Google Play Store URL for deep-linking.")]
        public string androidPlayStoreUrl = "https://play.google.com/store/apps/details?id=com.kohgane.swef.earthflight";

        // ── Build Options ────────────────────────────────────────────────────
        [Header("Build Options")]
        [Tooltip("Enable Development Build flag. Must be false for store submissions.")]
        public bool developmentBuild = false;

        [Tooltip("Enable deep profiling. Only relevant when developmentBuild is true.")]
        public bool enableDeepProfiling = false;

        [Tooltip("Strip unused engine code to reduce binary size.")]
        public bool stripEngineCode = true;

#if UNITY_EDITOR
        [Tooltip("Scripting backend to use. IL2CPP is required for iOS and recommended for Android.")]
        public ScriptingImplementation scriptingBackend = ScriptingImplementation.IL2CPP;
#endif

        // ── Privacy ──────────────────────────────────────────────────────────
        [Header("Privacy")]
        [Tooltip("URL to the app's privacy policy page.")]
        public string privacyPolicyUrl = "https://kohgane.com/swef/privacy";

        [Tooltip("Whether the app uses device location services.")]
        public bool usesLocationServices = true;

        [Tooltip("Whether the app accesses the device camera.")]
        public bool usesCamera = false;

        [Tooltip("Whether the app reads/writes the photo library (e.g. for screenshot saving).")]
        public bool usesPhotoLibrary = true;

        [Tooltip("Whether the app uses App Tracking Transparency (advertising identifier). Set false unless using ad networks.")]
        public bool usesTracking = false;

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Increments <see cref="buildNumber"/> by one and marks the asset dirty so it is saved.
        /// Called automatically by <see cref="BuildPipeline"/> before each build.
        /// </summary>
        public void IncrementBuildNumber()
        {
            buildNumber++;
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
#endif
            Debug.Log($"[SWEF] BuildConfig: build number incremented to {buildNumber}.");
        }
    }
}
