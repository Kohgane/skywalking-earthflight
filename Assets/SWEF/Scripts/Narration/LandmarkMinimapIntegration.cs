using System.Collections.Generic;
using UnityEngine;
using SWEF.Minimap;

namespace SWEF.Narration
{
    /// <summary>
    /// Bridges the Narration system to <see cref="MinimapManager"/>.
    /// Registers a <see cref="MinimapBlip"/> for each landmark in the database
    /// and updates visibility based on discovery state and player configuration.
    /// </summary>
    public class LandmarkMinimapIntegration : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Tooltip("Colour used for undiscovered landmarks.")]
        [SerializeField] private Color undiscoveredColor = new Color(0.7f, 0.7f, 1f, 0.7f);

        [Tooltip("Colour used for discovered landmarks.")]
        [SerializeField] private Color discoveredColor = new Color(0.3f, 1f, 0.5f, 1f);

        // ── State ─────────────────────────────────────────────────────────────────
        private MinimapManager        _minimap;
        private LandmarkDiscoveryTracker _discovery;
        private readonly List<string> _registeredIds = new List<string>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Start()
        {
            _minimap   = MinimapManager.Instance;
            _discovery = LandmarkDiscoveryTracker.Instance;

            if (_minimap == null)
            {
                Debug.LogWarning("[SWEF] LandmarkMinimapIntegration: MinimapManager not found.");
                return;
            }

            RegisterBlips();

            // Subscribe to discover events to refresh colours.
            if (_discovery != null)
                _discovery.OnFirstDiscovery += OnFirstDiscovery;

            // Subscribe to config changes (show/hide landmark icons).
            var mgr = NarrationManager.Instance;
            if (mgr != null)
                mgr.OnNarrationStarted += _ => RefreshVisibility();
        }

        private void OnDestroy()
        {
            UnregisterBlips();
            if (_discovery != null)
                _discovery.OnFirstDiscovery -= OnFirstDiscovery;
        }

        // ── Registration ──────────────────────────────────────────────────────────

        private void RegisterBlips()
        {
            var mgr = NarrationManager.Instance;
            if (mgr?.Database == null) return;
            bool showIcons = mgr.Config.showMinimapIcons;

            // Register all landmarks from all categories.
            foreach (LandmarkCategory cat in System.Enum.GetValues(typeof(LandmarkCategory)))
                foreach (var lm in mgr.Database.GetLandmarksByCategory(cat))
                    RegisterSingleBlip(lm, showIcons);
        }

        private void RegisterSingleBlip(LandmarkData lm, bool showIcons)
        {
            string id = "lm_blip_" + lm.landmarkId;
            if (_registeredIds.Contains(id)) return;

            bool discovered = _discovery?.IsDiscovered(lm.landmarkId) ?? false;

            var blip = new MinimapBlip
            {
                blipId        = id,
                iconType      = MinimapIconType.PointOfInterest,
                label         = lm.name,
                worldPosition = LatLonToWorld(lm.latitude, lm.longitude, lm.altitude),
                color         = discovered ? discoveredColor : undiscoveredColor,
                isActive      = showIcons,
                isPulsing     = false,
                customIconId  = lm.iconType
            };
            blip.metadata["landmarkId"] = lm.landmarkId;
            blip.metadata["category"]   = lm.category.ToString();

            _minimap.RegisterBlip(blip);
            _registeredIds.Add(id);
        }

        private void UnregisterBlips()
        {
            if (_minimap == null) return;
            foreach (var id in _registeredIds)
                _minimap.UnregisterBlip(id);
            _registeredIds.Clear();
        }

        private void OnFirstDiscovery(string landmarkId)
        {
            string blipId = "lm_blip_" + landmarkId;
            var blip = _minimap?.GetBlip(blipId);
            if (blip != null)
            {
                blip.color     = discoveredColor;
                blip.isPulsing = true;
            }
        }

        private void RefreshVisibility()
        {
            var mgr = NarrationManager.Instance;
            if (mgr == null || _minimap == null) return;
            bool show = mgr.Config.showMinimapIcons;
            foreach (var id in _registeredIds)
            {
                var blip = _minimap.GetBlip(id);
                if (blip != null) blip.isActive = show;
            }
        }

        // ── Coordinate conversion ─────────────────────────────────────────────────

        /// <summary>
        /// Converts GPS lat/lon to Unity world-space using the same convention as
        /// <see cref="NarrationManager"/>: 1 unit ≈ 1 metre, origin at (0, 0) = lat 0, lon 0.
        /// </summary>
        private static Vector3 LatLonToWorld(double lat, double lon, float altMetres)
        {
            float x = (float)(lon * 111320.0 * System.Math.Cos(lat * System.Math.PI / 180.0));
            float z = (float)(lat * 111320.0);
            return new Vector3(x, altMetres, z);
        }
    }
}
