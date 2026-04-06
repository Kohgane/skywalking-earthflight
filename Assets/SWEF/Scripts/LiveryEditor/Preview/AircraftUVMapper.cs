// AircraftUVMapper.cs — Phase 115: Advanced Aircraft Livery Editor
// Aircraft UV layout display: unwrapped 2D view with zone labels.
// Namespace: SWEF.LiveryEditor

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Provides UV zone layout information for an aircraft model.
    /// Maps named <see cref="UVZone"/> regions to normalised UV rectangles used
    /// by the 2-D canvas editor for zone-based painting and labelling.
    /// </summary>
    public class AircraftUVMapper : MonoBehaviour
    {
        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when the UV layout is rebuilt (e.g. after an aircraft change).</summary>
        public event Action<Dictionary<UVZone, Rect>> OnLayoutBuilt;

        // ── Internal state ────────────────────────────────────────────────────────
        private Dictionary<UVZone, Rect> _zoneRects = new Dictionary<UVZone, Rect>();
        private string _currentAircraftId;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Builds the UV zone layout for the given aircraft.
        /// Uses hard-coded approximate UV rects; in a full engine these would
        /// be driven by the aircraft mesh's UV data.
        /// </summary>
        /// <param name="aircraftId">Identifier of the aircraft whose UVs to map.</param>
        public void BuildLayout(string aircraftId)
        {
            _currentAircraftId = aircraftId;
            _zoneRects = DefaultLayout();
            OnLayoutBuilt?.Invoke(_zoneRects);
            Debug.Log($"[SWEF] AircraftUVMapper: layout built for '{aircraftId}'.");
        }

        /// <summary>Returns the UV bounding rect for the given zone.</summary>
        /// <returns>Normalised <see cref="Rect"/> or <c>Rect.zero</c> if not found.</returns>
        public Rect GetZoneRect(UVZone zone)
        {
            if (zone == UVZone.All) return new Rect(0, 0, 1, 1);
            return _zoneRects.TryGetValue(zone, out var r) ? r : Rect.zero;
        }

        /// <summary>Returns all zone rects as a read-only dictionary.</summary>
        public IReadOnlyDictionary<UVZone, Rect> GetAllZones() => _zoneRects;

        /// <summary>
        /// Returns the UV zone that contains the given UV coordinate,
        /// or <see cref="UVZone.Fuselage"/> as the default.
        /// </summary>
        public UVZone HitTestZone(Vector2 uv)
        {
            foreach (var kv in _zoneRects)
                if (kv.Value.Contains(uv)) return kv.Key;
            return UVZone.Fuselage;
        }

        /// <summary>Aircraft identifier for which the current layout was built.</summary>
        public string CurrentAircraftId => _currentAircraftId;

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static Dictionary<UVZone, Rect> DefaultLayout() =>
            new Dictionary<UVZone, Rect>
            {
                { UVZone.Fuselage,    new Rect(0.10f, 0.20f, 0.80f, 0.50f) },
                { UVZone.Wings,       new Rect(0.00f, 0.00f, 1.00f, 0.20f) },
                { UVZone.Tail,        new Rect(0.70f, 0.70f, 0.30f, 0.30f) },
                { UVZone.Engines,     new Rect(0.10f, 0.72f, 0.50f, 0.20f) },
                { UVZone.LandingGear, new Rect(0.20f, 0.92f, 0.60f, 0.08f) },
                { UVZone.Nose,        new Rect(0.00f, 0.20f, 0.10f, 0.50f) }
            };
    }
}
