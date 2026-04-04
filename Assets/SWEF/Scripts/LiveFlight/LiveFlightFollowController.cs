// LiveFlightFollowController.cs — SWEF Live Flight Tracking & Real-World Data Overlay (Phase 103)
using UnityEngine;

namespace SWEF.LiveFlight
{
    /// <summary>
    /// Locks the camera to a specific live aircraft so the player can watch it fly
    /// in real time.  Smooth follow with configurable offset and look-ahead.
    ///
    /// <para>When compiled with <c>SWEF_FLIGHT_AVAILABLE</c> the player's own
    /// <see cref="SWEF.Flight.FlightController"/> is paused during follow mode.</para>
    /// </summary>
    public class LiveFlightFollowController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Camera")]
        [SerializeField] private Camera followCamera;
        [Tooltip("Offset from the aircraft in local space (metres).")]
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 500f, -2000f);

        [Tooltip("Seconds to smooth camera position changes.")]
        [SerializeField] private float positionSmoothTime = 1.5f;

        [Tooltip("Seconds to smooth camera rotation changes.")]
        [SerializeField] private float rotationSmoothTime = 0.8f;

        [Tooltip("Distance (metres) ahead of the aircraft the camera looks toward.")]
        [SerializeField] private float lookAheadDistance = 3000f;

        [Header("HUD")]
        [SerializeField] private TMPro.TextMeshProUGUI hudCallsignText;
        [SerializeField] private TMPro.TextMeshProUGUI hudAltitudeText;
        [SerializeField] private TMPro.TextMeshProUGUI hudSpeedText;
        [SerializeField] private TMPro.TextMeshProUGUI hudTypeText;
        [SerializeField] private TMPro.TextMeshProUGUI hudRouteText;
        [SerializeField] private GameObject            hudPanel;

        // ── State ─────────────────────────────────────────────────────────────────
        private string           _followingIcao;
        private LiveAircraftInfo _target;
        private bool             _hasTarget;

        private Vector3 _camVelocity;
        private float   _camRotVelocity;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary><c>true</c> while a follow target is active.</summary>
        public bool IsFollowing => _hasTarget;

        /// <summary>The ICAO24 code of the currently followed aircraft (empty if none).</summary>
        public string FollowingIcao => _followingIcao ?? "";

        /// <summary>
        /// Begin following the aircraft identified by <paramref name="icao24"/>.
        /// Updates are applied in <see cref="Update"/> as new data arrives.
        /// </summary>
        public void FollowAircraft(string icao24)
        {
            _followingIcao = icao24;
            _hasTarget     = false; // will be populated on next data push

            if (hudPanel != null) hudPanel.SetActive(true);

#if SWEF_FLIGHT_AVAILABLE
            var fc = FindFirstObjectByType<SWEF.Flight.FlightController>();
            if (fc != null) fc.enabled = false;
#endif

            if (LiveFlightAPIClient.Instance != null)
                LiveFlightAPIClient.Instance.OnAircraftDataReceived += OnAircraftDataReceived;

            LiveFlightAnalytics.TrackAircraftFollowed(icao24);
        }

        /// <summary>Stop following and restore player control.</summary>
        public void StopFollowing()
        {
            _followingIcao = null;
            _hasTarget     = false;

            if (hudPanel != null) hudPanel.SetActive(false);

#if SWEF_FLIGHT_AVAILABLE
            var fc = FindFirstObjectByType<SWEF.Flight.FlightController>();
            if (fc != null) fc.enabled = true;
#endif

            if (LiveFlightAPIClient.Instance != null)
                LiveFlightAPIClient.Instance.OnAircraftDataReceived -= OnAircraftDataReceived;
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void OnDestroy() => StopFollowing();

        private void Update()
        {
            if (!_hasTarget || followCamera == null) return;

            Vector3 targetPos  = AircraftWorldPosition(_target);
            Vector3 forward    = Quaternion.Euler(0f, _target.heading, 0f) * Vector3.forward;
            Vector3 lookTarget = targetPos + forward * lookAheadDistance;

            // Smooth camera position
            Vector3 desiredPos = targetPos
                + Quaternion.Euler(0f, _target.heading, 0f) * cameraOffset;

            followCamera.transform.position = Vector3.SmoothDamp(
                followCamera.transform.position,
                desiredPos,
                ref _camVelocity,
                positionSmoothTime);

            // Smooth camera look
            Quaternion desiredRot = Quaternion.LookRotation(
                (lookTarget - followCamera.transform.position).normalized);
            followCamera.transform.rotation = Quaternion.Slerp(
                followCamera.transform.rotation,
                desiredRot,
                Time.deltaTime / rotationSmoothTime);

            // Update HUD
            UpdateHUD(_target);
        }

        // ── Internals ─────────────────────────────────────────────────────────────

        private void OnAircraftDataReceived(System.Collections.Generic.List<LiveAircraftInfo> list)
        {
            foreach (var info in list)
            {
                if (info.icao24 == _followingIcao)
                {
                    _target    = info;
                    _hasTarget = true;
                    return;
                }
            }
        }

        private void UpdateHUD(LiveAircraftInfo info)
        {
            if (hudCallsignText != null) hudCallsignText.text = info.callsign;
            if (hudAltitudeText != null) hudAltitudeText.text = $"{info.altitude:F0} m";
            if (hudSpeedText    != null) hudSpeedText.text    = $"{info.velocity:F0} m/s";
            if (hudTypeText     != null) hudTypeText.text     = info.aircraftType;
            if (hudRouteText    != null) hudRouteText.text    = $"{info.originCountry}";
        }

        private static Vector3 AircraftWorldPosition(LiveAircraftInfo info)
        {
#if SWEF_TERRAIN_AVAILABLE
            var bridge = SWEF.Terrain.CesiumTerrainBridge.Instance;
            if (bridge != null)
                return bridge.GeodeticToUnity(info.latitude, info.longitude, info.altitude);
#endif
            const double R = 6_371_000.0;
            return new Vector3(
                (float)(info.longitude * Mathf.Deg2Rad * R),
                info.altitude,
                (float)(info.latitude  * Mathf.Deg2Rad * R));
        }
    }
}
