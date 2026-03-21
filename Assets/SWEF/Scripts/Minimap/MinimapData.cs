using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Minimap
{
    /// <summary>
    /// Classifies every icon type that can appear on the SWEF mini-map or radar overlay.
    /// </summary>
    public enum MinimapIconType
    {
        /// <summary>The local player's own position marker.</summary>
        Player,

        /// <summary>A standard tour/navigation waypoint.</summary>
        Waypoint,

        /// <summary>The next (immediate) waypoint in the active tour or route.</summary>
        WaypointNext,

        /// <summary>A waypoint the player has already passed.</summary>
        WaypointVisited,

        /// <summary>Another connected player in the same multiplayer session.</summary>
        OtherPlayer,

        /// <summary>An unoccupied formation slot in a formation-flying session.</summary>
        FormationSlot,

        /// <summary>The ghost replay aircraft position.</summary>
        GhostReplay,

        /// <summary>An active or upcoming world event (e.g. aurora, air show).</summary>
        WorldEvent,

        /// <summary>A dynamic weather zone (e.g. thunderstorm, fog bank).</summary>
        WeatherZone,

        /// <summary>A general point of interest (landmark, city, airport).</summary>
        PointOfInterest,

        /// <summary>The final navigation destination.</summary>
        Destination,

        /// <summary>A polyline segment drawn along a guided tour path.</summary>
        TourPath,

        /// <summary>An area flagged as hazardous (e.g. restricted airspace, severe weather).</summary>
        DangerZone,

        /// <summary>A designated safe-landing zone.</summary>
        LandingZone
    }

    /// <summary>
    /// Represents a single icon rendered on the mini-map or radar.
    /// All world-space and derived navigation fields are updated every frame by
    /// <c>MinimapManager</c> before the blip is handed to the renderer.
    /// </summary>
    [Serializable]
    public class MinimapBlip
    {
        // ── Identity ──────────────────────────────────────────────────────────────

        /// <summary>Session-unique identifier for this blip (e.g. "player_remote_42").</summary>
        public string blipId;

        /// <summary>Icon category used to select the correct sprite and tint.</summary>
        public MinimapIconType iconType;

        // ── World-space data ──────────────────────────────────────────────────────

        /// <summary>World-space position of the entity represented by this blip.</summary>
        public Vector3 worldPosition;

        // ── Display ───────────────────────────────────────────────────────────────

        /// <summary>Short human-readable label drawn beneath the blip icon.</summary>
        public string label;

        /// <summary>Tint colour applied to the icon sprite.</summary>
        public Color color = Color.white;

        // ── Visibility & animation ────────────────────────────────────────────────

        /// <summary>Whether this blip is currently shown on the map.</summary>
        public bool isActive = true;

        /// <summary>When <c>true</c> the blip plays a pulse / attention animation.</summary>
        public bool isPulsing;

        // ── Sprite override ───────────────────────────────────────────────────────

        /// <summary>
        /// Optional sprite identifier that overrides the default icon for
        /// <see cref="iconType"/>. Leave empty to use the default sprite.
        /// </summary>
        public string customIconId;

        // ── Derived navigation (auto-calculated by MinimapManager) ────────────────

        /// <summary>Straight-line distance in world units from the local player to this blip.</summary>
        public float distanceFromPlayer;

        /// <summary>
        /// Bearing from the local player to this blip in degrees (0–360, clockwise from
        /// the player's current heading direction). Auto-calculated by <c>MinimapManager</c>.
        /// </summary>
        public float bearingDeg;

        // ── Arbitrary metadata ────────────────────────────────────────────────────

        /// <summary>
        /// Flexible key/value store for system-specific data attached to this blip
        /// (e.g. event type, player rank, formation index).
        /// </summary>
        public Dictionary<string, string> metadata = new Dictionary<string, string>();
    }
}
