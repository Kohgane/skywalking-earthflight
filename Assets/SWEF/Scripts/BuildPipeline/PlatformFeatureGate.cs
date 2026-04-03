// PlatformFeatureGate.cs — SWEF Phase 95: Platform Target Matrix & Build Pipeline
// Static utility that answers whether a named feature is available on the
// current (or a specified) platform, with runtime override support.
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.BuildPipeline
{
    /// <summary>
    /// Runtime feature-flag system for SWEF's cross-platform feature matrix.
    ///
    /// <para>Built-in defaults are derived from <see cref="PlatformTargetMatrix.SupportsFeature"/>.
    /// Individual flags can be overridden at runtime (e.g. by
    /// <see cref="PlatformBootstrapper"/> after loading a <see cref="BuildProfileConfig"/>).</para>
    ///
    /// <para>Supported feature IDs (case-insensitive):
    /// <c>"xr"</c>, <c>"gyroscope"</c>, <c>"gps"</c>, <c>"touch"</c>,
    /// <c>"keyboard"</c>, <c>"gamepad"</c>, <c>"haptics"</c>,
    /// <c>"arcore"</c>, <c>"arkit"</c>.</para>
    /// </summary>
    public static class PlatformFeatureGate
    {
        // ── Override dictionary ───────────────────────────────────────────────────
        // Populated by PlatformBootstrapper; keyed by lower-case feature ID.
        private static readonly Dictionary<string, bool> _overrides =
            new Dictionary<string, bool>(System.StringComparer.OrdinalIgnoreCase);

        // ── Current platform cache ────────────────────────────────────────────────
        private static PlatformTarget? _cachedPlatform;

        private static PlatformTarget CurrentPlatform
        {
            get
            {
                if (!_cachedPlatform.HasValue)
                    _cachedPlatform = PlatformTargetMatrix.GetCurrentPlatform();
                return _cachedPlatform.Value;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns <c>true</c> if <paramref name="featureId"/> is enabled on the
        /// current platform.
        ///
        /// <para>If a runtime override has been set via <see cref="SetOverride"/>,
        /// that value takes precedence over the built-in default.</para>
        /// </summary>
        public static bool IsEnabled(string featureId)
        {
            if (string.IsNullOrEmpty(featureId)) return false;

            string key = featureId.ToLowerInvariant();
            if (_overrides.TryGetValue(key, out bool overrideValue))
                return overrideValue;

            return PlatformTargetMatrix.SupportsFeature(key, CurrentPlatform);
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="featureId"/> is enabled on the
        /// explicitly specified <paramref name="target"/> platform.
        /// Runtime overrides are NOT applied when a target is provided explicitly.
        /// </summary>
        public static bool IsEnabledFor(string featureId, PlatformTarget target)
        {
            if (string.IsNullOrEmpty(featureId)) return false;
            return PlatformTargetMatrix.SupportsFeature(featureId.ToLowerInvariant(), target);
        }

        /// <summary>
        /// Sets a runtime override for <paramref name="featureId"/>.
        /// This is called by <see cref="PlatformBootstrapper"/> after loading the
        /// platform's <see cref="BuildProfileConfig"/>.
        /// </summary>
        public static void SetOverride(string featureId, bool enabled)
        {
            if (string.IsNullOrEmpty(featureId)) return;
            _overrides[featureId.ToLowerInvariant()] = enabled;
        }

        /// <summary>
        /// Removes a previously set override, restoring built-in default behaviour.
        /// </summary>
        public static void ClearOverride(string featureId)
        {
            if (string.IsNullOrEmpty(featureId)) return;
            _overrides.Remove(featureId.ToLowerInvariant());
        }

        /// <summary>Removes all runtime overrides.</summary>
        public static void ClearAllOverrides()
        {
            _overrides.Clear();
        }

        /// <summary>
        /// Invalidates the cached current-platform value.
        /// Call this in tests or if the platform detection context changes.
        /// </summary>
        public static void ResetPlatformCache()
        {
            _cachedPlatform = null;
        }

        // ── Convenience shortcuts ─────────────────────────────────────────────────

        /// <summary>Returns <c>true</c> when XR (VR/MR) is enabled on this platform.</summary>
        public static bool XREnabled     => IsEnabled("xr");

        /// <summary>Returns <c>true</c> when gyroscope input is available.</summary>
        public static bool GyroEnabled   => IsEnabled("gyroscope");

        /// <summary>Returns <c>true</c> when GPS / device location is available.</summary>
        public static bool GPSEnabled    => IsEnabled("gps");

        /// <summary>Returns <c>true</c> when touch-screen input is the primary input.</summary>
        public static bool TouchEnabled  => IsEnabled("touch");

        /// <summary>Returns <c>true</c> when keyboard + mouse input is available.</summary>
        public static bool KeyboardEnabled => IsEnabled("keyboard");

        /// <summary>Returns <c>true</c> when a gamepad / controller is supported.</summary>
        public static bool GamepadEnabled => IsEnabled("gamepad");

        /// <summary>Returns <c>true</c> when haptic feedback is supported.</summary>
        public static bool HapticsEnabled => IsEnabled("haptics");

        /// <summary>Returns <c>true</c> when ARCore (Android AR) is available.</summary>
        public static bool ARCoreEnabled  => IsEnabled("arcore");

        /// <summary>Returns <c>true</c> when ARKit (iOS/iPadOS AR) is available.</summary>
        public static bool ARKitEnabled   => IsEnabled("arkit");
    }
}
