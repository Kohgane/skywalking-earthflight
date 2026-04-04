// StoreSubmissionChecklist.cs — SWEF Phase 102: Final QA & Release Candidate Prep
// Data model and registry for store submission requirements across
// App Store (iOS), Google Play (Android), and Steam (PC/Mac).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.QA
{
    // ── Enums ────────────────────────────────────────────────────────────────────

    /// <summary>Target distribution store.</summary>
    public enum StoreTarget
    {
        AppStore,
        GooglePlay,
        Steam
    }

    /// <summary>Status of a single store submission requirement.</summary>
    public enum SubmissionStatus
    {
        /// <summary>Requirement has not been addressed.</summary>
        NotStarted,
        /// <summary>Requirement is in progress.</summary>
        InProgress,
        /// <summary>Requirement is complete and verified.</summary>
        Complete,
        /// <summary>Requirement is not applicable for this submission.</summary>
        NotApplicable
    }

    // ── Data types ───────────────────────────────────────────────────────────────

    /// <summary>A single store submission requirement line item.</summary>
    [Serializable]
    public sealed class SubmissionRequirement
    {
        /// <summary>Unique identifier (e.g. "IOS-META-001").</summary>
        public string Id;

        /// <summary>Store this requirement belongs to.</summary>
        public StoreTarget Store;

        /// <summary>Human-readable category (e.g. "Metadata", "Privacy", "Assets").</summary>
        public string Category;

        /// <summary>Short title of the requirement.</summary>
        public string Title;

        /// <summary>Detailed description and instructions.</summary>
        public string Description;

        /// <summary>Whether this requirement is mandatory (blocks store approval if missing).</summary>
        public bool IsMandatory;

        /// <summary>Current completion status.</summary>
        public SubmissionStatus Status = SubmissionStatus.NotStarted;

        /// <summary>Optional notes or reference links.</summary>
        public string Notes;

        /// <summary>Creates a new <see cref="SubmissionRequirement"/>.</summary>
        public SubmissionRequirement(string id, StoreTarget store, string category, string title,
                                     string description, bool mandatory = true)
        {
            Id          = id;
            Store       = store;
            Category    = category;
            Title       = title;
            Description = description;
            IsMandatory = mandatory;
        }
    }

    // ── Registry ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Complete store submission checklist for SWEF v1.0.0-rc1.
    ///
    /// <para>Covers App Store (iOS), Google Play (Android), and Steam (PC/Mac).</para>
    /// </summary>
    public sealed class StoreSubmissionChecklist
    {
        // ── Items ─────────────────────────────────────────────────────────────

        /// <summary>All submission requirements.</summary>
        public IReadOnlyList<SubmissionRequirement> Items => _items;
        private readonly List<SubmissionRequirement> _items = new List<SubmissionRequirement>();

        // ── Summary ───────────────────────────────────────────────────────────

        /// <summary>Returns requirements for a specific store.</summary>
        public List<SubmissionRequirement> GetByStore(StoreTarget store)
        {
            var result = new List<SubmissionRequirement>();
            foreach (var item in _items)
                if (item.Store == store) result.Add(item);
            return result;
        }

        /// <summary>Returns all mandatory requirements that are not yet complete.</summary>
        public List<SubmissionRequirement> GetBlockers()
        {
            var result = new List<SubmissionRequirement>();
            foreach (var item in _items)
                if (item.IsMandatory && item.Status != SubmissionStatus.Complete
                    && item.Status != SubmissionStatus.NotApplicable)
                    result.Add(item);
            return result;
        }

        /// <summary><c>true</c> when all mandatory requirements are complete or N/A.</summary>
        public bool IsReadyForSubmission => GetBlockers().Count == 0;

        // ── Factory ───────────────────────────────────────────────────────────

        /// <summary>Builds and returns the complete default submission checklist.</summary>
        public static StoreSubmissionChecklist Build()
        {
            var cl = new StoreSubmissionChecklist();
            cl.AddAppStoreItems();
            cl.AddGooglePlayItems();
            cl.AddSteamItems();
            return cl;
        }

        // ── Private builders ──────────────────────────────────────────────────

        private void Add(SubmissionRequirement req) => _items.Add(req);

        private void AddAppStoreItems()
        {
            // Metadata
            Add(new SubmissionRequirement("IOS-META-001", StoreTarget.AppStore, "Metadata",
                "App name (≤ 30 chars)",
                "App Store Connect > App Name: 'Skywalking: Earth Flight' (26 chars). Verify character count."));

            Add(new SubmissionRequirement("IOS-META-002", StoreTarget.AppStore, "Metadata",
                "Subtitle (≤ 30 chars)",
                "Set subtitle, e.g. 'Fly from your location to space' — verify localised versions for all supported locales."));

            Add(new SubmissionRequirement("IOS-META-003", StoreTarget.AppStore, "Metadata",
                "Description (≤ 4 000 chars per locale)",
                "Long description covering GPS flight, Cesium tiles, multi-platform, ARIA co-pilot, all major features."));

            Add(new SubmissionRequirement("IOS-META-004", StoreTarget.AppStore, "Metadata",
                "Keywords (≤ 100 chars)",
                "Comma-separated keywords: flight, simulator, GPS, earth, space, cesium, 3D, VR, arcade."));

            Add(new SubmissionRequirement("IOS-META-005", StoreTarget.AppStore, "Metadata",
                "Support URL",
                "Provide a valid support URL (e.g. https://kohgane.com/swef/support)."));

            Add(new SubmissionRequirement("IOS-META-006", StoreTarget.AppStore, "Metadata",
                "Privacy Policy URL",
                "Privacy policy URL required for apps using GPS, camera, microphone, or network. Provide GDPR/CCPA-compliant policy."));

            // Privacy descriptions (NSUsageDescription keys in Info.plist)
            Add(new SubmissionRequirement("IOS-PRIV-001", StoreTarget.AppStore, "Privacy",
                "NSLocationWhenInUseUsageDescription",
                "Info.plist key: 'SWEF uses your location to start your flight from your real-world position using GPS.'"));

            Add(new SubmissionRequirement("IOS-PRIV-002", StoreTarget.AppStore, "Privacy",
                "NSCameraUsageDescription",
                "Info.plist key: 'SWEF accesses your camera to capture in-flight photos and support augmented reality features.'"));

            Add(new SubmissionRequirement("IOS-PRIV-003", StoreTarget.AppStore, "Privacy",
                "NSMicrophoneUsageDescription",
                "Info.plist key: 'SWEF accesses your microphone for voice commands and in-flight voice chat with other pilots.'"));

            Add(new SubmissionRequirement("IOS-PRIV-004", StoreTarget.AppStore, "Privacy",
                "App Tracking Transparency (ATT)",
                "If analytics tracking crosses app boundary, add NSUserTrackingUsageDescription and ATT prompt. Verify PrivacyConsentManager (Phase 70)."));

            Add(new SubmissionRequirement("IOS-PRIV-005", StoreTarget.AppStore, "Privacy",
                "App Privacy Nutrition Labels",
                "Complete 'App Privacy' section in App Store Connect: data types collected (Location, Photos, Audio), usage, and whether data is linked to identity."));

            // Assets
            Add(new SubmissionRequirement("IOS-ASSET-001", StoreTarget.AppStore, "Assets",
                "App icon — all required sizes",
                "Provide app icons at 1024×1024 px (App Store), 180×180 px (iPhone @3x), 167×167 px (iPad Pro @2x), and all other required sizes. No alpha channel."));

            Add(new SubmissionRequirement("IOS-ASSET-002", StoreTarget.AppStore, "Assets",
                "Screenshots — iPhone 6.9-inch (required)",
                "Minimum 3 screenshots at 1320×2868 px (iPhone 16 Pro Max). Show key features: GPS flight, space, HUD, ARIA."));

            Add(new SubmissionRequirement("IOS-ASSET-003", StoreTarget.AppStore, "Assets",
                "Screenshots — iPad 13-inch",
                "Minimum 3 screenshots at 2064×2752 px (iPad Pro 13-inch). Show tablet split-panel HUD."));

            Add(new SubmissionRequirement("IOS-ASSET-004", StoreTarget.AppStore, "Assets",
                "App preview video (optional but recommended)",
                "30-second video at 1080p showing GPS start, flight, space transition. No misleading content.",
                mandatory: false));

            // Build
            Add(new SubmissionRequirement("IOS-BUILD-001", StoreTarget.AppStore, "Build",
                "Bundle ID matches App Store Connect",
                "com.kohgane.swef — verify in Unity Player Settings > iOS > Bundle Identifier."));

            Add(new SubmissionRequirement("IOS-BUILD-002", StoreTarget.AppStore, "Build",
                "Version 1.0.0, Build number 1",
                "Player Settings > Version: 1.0.0; Build: 1. Must be higher than any previously uploaded build."));

            Add(new SubmissionRequirement("IOS-BUILD-003", StoreTarget.AppStore, "Build",
                "Minimum iOS version",
                "Set to iOS 15.0 or higher. Verify in Player Settings > iOS > Target minimum iOS Version."));

            Add(new SubmissionRequirement("IOS-BUILD-004", StoreTarget.AppStore, "Build",
                "ARM64 only (no ARMv7 on iOS)",
                "Unity iOS builds are ARM64 only since Unity 2021. Verify no ARMv7 slice in the IPA."));

            // Content rating
            Add(new SubmissionRequirement("IOS-RATING-001", StoreTarget.AppStore, "Content Rating",
                "Age rating questionnaire",
                "Complete the age-rating questionnaire in App Store Connect. SWEF: no violence, no adult content, realistic flight simulation → expect 4+ or 9+."));
        }

        private void AddGooglePlayItems()
        {
            // Metadata
            Add(new SubmissionRequirement("GP-META-001", StoreTarget.GooglePlay, "Metadata",
                "App title (≤ 30 chars)",
                "Google Play Console > Store listing > App name: 'Skywalking: Earth Flight'."));

            Add(new SubmissionRequirement("GP-META-002", StoreTarget.GooglePlay, "Metadata",
                "Short description (≤ 80 chars)",
                "E.g. 'Fly from your real GPS location to outer space in stunning 3D.'"));

            Add(new SubmissionRequirement("GP-META-003", StoreTarget.GooglePlay, "Metadata",
                "Full description (≤ 4 000 chars)",
                "Cover GPS flight, Cesium 3D tiles, ARIA co-pilot, multiplayer, battle pass. No keyword stuffing."));

            // Permissions audit
            Add(new SubmissionRequirement("GP-PERM-001", StoreTarget.GooglePlay, "Permissions",
                "ACCESS_FINE_LOCATION",
                "AndroidManifest.xml: <uses-permission android:name='android.permission.ACCESS_FINE_LOCATION'/> — required for GPS spawn. Runtime prompt via Unity LocationService."));

            Add(new SubmissionRequirement("GP-PERM-002", StoreTarget.GooglePlay, "Permissions",
                "CAMERA",
                "AndroidManifest.xml: <uses-permission android:name='android.permission.CAMERA'/> — required for photo mode and AR camera. Request at runtime before first camera access."));

            Add(new SubmissionRequirement("GP-PERM-003", StoreTarget.GooglePlay, "Permissions",
                "RECORD_AUDIO",
                "AndroidManifest.xml: <uses-permission android:name='android.permission.RECORD_AUDIO'/> — required for voice commands and in-flight voice chat."));

            Add(new SubmissionRequirement("GP-PERM-004", StoreTarget.GooglePlay, "Permissions",
                "INTERNET",
                "AndroidManifest.xml: <uses-permission android:name='android.permission.INTERNET'/> — required for Cesium tile streaming and multiplayer. Normal permission (no runtime prompt needed)."));

            Add(new SubmissionRequirement("GP-PERM-005", StoreTarget.GooglePlay, "Permissions",
                "ACCESS_NETWORK_STATE",
                "AndroidManifest.xml: <uses-permission android:name='android.permission.ACCESS_NETWORK_STATE'/> — for offline detection. Normal permission."));

            Add(new SubmissionRequirement("GP-PERM-006", StoreTarget.GooglePlay, "Permissions",
                "VIBRATE",
                "AndroidManifest.xml: <uses-permission android:name='android.permission.VIBRATE'/> — for haptic feedback. Normal permission."));

            Add(new SubmissionRequirement("GP-PERM-007", StoreTarget.GooglePlay, "Permissions",
                "Remove any unused permissions",
                "Audit AndroidManifest.xml and Unity player settings. Remove permissions that Unity auto-adds but SWEF does not use (e.g. WRITE_EXTERNAL_STORAGE on API 29+)."));

            // Assets
            Add(new SubmissionRequirement("GP-ASSET-001", StoreTarget.GooglePlay, "Assets",
                "High-res icon 512×512 px",
                "32-bit PNG, no alpha clipping issues. Required for Play Store listing."));

            Add(new SubmissionRequirement("GP-ASSET-002", StoreTarget.GooglePlay, "Assets",
                "Feature graphic 1024×500 px",
                "Used as banner on store page and tablet layout. Show compelling key art."));

            Add(new SubmissionRequirement("GP-ASSET-003", StoreTarget.GooglePlay, "Assets",
                "Screenshots — phone (min 2, max 8)",
                "At least 2 phone screenshots; recommended 5 at 1080×1920 px or higher."));

            Add(new SubmissionRequirement("GP-ASSET-004", StoreTarget.GooglePlay, "Assets",
                "Screenshots — tablet 7-inch and 10-inch",
                "At least 1 screenshot per tablet size class showing tablet HUD layout.",
                mandatory: false));

            // Build
            Add(new SubmissionRequirement("GP-BUILD-001", StoreTarget.GooglePlay, "Build",
                "Target API level 34 (Android 14)",
                "Required by Google Play as of August 2024. Set in Unity Player Settings > Android > Target API Level."));

            Add(new SubmissionRequirement("GP-BUILD-002", StoreTarget.GooglePlay, "Build",
                "Minimum API level 26 (Android 8.0)",
                "Set Minimum API level to 26 in Unity Player Settings. Covers ≥ 97% of active Android devices."));

            Add(new SubmissionRequirement("GP-BUILD-003", StoreTarget.GooglePlay, "Build",
                "AAB format (Android App Bundle)",
                "Upload as .aab, not .apk. Unity: File > Build Settings > Android > Build App Bundle. Required for Play Store."));

            Add(new SubmissionRequirement("GP-BUILD-004", StoreTarget.GooglePlay, "Build",
                "Keystore signed with release key",
                "Sign with a dedicated release keystore (not the Unity debug keystore). Keep keystore secure."));

            Add(new SubmissionRequirement("GP-BUILD-005", StoreTarget.GooglePlay, "Build",
                "ARM64 + ARMv7 architecture support",
                "Ensure ARM64 is included; ARMv7 optional for older devices. Set in Player Settings > Android > Target Architectures."));

            // Content rating
            Add(new SubmissionRequirement("GP-RATING-001", StoreTarget.GooglePlay, "Content Rating",
                "IARC content rating questionnaire",
                "Complete the IARC rating questionnaire in Play Console. SWEF: realistic flight simulation, no violence → expect Everyone / PEGI 3."));

            // Data safety
            Add(new SubmissionRequirement("GP-SAFETY-001", StoreTarget.GooglePlay, "Data Safety",
                "Google Play Data Safety form",
                "Declare all data types collected: Location (precise), Photos/Videos (in-app storage), Audio files (voice recording). State whether data is shared with 3rd parties (Cesium, analytics)."));
        }

        private void AddSteamItems()
        {
            // Store page
            Add(new SubmissionRequirement("STM-META-001", StoreTarget.Steam, "Store Page",
                "Game title and short description",
                "Steamworks > Store Page > Basic Info. Title: 'Skywalking: Earth Flight'. Short description (300 chars max)."));

            Add(new SubmissionRequirement("STM-META-002", StoreTarget.Steam, "Store Page",
                "Detailed description",
                "HTML-formatted long description in Steamworks. Cover GPS flight, Cesium, ARIA, all major systems."));

            Add(new SubmissionRequirement("STM-META-003", StoreTarget.Steam, "Store Page",
                "Tags and categories",
                "Set genre tags: Simulation, Flight, Casual, Realistic. Category: Single-player, Multi-player, Online PvP, Online Co-op."));

            // Assets
            Add(new SubmissionRequirement("STM-ASSET-001", StoreTarget.Steam, "Assets",
                "Capsule images (header 460×215, small 231×87, main 616×353)",
                "All three capsule sizes required. No text/logo more than 25% of image area on small capsule."));

            Add(new SubmissionRequirement("STM-ASSET-002", StoreTarget.Steam, "Assets",
                "Screenshots (min 5)",
                "At least 5 screenshots at 1920×1080 px. Show GPS takeoff, world flight, space, HUD, ARIA panel."));

            Add(new SubmissionRequirement("STM-ASSET-003", StoreTarget.Steam, "Assets",
                "Trailer video",
                "Upload at least one 1080p trailer video to Steamworks. YouTube link or direct upload.",
                mandatory: false));

            Add(new SubmissionRequirement("STM-ASSET-004", StoreTarget.Steam, "Assets",
                "Background / library art",
                "Library capsule 600×900 px and Library hero 3840×1240 px for the new Steam library.",
                mandatory: false));

            // Build
            Add(new SubmissionRequirement("STM-BUILD-001", StoreTarget.Steam, "Build",
                "Steamworks App ID configured",
                "Set App ID in Steamworks partner portal. Configure depots for Windows x64 and macOS Universal."));

            Add(new SubmissionRequirement("STM-BUILD-002", StoreTarget.Steam, "Build",
                "Steam SDK integration (Steamworks.NET or Facepunch.Steamworks)",
                "Integrate Steam SDK for achievements, leaderboards, and cloud save. Initialise SteamAPI in Boot scene."));

            Add(new SubmissionRequirement("STM-BUILD-003", StoreTarget.Steam, "Build",
                "Steam Cloud Save enabled",
                "Configure Steam Remote Storage paths for save files. Map to Application.persistentDataPath on both Win/Mac."));

            Add(new SubmissionRequirement("STM-BUILD-004", StoreTarget.Steam, "Build",
                "Steam Achievements defined",
                "Add all SWEF achievements to Steamworks portal. Map achievement keys from AchievementManager to Steam stat names."));

            // System requirements
            Add(new SubmissionRequirement("STM-SYS-001", StoreTarget.Steam, "System Requirements",
                "Minimum system requirements",
                "Windows: CPU i5-6600K, GPU GTX 1060, RAM 8 GB, OS Win10 64-bit, Storage 4 GB SSD. macOS: M1 or Intel Core i7, RAM 8 GB, macOS 13+."));

            Add(new SubmissionRequirement("STM-SYS-002", StoreTarget.Steam, "System Requirements",
                "Recommended system requirements",
                "Windows: CPU i7-9700K, GPU RTX 2070, RAM 16 GB. macOS: M2, RAM 16 GB."));

            // Legal
            Add(new SubmissionRequirement("STM-LEGAL-001", StoreTarget.Steam, "Legal",
                "EULA / Terms of Service",
                "Provide EULA link or embed in Steamworks Legal page. Reference privacy policy URL."));

            Add(new SubmissionRequirement("STM-LEGAL-002", StoreTarget.Steam, "Legal",
                "Third-party licences (Cesium, Unity, etc.)",
                "Include open-source licence acknowledgements in in-app credits screen and/or legal documentation."));
        }
    }
}
