// SatelliteViewRenderer.cs — SWEF Satellite View & Orbital Camera System
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.OrbitalCamera
{
    /// <summary>
    /// Manages altitude-based detail levels, city-lights visibility, country/region
    /// boundary overlays, POI markers, and the atmosphere rim-glow for satellite view.
    /// </summary>
    public class SatelliteViewRenderer : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Profile")]
        [Tooltip("Orbital camera profile supplying satellite view configuration.")]
        [SerializeField] private OrbitalCameraProfile profile;

        [Header("City Lights")]
        [Tooltip("Root GameObject containing city lights geometry / particles.")]
        [SerializeField] private GameObject cityLightsRoot;

        [Tooltip("Altitude (km) below which city lights become visible at night.")]
        [SerializeField] private float cityLightsMaxAltKm = 1500f;

        [Header("Atmosphere Rim")]
        [Tooltip("Renderer responsible for the rim-glow atmosphere effect.")]
        [SerializeField] private Renderer atmosphereRimRenderer;

        [Tooltip("Atmosphere rim glow Material property name for intensity.")]
        [SerializeField] private string rimIntensityProperty = "_RimIntensity";

        [Header("POI Markers")]
        [Tooltip("List of POI marker transforms to scale based on altitude.")]
        [SerializeField] private List<Transform> poiMarkers = new List<Transform>();

        [Header("Overlay")]
        [Tooltip("GameObject shown when Borders overlay is active.")]
        [SerializeField] private GameObject bordersOverlayRoot;

        [Tooltip("GameObject shown when Grid overlay is active.")]
        [SerializeField] private GameObject gridOverlayRoot;

        [Tooltip("GameObject shown when Heatmap overlay is active.")]
        [SerializeField] private GameObject heatmapOverlayRoot;

        #endregion

        #region Private State

        private OverlayMode _activeOverlay = OverlayMode.None;
        private bool _nightLightsEnabled = true;
        private string _highlightedRegion = string.Empty;
        private static readonly int RimIntensityId = Shader.PropertyToID("_RimIntensity");

        #endregion

        #region Unity Lifecycle

        private void LateUpdate()
        {
            var ctrl = OrbitalCameraController.Instance;
            if (ctrl == null) return;

            var altKm = ctrl.GetCurrentAltitudeKm();
            UpdateCityLights(altKm);
            UpdateAtmosphereRim(altKm);
            UpdatePoiMarkers(altKm);
        }

        #endregion

        #region Public API

        /// <summary>Sets the active overlay mode for country borders, grid, or heatmap.</summary>
        /// <param name="mode">Desired <see cref="OverlayMode"/>.</param>
        public void SetOverlayMode(OverlayMode mode)
        {
            _activeOverlay = mode;
            SetActiveIfNotNull(bordersOverlayRoot, mode == OverlayMode.Borders);
            SetActiveIfNotNull(gridOverlayRoot,    mode == OverlayMode.Grid);
            SetActiveIfNotNull(heatmapOverlayRoot, mode == OverlayMode.Heatmap);
        }

        /// <summary>Enables or disables city lights on the night side of Earth.</summary>
        /// <param name="enabled">Whether to show city lights.</param>
        public void SetNightLightsEnabled(bool enabled)
        {
            _nightLightsEnabled = enabled;
            if (!enabled && cityLightsRoot != null)
                cityLightsRoot.SetActive(false);
        }

        /// <summary>
        /// Highlights a country or region by its identifier.
        /// Pass an empty string to clear the highlight.
        /// </summary>
        /// <param name="regionId">Region identifier string.</param>
        public void HighlightRegion(string regionId)
        {
            _highlightedRegion = regionId ?? string.Empty;
            // Region highlight rendering is driven externally (e.g. shader keyword / material).
        }

        #endregion

        #region Private Helpers

        private void UpdateCityLights(float altKm)
        {
            if (cityLightsRoot == null || !_nightLightsEnabled) return;
            cityLightsRoot.SetActive(altKm <= cityLightsMaxAltKm);
        }

        private void UpdateAtmosphereRim(float altKm)
        {
            if (atmosphereRimRenderer == null) return;
            // Rim intensity increases with altitude
            var curvatureStart = profile != null
                ? profile.satelliteViewConfig.earthCurvatureStartAltitudeKm
                : 50f;
            var intensity = Mathf.Clamp01(altKm / Mathf.Max(curvatureStart, 1f));
            atmosphereRimRenderer.material.SetFloat(RimIntensityId, intensity);
        }

        private void UpdatePoiMarkers(float altKm)
        {
            if (profile == null || poiMarkers == null) return;
            var scale = SampleLabelScaleCurve(altKm);
            foreach (var t in poiMarkers)
            {
                if (t != null)
                    t.localScale = Vector3.one * scale;
            }
        }

        private float SampleLabelScaleCurve(float altKm)
        {
            var pts = profile?.satelliteViewConfig.labelScaleCurvePoints;
            if (pts == null || pts.Length < 4) return 1f;

            for (var i = 0; i < pts.Length - 3; i += 2)
            {
                var a0 = pts[i]; var s0 = pts[i + 1];
                var a1 = pts[i + 2]; var s1 = pts[i + 3];
                if (altKm >= a0 && altKm <= a1)
                    return Mathf.Lerp(s0, s1, (altKm - a0) / (a1 - a0));
            }
            return pts[pts.Length - 1];
        }

        private static void SetActiveIfNotNull(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        #endregion
    }
}
