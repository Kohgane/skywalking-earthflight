// EmergencyDitchingSystem.cs — Phase 117: Advanced Ocean & Maritime System
// Emergency water landing: structural stress, passenger evacuation, life raft.
// Namespace: SWEF.OceanSystem

using System;
using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Handles emergency water ditching events.
    /// Monitors structural stress at water impact, triggers passenger evacuation
    /// animation and life raft deployment.
    /// </summary>
    public class EmergencyDitchingSystem : MonoBehaviour
    {
        // ── Ditching State ────────────────────────────────────────────────────────

        /// <summary>Phases of an emergency ditching event.</summary>
        public enum DitchingState
        {
            /// <summary>Normal flight — no emergency.</summary>
            Normal,
            /// <summary>Ditching is imminent, crew briefing in progress.</summary>
            Imminent,
            /// <summary>Aircraft is contacting the water surface.</summary>
            Impact,
            /// <summary>Aircraft has stopped on water, evacuation in progress.</summary>
            Evacuation,
            /// <summary>Survivors in water, awaiting rescue.</summary>
            Survived,
            /// <summary>Impact was unsurvivable.</summary>
            Fatal
        }

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Structural Thresholds")]
        [Tooltip("Impact vertical speed (m/s) above which structural failure occurs.")]
        [SerializeField] private float fatalVerticalSpeed = 15f;
        [Tooltip("Impact vertical speed (m/s) above which structural damage is severe.")]
        [SerializeField] private float severeVerticalSpeed = 8f;

        [Header("Life Raft")]
        [SerializeField] private GameObject lifeRaftPrefab;
        [SerializeField] private Transform  lifeRaftDeployPoint;

        [Header("Audio")]
        [SerializeField] private AudioSource impactAudio;
        [SerializeField] private AudioClip   ditchWarningClip;

        // ── Private state ─────────────────────────────────────────────────────────

        private DitchingState _state = DitchingState.Normal;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when ditching state changes.</summary>
        public event Action<DitchingState> OnStateChanged;

        /// <summary>Raised when life raft is deployed.</summary>
        public event Action OnLifeRaftDeployed;

        // ── Public Properties ─────────────────────────────────────────────────────

        /// <summary>Current ditching state.</summary>
        public DitchingState State => _state;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Triggers the ditching sequence with the given impact speeds.
        /// Called by <see cref="WaterLandingController"/> when emergency ditching is detected.
        /// </summary>
        public void TriggerDitching(float verticalSpeedMs, float horizontalSpeedMs)
        {
            if (_state != DitchingState.Normal && _state != DitchingState.Imminent) return;

            SetState(DitchingState.Impact);

            if (verticalSpeedMs >= fatalVerticalSpeed)
            {
                SetState(DitchingState.Fatal);
                return;
            }

            if (impactAudio != null)
            {
                impactAudio.pitch  = Mathf.Lerp(0.8f, 1.5f, verticalSpeedMs / fatalVerticalSpeed);
                impactAudio.Play();
            }

            // Small delay before evacuation in a real scenario; here we trigger directly
            SetState(DitchingState.Evacuation);
            DeployLifeRaft();
        }

        /// <summary>Signals that an imminent ditching is expected (crew briefing).</summary>
        public void AnnounceImminent()
        {
            if (_state != DitchingState.Normal) return;
            SetState(DitchingState.Imminent);
            if (impactAudio != null && ditchWarningClip != null)
                impactAudio.PlayOneShot(ditchWarningClip);
        }

        /// <summary>Marks the evacuation as complete and survivors in water.</summary>
        public void CompleteEvacuation()
        {
            if (_state != DitchingState.Evacuation) return;
            SetState(DitchingState.Survived);
        }

        /// <summary>Resets the ditching system to normal state.</summary>
        public void Reset()
        {
            SetState(DitchingState.Normal);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void SetState(DitchingState newState)
        {
            _state = newState;
            OnStateChanged?.Invoke(newState);
        }

        private void DeployLifeRaft()
        {
            if (lifeRaftPrefab != null && lifeRaftDeployPoint != null)
            {
                Instantiate(lifeRaftPrefab, lifeRaftDeployPoint.position, lifeRaftDeployPoint.rotation);
                OnLifeRaftDeployed?.Invoke();
            }
        }
    }
}
