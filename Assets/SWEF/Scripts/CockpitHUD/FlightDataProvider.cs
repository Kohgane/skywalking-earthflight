// FlightDataProvider.cs — SWEF Cockpit Instrument & HUD Dashboard System (Phase 65)
using System;
using UnityEngine;

namespace SWEF.CockpitHUD
{
    /// <summary>
    /// Phase 65 — MonoBehaviour that gathers raw physics data from the aircraft
    /// every <c>FixedUpdate</c> and exposes a fully-populated <see cref="FlightData"/>
    /// snapshot for consumption by HUD instruments.
    ///
    /// <para>Attach to the same GameObject as — or alongside — the aircraft Rigidbody.</para>
    /// </summary>
    public class FlightDataProvider : MonoBehaviour
    {
        #region Inspector

        [Header("Aircraft References")]
        [Tooltip("Rigidbody of the aircraft. Auto-resolved on Awake if left null.")]
        [SerializeField] private Rigidbody aircraftRigidbody;

        [Tooltip("Transform of the aircraft. Defaults to this GameObject's transform if null.")]
        [SerializeField] private Transform aircraftTransform;

        [Header("Altitude")]
        [Tooltip("World-space Y position that is treated as sea level (meters).")]
        [SerializeField] private float seaLevelReference = 0f;

        [Tooltip("Maximum downward raycast distance for AGL calculation (meters).")]
        [SerializeField] private float groundRaycastMaxDistance = 10000f;

        [Header("Stall Detection")]
        [Tooltip("Angle of attack (degrees) above which a stall is detected.")]
        [SerializeField] private float stallAngle = 25f;

        [Tooltip("Speed (m/s) below which a stall is possible regardless of AOA.")]
        [SerializeField] private float stallSpeed = 50f;

        [Header("Overspeed")]
        [Tooltip("Speed (m/s) above which an overspeed warning is triggered.")]
        [SerializeField] private float overspeedThreshold = 154f; // ≈ 300 kt

        #endregion

        #region Public State

        /// <summary>The latest flight data snapshot, updated every FixedUpdate.</summary>
        public FlightData CurrentData { get; private set; } = new FlightData();

        /// <summary>Fired every FixedUpdate after <see cref="CurrentData"/> is refreshed.</summary>
        public event Action<FlightData> OnFlightDataUpdated;

        #endregion

        #region Private State

        private Vector3 _previousVelocity;
        private float   _previousFixedTime;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (aircraftRigidbody == null)
                aircraftRigidbody = GetComponent<Rigidbody>();

            if (aircraftTransform == null)
                aircraftTransform = transform;

            _previousVelocity  = Vector3.zero;
            _previousFixedTime = Time.fixedTime;
        }

        private void FixedUpdate()
        {
            Gather();
            OnFlightDataUpdated?.Invoke(CurrentData);
        }

        #endregion

        #region Data Gathering

        private void Gather()
        {
            FlightData d = CurrentData;

            // ── Position & velocity ──────────────────────────────────────────
            d.position = aircraftTransform.position;
            d.velocity = aircraftRigidbody != null
                ? aircraftRigidbody.linearVelocity
                : Vector3.zero;

            // ── Speed ────────────────────────────────────────────────────────
            d.speed       = d.velocity.magnitude;
            d.speedKnots  = CockpitHUDConfig.MsToKnots(d.speed);
            d.speedMach   = d.speed / CockpitHUDConfig.SpeedOfSound;

            // ── Altitude ASL & AGL ───────────────────────────────────────────
            d.altitude = d.position.y - seaLevelReference;

            if (Physics.Raycast(d.position, Vector3.down, out RaycastHit hit, groundRaycastMaxDistance))
                d.altitudeAGL = hit.distance;
            else
                d.altitudeAGL = d.altitude; // fallback — assume ground at sea level

            // ── Orientation ──────────────────────────────────────────────────
            Vector3 eulers = aircraftTransform.eulerAngles;

            // Forward projected onto the XZ plane for heading (0-360°).
            Vector3 forwardFlat = Vector3.ProjectOnPlane(aircraftTransform.forward, Vector3.up);
            d.heading = forwardFlat.sqrMagnitude > 0.0001f
                ? Mathf.Repeat(Mathf.Atan2(forwardFlat.x, forwardFlat.z) * Mathf.Rad2Deg, 360f)
                : 0f;

            // Pitch: positive = nose up (negate Unity's forward-tilt convention).
            d.pitch = WrapAngle(eulers.x);

            // Roll: positive = right wing down.
            d.roll  = WrapAngle(eulers.z);

            // Yaw mirrors heading for local use.
            d.yaw   = d.heading;

            // ── Vertical speed ───────────────────────────────────────────────
            d.verticalSpeed = d.velocity.y;

            // ── G-force ──────────────────────────────────────────────────────
            float dt = Time.fixedDeltaTime;
            if (dt > Mathf.Epsilon)
            {
                Vector3 accel = (d.velocity - _previousVelocity) / dt;
                // Subtract gravity vector so that 1 G is shown in level flight.
                d.gForce = (accel - Physics.gravity).magnitude
                           / Physics.gravity.magnitude;
            }
            _previousVelocity = d.velocity;

            // ── Throttle & fuel ──────────────────────────────────────────────
            // Sourced from FlightController when available; default passthrough.
#if SWEF_FLIGHTCONTROLLER_AVAILABLE
            if (SWEF.Flight.FlightController.Instance != null)
            {
                d.throttlePercent = SWEF.Flight.FlightController.Instance.ThrottlePercent;
                d.fuelPercent     = SWEF.Flight.FlightController.Instance.FuelPercent;
            }
#endif

            // ── Warnings ─────────────────────────────────────────────────────
            float aoa = Vector3.Angle(d.velocity.normalized, aircraftTransform.forward);
            d.isStalling  = d.speed < stallSpeed || aoa > stallAngle;
            d.isOverspeed = d.speed > overspeedThreshold;

            // ── Environment defaults (extend with weather system if available) ──
            d.temperature    = 15f - d.altitude * 0.0065f; // ISA lapse rate
            d.windSpeed      = 0f;
            d.windDirection  = 0f;
        }

        /// <summary>
        /// Converts a Unity Euler angle (0–360) into the range (−180, +180].
        /// </summary>
        private static float WrapAngle(float angle)
        {
            angle %= 360f;
            return angle > 180f ? angle - 360f : angle;
        }

        #endregion
    }
}
