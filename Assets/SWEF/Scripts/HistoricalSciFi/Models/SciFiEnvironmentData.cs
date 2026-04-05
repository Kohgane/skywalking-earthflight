// SciFiEnvironmentData.cs — SWEF Phase 106: Historical & Sci-Fi Flight Mode
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.HistoricalSciFi
{
    /// <summary>Identifies the celestial body or environment type.</summary>
    public enum CelestialBody
    {
        /// <summary>Standard Earth atmosphere and gravity.</summary>
        Earth,
        /// <summary>Low-Earth-Orbit / near-space environment.</summary>
        Space,
        /// <summary>Lunar surface — 1/6 gravity, no atmosphere.</summary>
        Moon,
        /// <summary>Martian surface — 0.38 g, thin CO₂ atmosphere.</summary>
        Mars,
    }

    /// <summary>
    /// Immutable data record describing a Sci-Fi flight environment / celestial body.
    /// Controls physical simulation parameters (gravity, atmosphere) and mission availability.
    /// </summary>
    [Serializable]
    public sealed class SciFiEnvironmentData
    {
        // ── Identity ─────────────────────────────────────────────────────────

        /// <summary>Unique machine-readable identifier (e.g. "moon_surface").</summary>
        public string id;

        /// <summary>Human-readable display name shown in the environment selector UI.</summary>
        public string displayName;

        /// <summary>The celestial body this environment represents.</summary>
        public CelestialBody celestialBody;

        /// <summary>Flavour text describing the environment shown to the player.</summary>
        [TextArea(2, 4)]
        public string surfaceDescription;

        // ── Physics Parameters ────────────────────────────────────────────────

        /// <summary>
        /// Gravity multiplier relative to Earth-standard (9.81 m/s²).
        /// Moon ≈ 0.165, Mars ≈ 0.376, Space ≈ 0.0.
        /// </summary>
        [Range(0f, 2f)]
        public float gravityMultiplier;

        /// <summary>
        /// Atmospheric density relative to Earth sea-level (1.225 kg/m³).
        /// Values near 0 mean near-vacuum (Moon/Space); values near 1 are Earth-like.
        /// </summary>
        [Range(0f, 2f)]
        public float atmosphereDensity;

        /// <summary>
        /// Maximum wind speed in km/h at surface level.
        /// Used to modulate turbulence intensity in the flight controller.
        /// </summary>
        public float maxWindSpeedKph;

        // ── Mission Availability ──────────────────────────────────────────────

        /// <summary>IDs of missions that are available in this environment.</summary>
        public List<string> availableMissionIds = new List<string>();

        // ── Factory ──────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a fully-populated <see cref="SciFiEnvironmentData"/> instance.
        /// </summary>
        public static SciFiEnvironmentData Create(
            string id,
            string displayName,
            CelestialBody celestialBody,
            string surfaceDescription,
            float gravityMultiplier,
            float atmosphereDensity,
            float maxWindSpeedKph = 0f,
            List<string> availableMissionIds = null)
        {
            return new SciFiEnvironmentData
            {
                id                   = id,
                displayName          = displayName,
                celestialBody        = celestialBody,
                surfaceDescription   = surfaceDescription,
                gravityMultiplier    = Mathf.Clamp(gravityMultiplier,   0f, 2f),
                atmosphereDensity    = Mathf.Clamp(atmosphereDensity,   0f, 2f),
                maxWindSpeedKph      = Mathf.Max(0f, maxWindSpeedKph),
                availableMissionIds  = availableMissionIds ?? new List<string>(),
            };
        }

        /// <summary>Returns <c>true</c> when the environment has a meaningful atmosphere
        /// (density > 0.01) that would allow aerodynamic flight.</summary>
        public bool HasAtmosphere => atmosphereDensity > 0.01f;

        /// <summary>Returns <c>true</c> when the gravity is effectively zero (Space).</summary>
        public bool IsZeroGravity => gravityMultiplier < 0.01f;
    }
}
