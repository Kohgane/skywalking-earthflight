// SurveyMinimapIntegration.cs — SWEF Terrain Scanning & Geological Survey System
using UnityEngine;

// Optional dependency guard — MinimapManager
#if SWEF_MINIMAP_AVAILABLE
using SWEF.Minimap;
#endif

namespace SWEF.TerrainSurvey
{
    /// <summary>
    /// Subscribes to <see cref="SurveyPOIManager.OnPOIDiscovered"/> and registers a
    /// minimap blip for each newly found <see cref="SurveyPOI"/>.
    /// Integrates with <c>SWEF.Minimap.MinimapManager</c> when available (null-safe).
    /// </summary>
    public class SurveyMinimapIntegration : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Filter")]
        [Tooltip("When set, only POIs of this feature type create minimap blips. " +
                 "Leave as -1 (cast to enum) to show all.")]
        [SerializeField] private GeologicalFeatureType filterType = (GeologicalFeatureType)(-1);

        [SerializeField]
        [Tooltip("Show blips for all feature types regardless of filter.")]
        private bool showAll = true;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (SurveyPOIManager.Instance != null)
                SurveyPOIManager.Instance.OnPOIDiscovered += OnPOIDiscovered;
        }

        private void OnDisable()
        {
            if (SurveyPOIManager.Instance != null)
                SurveyPOIManager.Instance.OnPOIDiscovered -= OnPOIDiscovered;
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void OnPOIDiscovered(SurveyPOI poi)
        {
            if (poi == null) return;

            if (!showAll && (int)filterType >= 0 && poi.featureType != filterType) return;

            RegisterMinimapBlip(poi);
        }

        private void RegisterMinimapBlip(SurveyPOI poi)
        {
#if SWEF_MINIMAP_AVAILABLE
            if (MinimapManager.Instance == null) return;

            var blip = new MinimapBlip
            {
                worldPosition = poi.position,
                label         = poi.nameLocKey,
                iconType      = MinimapIconType.SurveyPOI,
                id            = poi.id,
                color         = GeologicalClassifier.GetFeatureColor(poi.featureType),
            };
            MinimapManager.Instance.RegisterBlip(blip);
#else
            // MinimapManager not available — log discovery position for diagnostics
            Debug.Log($"[SurveyMinimap] POI discovered: {poi.featureType} @ {poi.position} (minimap unavailable)");
#endif
        }
    }
}
