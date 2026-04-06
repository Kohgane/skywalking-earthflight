// RadarScopeUI.cs — Phase 119: Advanced AI Traffic Control
// Radar scope display: aircraft targets, data blocks, conflict alerts,
// weather overlay.
// Namespace: SWEF.ATC

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — Manages the radar scope display: renders aircraft blips,
    /// data blocks and conflict alert overlays.
    /// </summary>
    public class RadarScopeUI : MonoBehaviour
    {
        [Header("Scope Settings")]
        [SerializeField] [Range(20f, 300f)] private float rangeNM = 60f;
        [SerializeField] private bool showDataBlocks = true;
        [SerializeField] private bool showConflictAlerts = true;
        [SerializeField] private bool showWeatherOverlay = false;

        private readonly Dictionary<string, RadarTarget> _targets = new Dictionary<string, RadarTarget>();

        // ── Radar Target ──────────────────────────────────────────────────────────

        private class RadarTarget
        {
            public string callsign;
            public Vector2 scopePosition;  // Normalized –1 to +1
            public int altitude;
            public bool hasConflict;
            public Color blipColor;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Current radar range in nautical miles.</summary>
        public float RangeNM
        {
            get => rangeNM;
            set => rangeNM = Mathf.Clamp(value, 20f, 300f);
        }

        /// <summary>Updates or adds a radar target.</summary>
        public void UpdateTarget(string callsign, Vector3 worldPos, int altFt, bool conflict)
        {
            if (!_targets.TryGetValue(callsign, out var t))
            {
                t = new RadarTarget { callsign = callsign };
                _targets[callsign] = t;
            }
            // Convert world position to normalized scope coordinates
            float rangeM = rangeNM * 1852f;
            t.scopePosition = new Vector2(worldPos.x / rangeM, worldPos.z / rangeM);
            t.altitude = altFt;
            t.hasConflict = conflict;
            t.blipColor = conflict ? Color.red : Color.green;
        }

        /// <summary>Removes a target from the scope.</summary>
        public bool RemoveTarget(string callsign) => _targets.Remove(callsign);

        /// <summary>Number of targets on the scope.</summary>
        public int TargetCount => _targets.Count;

        /// <summary>Whether data blocks are currently shown.</summary>
        public bool ShowDataBlocks => showDataBlocks;

        private void OnGUI()
        {
            // Actual rendering would use Unity UI / canvas in a real implementation.
            // This stub ensures the MonoBehaviour is valid and testable.
        }
    }
}
