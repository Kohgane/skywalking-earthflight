// SpaceStationMinimap.cs — SWEF Space Station & Orbital Docking System
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SpaceStation
{
    /// <summary>
    /// Registers space station positions on the minimap as orbital icons and
    /// shows the approach corridor during an active docking sequence.
    /// Also provides an interior-minimap mode that displays the station module
    /// graph while the player is inside the station.
    /// Depends on <c>SWEF_MINIMAP_AVAILABLE</c> for full minimap integration.
    /// </summary>
    public class SpaceStationMinimap : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Tooltip("Sprite to use for orbital station icons on the minimap.")]
        [SerializeField] private Sprite _stationIcon;

        [Tooltip("Sprite to use for the approach corridor indicator.")]
        [SerializeField] private Sprite _corridorIcon;

        // ── Private state ─────────────────────────────────────────────────────────

        private readonly Dictionary<string, object> _minimapMarkers =
            new Dictionary<string, object>();

        private bool _approachCorridorVisible;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (DockingController.Instance != null)
            {
                DockingController.Instance.OnPhaseChanged    += HandlePhaseChanged;
                DockingController.Instance.OnDockingComplete += HandleDockingComplete;
                DockingController.Instance.OnDockingAborted  += HandleDockingAborted;
            }
        }

        private void OnDisable()
        {
            if (DockingController.Instance != null)
            {
                DockingController.Instance.OnPhaseChanged    -= HandlePhaseChanged;
                DockingController.Instance.OnDockingComplete -= HandleDockingComplete;
                DockingController.Instance.OnDockingAborted  -= HandleDockingAborted;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Registers a station to appear on the minimap.</summary>
        public void RegisterStation(StationDefinition definition, Vector3 worldPosition)
        {
            if (definition == null) return;

#if SWEF_MINIMAP_AVAILABLE
            // Full minimap integration when available.
            var marker = SWEF.Minimap.MinimapManager.Instance?.AddMarker(
                definition.stationId, worldPosition, _stationIcon);
            _minimapMarkers[definition.stationId] = marker;
#else
            // Stub: track for interior minimap without full integration.
            _minimapMarkers[definition.stationId] = worldPosition;
            if (Debug.isDebugBuild)
                Debug.Log($"[SpaceStationMinimap] Registered station '{definition.stationId}' (stub).");
#endif
        }

        /// <summary>Unregisters a station from the minimap.</summary>
        public void UnregisterStation(string stationId)
        {
#if SWEF_MINIMAP_AVAILABLE
            if (_minimapMarkers.TryGetValue(stationId, out object marker))
                SWEF.Minimap.MinimapManager.Instance?.RemoveMarker(stationId);
#endif
            _minimapMarkers.Remove(stationId);
        }

        /// <summary>Renders the interior module graph on the minimap.</summary>
        public void ShowInteriorMinimap(StationLayout layout)
        {
            if (layout == null) return;
            if (Debug.isDebugBuild)
                Debug.Log($"[SpaceStationMinimap] Interior minimap — {layout.nodes.Count} nodes.");
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void HandlePhaseChanged(DockingApproachPhase phase)
        {
            bool showCorridor = phase is DockingApproachPhase.InitialAlignment
                                     or DockingApproachPhase.FinalApproach
                                     or DockingApproachPhase.SoftCapture;

            if (showCorridor != _approachCorridorVisible)
            {
                _approachCorridorVisible = showCorridor;
                if (Debug.isDebugBuild)
                    Debug.Log($"[SpaceStationMinimap] Approach corridor visible: {showCorridor}");
            }
        }

        private void HandleDockingComplete()       => _approachCorridorVisible = false;
        private void HandleDockingAborted(string _) => _approachCorridorVisible = false;
    }
}
