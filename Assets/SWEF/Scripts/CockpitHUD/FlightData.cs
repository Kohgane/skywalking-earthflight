// FlightData.cs — SWEF Cockpit Instrument & HUD Dashboard System (Phase 65)
using UnityEngine;

namespace SWEF.CockpitHUD
{
    /// <summary>
    /// Phase 65 — Data container passed to all HUD instruments every frame.
    ///
    /// <para>Populated by <see cref="FlightDataProvider"/> and consumed by every
    /// <see cref="HUDInstrument"/> subclass via <c>UpdateInstrument</c>.</para>
    /// </summary>
    public class FlightData
    {
        // ── Altitude ─────────────────────────────────────────────────────────

        /// <summary>Altitude above sea level in meters.</summary>
        public float altitude;

        /// <summary>Altitude above ground level in meters (via downward raycast).</summary>
        public float altitudeAGL;

        // ── Speed ─────────────────────────────────────────────────────────────

        /// <summary>Airspeed in meters per second.</summary>
        public float speed;

        /// <summary>Airspeed in knots.</summary>
        public float speedKnots;

        /// <summary>Airspeed as a Mach number (speed / speed-of-sound).</summary>
        public float speedMach;

        // ── Vertical Speed ────────────────────────────────────────────────────

        /// <summary>Vertical speed in m/s. Positive = climbing, negative = descending.</summary>
        public float verticalSpeed;

        // ── Orientation ───────────────────────────────────────────────────────

        /// <summary>Magnetic heading in degrees (0–360).</summary>
        public float heading;

        /// <summary>Pitch angle in degrees (−90 to +90; positive = nose up).</summary>
        public float pitch;

        /// <summary>Roll / bank angle in degrees (−180 to +180; positive = right wing down).</summary>
        public float roll;

        /// <summary>Yaw angle in degrees.</summary>
        public float yaw;

        // ── Performance ───────────────────────────────────────────────────────

        /// <summary>Current G-force experienced by the aircraft.</summary>
        public float gForce;

        /// <summary>Throttle position as a normalized value (0 = idle, 1 = full throttle).</summary>
        public float throttlePercent;

        /// <summary>Remaining fuel as a normalized value (0 = empty, 1 = full).</summary>
        public float fuelPercent;

        // ── Physics ───────────────────────────────────────────────────────────

        /// <summary>Current velocity vector in world space (m/s).</summary>
        public Vector3 velocity;

        /// <summary>Current world-space position of the aircraft.</summary>
        public Vector3 position;

        // ── Warning Flags ─────────────────────────────────────────────────────

        /// <summary><c>true</c> when the aircraft is in an aerodynamic stall condition.</summary>
        public bool isStalling;

        /// <summary><c>true</c> when airspeed exceeds the structural overspeed limit.</summary>
        public bool isOverspeed;

        // ── Environment ───────────────────────────────────────────────────────

        /// <summary>Outside air temperature in degrees Celsius.</summary>
        public float temperature;

        /// <summary>Ambient wind speed in m/s.</summary>
        public float windSpeed;

        /// <summary>Ambient wind direction in degrees (meteorological convention, 0 = from north).</summary>
        public float windDirection;
    }
}
