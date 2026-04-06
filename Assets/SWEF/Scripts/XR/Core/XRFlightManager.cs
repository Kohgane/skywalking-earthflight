// XRFlightManager.cs — Phase 112: VR/XR Flight Experience
// Central VR/XR flight manager singleton. DontDestroyOnLoad.
// Initialises XR subsystems and handles session lifecycle.
// Namespace: SWEF.XR

using System;
using UnityEngine;

#if SWEF_XR_AVAILABLE
using UnityEngine.XR.Management;
#endif

namespace SWEF.XR
{
    /// <summary>
    /// Central singleton that manages the entire VR/XR flight session lifecycle.
    /// Initialises platform adapters, tracks session state, and coordinates all
    /// XR sub-systems. Survives scene loads via <c>DontDestroyOnLoad</c>.
    /// </summary>
    public class XRFlightManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Shared singleton instance.</summary>
        public static XRFlightManager Instance { get; private set; }

        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Configuration")]
        [SerializeField] private XRFlightConfig config;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Current state of the XR session.</summary>
        public XRSessionState SessionState { get; private set; } = XRSessionState.Uninitialized;

        /// <summary>Active XR platform detected or configured.</summary>
        public XRPlatform ActivePlatform { get; private set; } = XRPlatform.Generic;

        /// <summary>Active platform adapter.</summary>
        public IXRPlatformAdapter PlatformAdapter { get; private set; }

        /// <summary>Runtime configuration in use.</summary>
        public XRFlightConfig Config => config;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when the session state changes.</summary>
        public event Action<XRSessionState> OnSessionStateChanged;

        /// <summary>Fired once XR initialisation completes successfully.</summary>
        public event Action OnXRReady;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (config == null)
                config = ScriptableObject.CreateInstance<XRFlightConfig>();

            InitialisePlatformAdapter();
        }

        private void Start()
        {
            BeginSession();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                TransitionToState(XRSessionState.Suspended);
            else if (SessionState == XRSessionState.Suspended)
                TransitionToState(XRSessionState.Running);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                StopSession();
                Instance = null;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Begins the XR session, triggering subsystem initialisation.
        /// </summary>
        public void BeginSession()
        {
            if (SessionState == XRSessionState.Running) return;
            TransitionToState(XRSessionState.Initializing);

#if SWEF_XR_AVAILABLE
            var xrManager = XRGeneralSettings.Instance?.Manager;
            if (xrManager != null && !xrManager.isInitializationComplete)
            {
                xrManager.InitializeLoaderSync();
                xrManager.StartSubsystems();
            }
#endif
            TransitionToState(XRSessionState.Running);
            OnXRReady?.Invoke();
            Debug.Log($"[SWEF] XRFlightManager: Session started on platform {ActivePlatform}.");
        }

        /// <summary>Stops the XR session and tears down subsystems.</summary>
        public void StopSession()
        {
            if (SessionState == XRSessionState.Stopped) return;

#if SWEF_XR_AVAILABLE
            var xrManager = XRGeneralSettings.Instance?.Manager;
            if (xrManager != null && xrManager.isInitializationComplete)
            {
                xrManager.StopSubsystems();
                xrManager.DeinitializeLoader();
            }
#endif
            TransitionToState(XRSessionState.Stopped);
            Debug.Log("[SWEF] XRFlightManager: Session stopped.");
        }

        /// <summary>Recenter the headset view.</summary>
        public void RecenterView()
        {
            PlatformAdapter?.RecenterView();
            Debug.Log("[SWEF] XRFlightManager: RecenterView requested.");
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void InitialisePlatformAdapter()
        {
            XRPlatform platform = config != null ? config.preferredPlatform : XRPlatform.Generic;

            if (platform == XRPlatform.Generic)
                platform = DetectPlatform();

            ActivePlatform = platform;

            switch (platform)
            {
#if SWEF_META_QUEST
                case XRPlatform.MetaQuest:
                    PlatformAdapter = new MetaQuestAdapter();
                    break;
#endif
#if SWEF_APPLE_VISION
                case XRPlatform.AppleVisionPro:
                    PlatformAdapter = new AppleVisionProAdapter();
                    break;
#endif
#if SWEF_STEAMVR
                case XRPlatform.SteamVR:
                    PlatformAdapter = new SteamVRAdapter();
                    break;
#endif
                default:
                    PlatformAdapter = new GenericXRAdapter();
                    break;
            }

            PlatformAdapter?.Initialise(config);
            Debug.Log($"[SWEF] XRFlightManager: Using adapter for {platform}.");
        }

        private XRPlatform DetectPlatform()
        {
#if UNITY_ANDROID
            string model = SystemInfo.deviceModel ?? string.Empty;
            if (model.IndexOf("Quest", StringComparison.OrdinalIgnoreCase) >= 0)
                return XRPlatform.MetaQuest;
#endif
#if UNITY_VISIONOS
            return XRPlatform.AppleVisionPro;
#endif
            return XRPlatform.Generic;
        }

        private void TransitionToState(XRSessionState newState)
        {
            if (SessionState == newState) return;
            SessionState = newState;
            OnSessionStateChanged?.Invoke(newState);
        }
    }
}
