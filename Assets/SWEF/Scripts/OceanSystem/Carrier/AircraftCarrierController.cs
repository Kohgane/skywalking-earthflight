// AircraftCarrierController.cs — Phase 117: Advanced Ocean & Maritime System
// Aircraft carrier simulation: deck movement, catapult, arrestor wire.
// Namespace: SWEF.OceanSystem

#if SWEF_CARRIER_AVAILABLE || !UNITY_EDITOR
using System;
using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Root controller for an aircraft carrier asset.
    /// Manages deck motion in response to sea state, exposes catapult and
    /// arrestor wire sub-systems, and reports position/heading to navigation aids.
    /// </summary>
    public class AircraftCarrierController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Configuration")]
        [SerializeField] private OceanSystemConfig config;

        [Header("Sub-Systems")]
        [SerializeField] private CatapultLaunchSystem[] catapults;
        [SerializeField] private ArrestorWireSystem     arrestorWires;
        [SerializeField] private CarrierDeckManager     deckManager;
        [SerializeField] private CarrierNavigationSystem navSystem;

        [Header("Deck Motion")]
        [Tooltip("Maximum pitch angle (degrees) due to sea state.")]
        [SerializeField] private float maxPitchDeg = 3f;
        [Tooltip("Maximum roll angle (degrees) due to sea state.")]
        [SerializeField] private float maxRollDeg  = 5f;
        [Tooltip("Deck motion response speed.")]
        [SerializeField] private float deckMotionSpeed = 0.5f;

        [Header("Navigation")]
        [SerializeField] private float headingDeg   = 0f;
        [SerializeField] private float speedKnots   = 15f;

        // ── Private state ─────────────────────────────────────────────────────────

        private float _deckPitch;
        private float _deckRoll;
        private float _deckMotionTime;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when all catapults are loaded and ready.</summary>
        public event Action OnDeckReady;

        // ── Public Properties ─────────────────────────────────────────────────────

        /// <summary>Current carrier heading in degrees.</summary>
        public float HeadingDeg => headingDeg;

        /// <summary>Current carrier speed in knots.</summary>
        public float SpeedKnots => speedKnots;

        /// <summary>Current deck pitch angle in degrees.</summary>
        public float DeckPitch => _deckPitch;

        /// <summary>Current deck roll angle in degrees.</summary>
        public float DeckRoll => _deckRoll;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Update()
        {
            MoveCarrier();
            SimulateDeckMotion();
        }

        // ── Private Logic ─────────────────────────────────────────────────────────

        private void MoveCarrier()
        {
            float headingRad = headingDeg * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Sin(headingRad), 0f, Mathf.Cos(headingRad));
            transform.position += dir * speedKnots * 0.5144f * Time.deltaTime;
        }

        private void SimulateDeckMotion()
        {
            var mgr = OceanSystemManager.Instance;
            float seaFactor = mgr != null ? GetSeaFactor(mgr.CurrentSeaState) : 0f;

            _deckMotionTime += Time.deltaTime * deckMotionSpeed;
            float targetPitch = Mathf.Sin(_deckMotionTime * 0.7f) * maxPitchDeg * seaFactor;
            float targetRoll  = Mathf.Sin(_deckMotionTime * 0.5f + 1.3f) * maxRollDeg * seaFactor;

            _deckPitch = Mathf.Lerp(_deckPitch, targetPitch, Time.deltaTime * 2f);
            _deckRoll  = Mathf.Lerp(_deckRoll,  targetRoll,  Time.deltaTime * 2f);

            transform.localRotation = Quaternion.Euler(_deckPitch, headingDeg, _deckRoll);
        }

        private static float GetSeaFactor(SeaState state)
        {
            return state switch
            {
                SeaState.Calm      => 0.1f,
                SeaState.Slight    => 0.2f,
                SeaState.Moderate  => 0.4f,
                SeaState.Rough     => 0.7f,
                SeaState.VeryRough => 0.9f,
                SeaState.HighSeas  => 1.0f,
                _ => 0f
            };
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Sets the carrier's course and speed.</summary>
        public void SetCourse(float headingDegrees, float speedKn)
        {
            headingDeg = headingDegrees;
            speedKnots = speedKn;
        }
    }
}
#endif
