using System;
using System.Collections;
using UnityEngine;
#if UNITY_IOS
using System.Runtime.InteropServices;
#endif

namespace SWEF.Build
{
    /// <summary>
    /// Runtime component for iOS App Tracking Transparency (ATT) compliance.
    /// On iOS 14.5+, presents the system ATT permission dialog before any analytics
    /// or tracking begins. On Android and in the Editor the request is a no-op.
    ///
    /// Attach to a persistent Boot-scene GameObject and call
    /// <see cref="RequestAuthorization"/> from <see cref="SWEF.Core.BootManager"/>.
    /// Subscribe to <see cref="OnAuthorizationComplete"/> to react to the result.
    /// </summary>
    public class AppTrackingTransparency : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        /// <summary>Singleton instance; available after Awake.</summary>
        public static AppTrackingTransparency Instance { get; private set; }

        // ── ATT status enum ──────────────────────────────────────────────────
        /// <summary>Mirrors ATTrackingManager.AuthorizationStatus from iOS SDK.</summary>
        public enum AuthorizationStatus
        {
            /// <summary>The user has not yet been asked for permission.</summary>
            NotDetermined = 0,
            /// <summary>Authorization is restricted (e.g. parental controls).</summary>
            Restricted = 1,
            /// <summary>The user denied permission.</summary>
            Denied = 2,
            /// <summary>The user granted permission.</summary>
            Authorized = 3
        }

        // ── Events / State ───────────────────────────────────────────────────
        /// <summary>Fired once the ATT authorization dialog is dismissed (or immediately on non-iOS).</summary>
        public static event Action<AuthorizationStatus> OnAuthorizationComplete;

        /// <summary>Current ATT authorization status. <see cref="AuthorizationStatus.NotDetermined"/> until <see cref="RequestAuthorization"/> completes.</summary>
        public AuthorizationStatus CurrentStatus { get; private set; } = AuthorizationStatus.NotDetermined;

        /// <summary>True once <see cref="RequestAuthorization"/> has completed.</summary>
        public bool IsAuthorizationComplete { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────
        [Header("Config (optional)")]
        [Tooltip("Optional BuildConfig reference. When assigned, RequestAuthorization is skipped if usesTracking is false and analytics is not forced.")]
        [SerializeField] private BuildConfig buildConfig;

#if UNITY_IOS
        // ── Native stubs ─────────────────────────────────────────────────────
        [DllImport("__Internal")]
        private static extern void SWEF_ATT_RequestAuthorization(string callbackObjectName, string callbackMethodName);

        [DllImport("__Internal")]
        private static extern int SWEF_ATT_GetAuthorizationStatus();
#endif

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Requests ATT authorization. On iOS 14.5+ presents the system dialog.
        /// On other platforms completes immediately with <see cref="AuthorizationStatus.Authorized"/>.
        /// Returns a coroutine that can be yielded in <see cref="SWEF.Core.BootManager"/>.
        /// </summary>
        public IEnumerator RequestAuthorization()
        {
#if UNITY_IOS && !UNITY_EDITOR
            // Check if we should skip the prompt (tracking disabled and no analytics)
            if (buildConfig != null && !buildConfig.usesTracking)
            {
                // Analytics still exists locally so we still ask, but only if we have a reason to
                // Skip ATT and grant authorization implicitly for local-only analytics
                CompleteAuthorization(AuthorizationStatus.Authorized);
                yield break;
            }

            IsAuthorizationComplete = false;
            SWEF_ATT_RequestAuthorization(gameObject.name, nameof(OnNativeAuthorizationResult));

            // Wait until the native callback sets IsAuthorizationComplete
            while (!IsAuthorizationComplete)
                yield return null;
#else
            // Android, Editor, or any non-iOS platform: treat as authorized
            CompleteAuthorization(AuthorizationStatus.Authorized);
            yield return null;
#endif
        }

        // ── Native callback (called by iOS native code via UnitySendMessage) ─

        /// <summary>
        /// Called by the native iOS plugin via <c>UnitySendMessage</c> with the
        /// raw integer status value from <c>ATTrackingManagerAuthorizationStatus</c>.
        /// </summary>
        private void OnNativeAuthorizationResult(string statusString)
        {
            int raw = 0;
            int.TryParse(statusString, out raw);
            var status = (AuthorizationStatus)raw;
            CompleteAuthorization(status);
        }

        // ── Internal helpers ─────────────────────────────────────────────────

        private void CompleteAuthorization(AuthorizationStatus status)
        {
            CurrentStatus = status;
            IsAuthorizationComplete = true;

            Debug.Log($"[SWEF] AppTrackingTransparency: authorization status = {status}");

            // Disable analytics when the user denies or is restricted
            if (status == AuthorizationStatus.Denied || status == AuthorizationStatus.Restricted)
            {
                Core.AnalyticsLogger.SetEnabled(false);
                Debug.Log("[SWEF] AppTrackingTransparency: analytics disabled due to ATT denial.");
            }

            OnAuthorizationComplete?.Invoke(status);
        }
    }
}
