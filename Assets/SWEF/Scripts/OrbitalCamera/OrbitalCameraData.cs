// OrbitalCameraData.cs — SWEF Satellite View & Orbital Camera System
using System;
using UnityEngine;

namespace SWEF.OrbitalCamera
{
    #region Enumerations

    /// <summary>High-level operating mode of the orbital camera.</summary>
    public enum OrbitalViewMode
    {
        /// <summary>User-controlled free-orbit around the Earth.</summary>
        FreeOrbit,
        /// <summary>Camera locked on a specific surface target.</summary>
        LockedTarget,
        /// <summary>Camera follows the satellite ground-track path.</summary>
        GroundTrack,
        /// <summary>Polar orbit — camera passes over both poles.</summary>
        PolarOrbit,
        /// <summary>Geostationary orbit — fixed over one longitude.</summary>
        GeoStationary
    }

    /// <summary>Altitude band the camera currently occupies.</summary>
    public enum AltitudeZone
    {
        /// <summary>0 – 1 km above sea level.</summary>
        Ground,
        /// <summary>1 – 10 km (troposphere / low atmosphere).</summary>
        LowAtmosphere,
        /// <summary>10 – 50 km (stratosphere / high atmosphere).</summary>
        HighAtmosphere,
        /// <summary>50 – 200 km (mesosphere / near space).</summary>
        NearSpace,
        /// <summary>200 – 2 000 km (low Earth orbit).</summary>
        LowOrbit,
        /// <summary>Above 2 000 km (medium / high Earth orbit).</summary>
        HighOrbit
    }

    /// <summary>Overlay mode applied by the satellite view renderer.</summary>
    public enum OverlayMode
    {
        /// <summary>No overlay.</summary>
        None,
        /// <summary>Country / region border lines.</summary>
        Borders,
        /// <summary>Latitude / longitude grid.</summary>
        Grid,
        /// <summary>Data heatmap layer.</summary>
        Heatmap
    }

    #endregion

    #region Configuration Structs

    /// <summary>
    /// Core camera behaviour parameters for orbital view.
    /// </summary>
    [Serializable]
    public struct OrbitalCameraConfig
    {
        /// <summary>Minimum altitude in kilometres the camera can reach.</summary>
        [Tooltip("Minimum altitude the camera can reach (km).")]
        public float minAltitudeKm;

        /// <summary>Maximum altitude in kilometres the camera can reach.</summary>
        [Tooltip("Maximum altitude the camera can reach (km).")]
        public float maxAltitudeKm;

        /// <summary>Rotational orbit speed (degrees per second).</summary>
        [Tooltip("Orbit rotation speed in degrees per second.")]
        public float orbitSpeedDegPerSec;

        /// <summary>Minimum and maximum camera tilt angle from the nadir direction.</summary>
        [Tooltip("Camera tilt range (min, max degrees from nadir).")]
        public Vector2 tiltRangeDegrees;

        /// <summary>
        /// Altitude-to-FOV mapping: each element is (altitudeKm, fieldOfViewDegrees).
        /// </summary>
        [Tooltip("Altitude (km) to FOV (degrees) curve defined as alternating altitude/FOV pairs.")]
        public float[] fovCurvePoints;

        /// <summary>Speed at which FOV and position transitions are interpolated.</summary>
        [Tooltip("Transition interpolation speed (higher = snappier).")]
        public float transitionSpeed;
    }

    /// <summary>
    /// Timing and keyframe data for cinematic space-to-ground transitions.
    /// </summary>
    [Serializable]
    public struct SpaceToGroundTransitionConfig
    {
        /// <summary>Total default duration of a descent / ascent transition (seconds).</summary>
        [Tooltip("Default transition duration in seconds.")]
        public float transitionDuration;

        /// <summary>Altitude keyframes (km) used to pace the transition curve.</summary>
        [Tooltip("Altitude keyframes in km for transition pacing.")]
        public float[] altitudeKeyframesKm;

        /// <summary>FOV keyframes (degrees) paired with each altitude keyframe.</summary>
        [Tooltip("FOV keyframes in degrees paired with altitude keyframes.")]
        public float[] fovKeyframesDegrees;

        /// <summary>Altitude (km) at which the cloud layer starts to fade out.</summary>
        [Tooltip("Altitude (km) where cloud layer begins to fade during descent.")]
        public float cloudLayerFadeAltitudeKm;

        /// <summary>Altitude (km) at which atmospheric scattering begins to appear.</summary>
        [Tooltip("Altitude (km) where atmospheric scatter effect starts.")]
        public float atmosphericScatterStartAltitudeKm;
    }

    /// <summary>
    /// Visual parameters specific to satellite / top-down view rendering.
    /// </summary>
    [Serializable]
    public struct SatelliteViewConfig
    {
        /// <summary>Altitude (km) at which Earth curvature visualisation starts.</summary>
        [Tooltip("Altitude (km) above which Earth curvature becomes visible.")]
        public float earthCurvatureStartAltitudeKm;

        /// <summary>Whether to show a lat/lon grid overlay by default.</summary>
        [Tooltip("Enable grid overlay in satellite view by default.")]
        public bool showGridOverlay;

        /// <summary>Maximum distance (km) at which POI markers remain visible.</summary>
        [Tooltip("POI visibility range in km.")]
        public float poiVisibilityRangeKm;

        /// <summary>
        /// Altitude-to-scale curve for POI labels; alternating (altitudeKm, scale) pairs.
        /// </summary>
        [Tooltip("Label scale curve: alternating altitude (km) / scale pairs.")]
        public float[] labelScaleCurvePoints;
    }

    /// <summary>
    /// Simplified Keplerian orbital mechanics parameters.
    /// </summary>
    [Serializable]
    public struct OrbitalMechanicsConfig
    {
        /// <summary>
        /// Standard gravitational parameter μ = GM (km³/s²).
        /// Earth standard value ≈ 398 600.4418 km³/s².
        /// </summary>
        [Tooltip("Gravitational parameter μ = GM (km³/s²). Earth ≈ 398600.4418.")]
        public double gravitationalParameter;

        /// <summary>Whether to compute orbital period automatically from altitude.</summary>
        [Tooltip("If true, orbital period is calculated from current altitude.")]
        public bool calculateOrbitalPeriod;

        /// <summary>Allowed inclination range (min, max degrees).</summary>
        [Tooltip("Orbital inclination range (min/max degrees).")]
        public Vector2 inclinationRangeDegrees;

        /// <summary>Allowed orbital eccentricity range (0 = circular, &lt;1 = elliptical).</summary>
        [Tooltip("Orbital eccentricity range (0 = circular).")]
        public Vector2 eccentricityRange;
    }

    #endregion

    #region ScriptableObject Profile

    /// <summary>
    /// Asset that bundles all orbital camera configuration into a single
    /// drag-and-drop <see cref="ScriptableObject"/>.
    /// </summary>
    [CreateAssetMenu(
        fileName = "OrbitalCameraProfile",
        menuName  = "SWEF/OrbitalCamera/Profile",
        order     = 0)]
    public class OrbitalCameraProfile : ScriptableObject
    {
        // ── Inspector ────────────────────────────────────────────────────────────

        [Header("Camera Behaviour")]
        [Tooltip("Core orbital camera behaviour parameters.")]
        public OrbitalCameraConfig cameraConfig;

        [Header("Space-to-Ground Transition")]
        [Tooltip("Keyframe and timing data for descent / ascent transitions.")]
        public SpaceToGroundTransitionConfig transitionConfig;

        [Header("Satellite View")]
        [Tooltip("Visual settings specific to top-down satellite rendering.")]
        public SatelliteViewConfig satelliteViewConfig;

        [Header("Orbital Mechanics")]
        [Tooltip("Simplified Keplerian orbit parameters.")]
        public OrbitalMechanicsConfig mechanicsConfig;
    }

    #endregion
}
