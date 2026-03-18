using UnityEngine;

#if UNITY_XR_MANAGEMENT
using UnityEngine.XR;
#endif

namespace SWEF.XR
{
    /// <summary>
    /// Static utility that detects the current XR platform at runtime.
    /// All detection is performed lazily on first access and cached.
    /// Compiles safely without the XR Management package.
    /// </summary>
    public static class XRPlatformDetector
    {
        /// <summary>Supported XR platform identifiers.</summary>
        public enum XRPlatformType
        {
            None,
            MetaQuest,
            AppleVisionPro,
            PCVR,
            Other
        }

        private static XRPlatformType? _cachedPlatform;

        /// <summary>
        /// Current XR platform, evaluated once on first access and cached.
        /// Returns <see cref="XRPlatformType.None"/> when XR packages are absent.
        /// </summary>
        public static XRPlatformType CurrentPlatform
        {
            get
            {
                if (_cachedPlatform.HasValue)
                    return _cachedPlatform.Value;

                _cachedPlatform = DetectPlatform();
                Debug.Log($"[SWEF] XR Platform detected: {_cachedPlatform.Value}, Device: {DeviceName}");
                return _cachedPlatform.Value;
            }
        }

        /// <summary>Returns true if any XR device is currently connected.</summary>
        public static bool IsXRActive
        {
            get
            {
#if UNITY_XR_MANAGEMENT
                return XRSettings.isDeviceActive;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// Whether hand tracking is available on the current platform.
        /// Stub — always returns false until the XR Hands package is integrated.
        /// </summary>
        public static bool IsHandTrackingAvailable => false; // TODO: Implement with XR Hands package

        /// <summary>
        /// Name of the loaded XR device, or "None" when no XR device is active.
        /// </summary>
        public static string DeviceName
        {
            get
            {
#if UNITY_XR_MANAGEMENT
                string name = XRSettings.loadedDeviceName;
                return string.IsNullOrEmpty(name) ? "None" : name;
#else
                return "None";
#endif
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private static XRPlatformType DetectPlatform()
        {
#if UNITY_XR_MANAGEMENT
            if (!XRSettings.isDeviceActive)
                return XRPlatformType.None;

            string device = XRSettings.loadedDeviceName?.ToLowerInvariant() ?? string.Empty;

            if (device.Contains("oculus") || device.Contains("meta") || device.Contains("quest"))
                return XRPlatformType.MetaQuest;

            if (device.Contains("realitykit") || device.Contains("visionos") || device.Contains("polyspatial"))
                return XRPlatformType.AppleVisionPro;

            if (device.Contains("openvr") || device.Contains("steamvr") || device.Contains("windowsmr"))
                return XRPlatformType.PCVR;

            if (!string.IsNullOrEmpty(device) && device != "none")
                return XRPlatformType.Other;

            return XRPlatformType.None;
#else
            return XRPlatformType.None;
#endif
        }
    }
}
