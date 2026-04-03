// AircraftBuildData.cs — SWEF Aircraft Workshop & Part Customization (Phase 90)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Workshop
{
    /// <summary>
    /// Serialisable class representing a complete aircraft build / loadout.
    /// Stored as JSON in <c>workshop_builds.json</c> by <see cref="WorkshopManager"/>.
    /// </summary>
    [Serializable]
    public class AircraftBuildData
    {
        // ── Identity ──────────────────────────────────────────────────────────

        /// <summary>Unique identifier for this build (GUID string).</summary>
        [Tooltip("Unique identifier for this build (GUID string).")]
        public string buildId = Guid.NewGuid().ToString();

        /// <summary>Player-assigned name for this build preset.</summary>
        [Tooltip("Player-assigned name for this build preset.")]
        public string buildName = "Custom Build";

        /// <summary>UTC timestamp (ISO-8601) when this build was last saved.</summary>
        [Tooltip("UTC timestamp of the last save.")]
        public string lastSavedUtc = DateTime.UtcNow.ToString("o");

        // ── Equipped Parts ────────────────────────────────────────────────────

        /// <summary>
        /// List of part IDs currently equipped on this aircraft.
        /// At most one part per <see cref="AircraftPartType"/> slot is active at a time;
        /// the first matching entry for each type is used by <see cref="PerformanceSimulator"/>.
        /// </summary>
        [Tooltip("Part IDs of every equipped component.")]
        public List<string> equippedPartIds = new List<string>();

        // ── Cosmetics ─────────────────────────────────────────────────────────

        /// <summary>Paint/livery scheme applied to this build.</summary>
        [Tooltip("Active paint scheme.")]
        public PaintSchemeData paintScheme = new PaintSchemeData();

        /// <summary>
        /// Decals placed on this aircraft.
        /// Maximum count is enforced by <see cref="DecalEditorController.MaxDecals"/>.
        /// </summary>
        [Tooltip("Decals placed on this build (max 10).")]
        public List<DecalData> decals = new List<DecalData>();

        // ── Computed Stats (cached, re-calculated on Apply) ───────────────────

        /// <summary>Cached maximum speed (km/h) from <see cref="PerformanceSimulator"/>.</summary>
        [Tooltip("Cached maximum speed in km/h.")]
        public float cachedMaxSpeed;

        /// <summary>Cached maximum climb rate (m/s).</summary>
        [Tooltip("Cached climb rate in m/s.")]
        public float cachedClimbRate;

        /// <summary>Cached maneuverability score in the range [0, 1].</summary>
        [Tooltip("Cached maneuverability score [0–1].")]
        public float cachedManeuverability;

        /// <summary>Cached fuel-efficiency score in the range [0, 1].</summary>
        [Tooltip("Cached fuel-efficiency score [0–1].")]
        public float cachedFuelEfficiency;

        /// <summary>Cached structural integrity (total durability, arbitrary units).</summary>
        [Tooltip("Cached total structural durability score.")]
        public float cachedStructuralIntegrity;

        /// <summary>
        /// Cached centre-of-gravity balance score in the range [0, 1]
        /// (1 = perfectly balanced, 0 = severely nose/tail heavy).
        /// </summary>
        [Tooltip("Cached CG balance score [0–1].")]
        public float cachedWeightBalance;
    }
}
