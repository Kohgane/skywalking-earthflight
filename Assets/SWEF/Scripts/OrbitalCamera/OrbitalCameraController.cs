// OrbitalCameraController.cs — SWEF Satellite View & Orbital Camera System
using System;
using UnityEngine;

namespace SWEF.OrbitalCamera
{
    /// <summary>
    /// Singleton MonoBehaviour that governs altitude-based camera behaviour,
    /// smooth FOV adjustment, Earth-centre orientation, and view-mode switching
    /// for the orbital camera system.
    /// </summary>
    public class OrbitalCameraController : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static OrbitalCameraController Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        #endregion

        #region Inspector Fields

        [Header("Profile")]
        [Tooltip("Orbital camera profile asset containing all configuration.")]
        [SerializeField] private OrbitalCameraProfile profile;

        [Header("Scene References")]
        [Tooltip("Camera whose FOV and transform this controller drives.")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("Transform representing Earth's centre (origin if null).")]
        [SerializeField] private Transform earthCentre;

        [Header("Runtime State")]
        [Tooltip("Current altitude in kilometres (read-only preview).")]
        [SerializeField] private float currentAltitudeKm;

        [Tooltip("Active view mode (read-only preview).")]
        [SerializeField] private OrbitalViewMode currentViewMode = OrbitalViewMode.FreeOrbit;

        #endregion

        #region Events

        /// <summary>Raised when the camera crosses into a new altitude zone.</summary>
        public event Action<AltitudeZone, AltitudeZone> OnAltitudeZoneChanged;

        /// <summary>Raised when the active view mode changes.</summary>
        public event Action<OrbitalViewMode> OnViewModeChanged;

        #endregion

        #region Private State

        private AltitudeZone _currentZone = AltitudeZone.Ground;
        private float _targetFov = 60f;

        // Earth mean radius (km)
        private const double EarthRadiusKm = 6371.0;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            UpdateAltitude();
        }

        private void LateUpdate()
        {
            UpdateAltitude();
            UpdateFov();

            if (currentViewMode == OrbitalViewMode.FreeOrbit ||
                currentViewMode == OrbitalViewMode.PolarOrbit ||
                currentViewMode == OrbitalViewMode.GeoStationary)
            {
                OrientTowardsEarth();
            }
        }

        #endregion

        #region Public API

        /// <summary>Switches the orbital camera to the specified view mode.</summary>
        /// <param name="mode">Desired <see cref="OrbitalViewMode"/>.</param>
        public void SetViewMode(OrbitalViewMode mode)
        {
            if (mode == currentViewMode) return;
            currentViewMode = mode;
            OnViewModeChanged?.Invoke(mode);
        }

        /// <summary>
        /// Smoothly moves the camera to the specified target altitude.
        /// </summary>
        /// <param name="altitudeKm">Target altitude in kilometres.</param>
        public void SetTargetAltitude(float altitudeKm)
        {
            if (profile == null) return;
            altitudeKm = Mathf.Clamp(
                altitudeKm,
                profile.cameraConfig.minAltitudeKm,
                profile.cameraConfig.maxAltitudeKm);

            var dir = GetEarthCentrePosition() - transform.position;
            if (dir == Vector3.zero) return;

            // NOTE: Position arithmetic assumes 1 km equals 1 Unity world unit.
            // If your scene uses a different scale (e.g. 1 unit = 1 m), multiply
            // altitudeKm and EarthRadiusKm by the appropriate conversion factor.
            var radiusKm = (float)EarthRadiusKm + altitudeKm;
            transform.position = GetEarthCentrePosition() - dir.normalized * radiusKm;
        }

        /// <summary>
        /// Points the camera toward the given geographic coordinate.
        /// </summary>
        /// <param name="latitude">Latitude in degrees (−90 to +90).</param>
        /// <param name="longitude">Longitude in degrees (−180 to +180).</param>
        public void LookAtCoordinate(double latitude, double longitude)
        {
            var surfacePoint = LatLonToWorld(latitude, longitude);
            transform.LookAt(surfacePoint);
        }

        /// <summary>Returns the altitude zone the camera currently occupies.</summary>
        public AltitudeZone GetCurrentAltitudeZone() => _currentZone;

        /// <summary>Returns the camera's current altitude in kilometres.</summary>
        public float GetCurrentAltitudeKm() => currentAltitudeKm;

        #endregion

        #region Private Helpers

        private void UpdateAltitude()
        {
            var centrePos = GetEarthCentrePosition();
            var distKm = Vector3.Distance(transform.position, centrePos);
            currentAltitudeKm = distKm - (float)EarthRadiusKm;

            var newZone = AltitudeToZone(currentAltitudeKm);
            if (newZone != _currentZone)
            {
                var prev = _currentZone;
                _currentZone = newZone;
                OnAltitudeZoneChanged?.Invoke(prev, newZone);
            }
        }

        private void UpdateFov()
        {
            if (targetCamera == null || profile == null) return;
            _targetFov = SampleFovCurve(currentAltitudeKm);
            var speed = profile.cameraConfig.transitionSpeed > 0f
                ? profile.cameraConfig.transitionSpeed
                : 2f;
            targetCamera.fieldOfView = Mathf.Lerp(
                targetCamera.fieldOfView, _targetFov, Time.deltaTime * speed);
        }

        private void OrientTowardsEarth()
        {
            var toEarth = GetEarthCentrePosition() - transform.position;
            if (toEarth != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(toEarth.normalized, Vector3.up);
        }

        private float SampleFovCurve(float altKm)
        {
            var pts = profile?.cameraConfig.fovCurvePoints;
            if (pts == null || pts.Length < 4)
                return 60f;

            // pts layout: [alt0, fov0, alt1, fov1, ...]
            for (var i = 0; i < pts.Length - 3; i += 2)
            {
                var a0 = pts[i]; var f0 = pts[i + 1];
                var a1 = pts[i + 2]; var f1 = pts[i + 3];
                if (altKm >= a0 && altKm <= a1)
                {
                    var t = (altKm - a0) / (a1 - a0);
                    return Mathf.Lerp(f0, f1, t);
                }
            }
            // clamp to last value
            return pts[pts.Length - 1];
        }

        private static AltitudeZone AltitudeToZone(float altKm)
        {
            if (altKm < 1f)      return AltitudeZone.Ground;
            if (altKm < 10f)     return AltitudeZone.LowAtmosphere;
            if (altKm < 50f)     return AltitudeZone.HighAtmosphere;
            if (altKm < 200f)    return AltitudeZone.NearSpace;
            if (altKm < 2000f)   return AltitudeZone.LowOrbit;
            return AltitudeZone.HighOrbit;
        }

        private Vector3 GetEarthCentrePosition() =>
            earthCentre != null ? earthCentre.position : Vector3.zero;

        /// <summary>Converts geographic coordinates to a world-space position on the Earth surface.</summary>
        private Vector3 LatLonToWorld(double lat, double lon)
        {
            var latRad = lat * Math.PI / 180.0;
            var lonRad = lon * Math.PI / 180.0;
            var r = (float)EarthRadiusKm;
            var x = (float)(r * Math.Cos(latRad) * Math.Cos(lonRad));
            var y = (float)(r * Math.Sin(latRad));
            var z = (float)(r * Math.Cos(latRad) * Math.Sin(lonRad));
            return GetEarthCentrePosition() + new Vector3(x, y, z);
        }

        #endregion
    }
}
