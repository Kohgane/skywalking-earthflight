// MultiEngineAudioController.cs — Phase 118: Spatial Audio & 3D Soundscape
// Per-engine audio source management for multi-engine aircraft.
// Namespace: SWEF.SpatialAudio

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Manages individual <see cref="EngineSoundController"/> instances for each
    /// engine on a multi-engine aircraft, supporting asymmetric thrust audio and
    /// engine failure sounds.
    /// </summary>
    public class MultiEngineAudioController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Engine Controllers")]
        [Tooltip("Per-engine sound controllers (one entry per engine).")]
        [SerializeField] private List<EngineSoundController> engineControllers = new List<EngineSoundController>();

        [Header("Failure Audio")]
        [SerializeField] private AudioSource engineFailureSource;
        [SerializeField] private AudioClip   engineFailureClip;

        // ── State ─────────────────────────────────────────────────────────────────

        private readonly HashSet<int> _failedEngines = new HashSet<int>();

        /// <summary>Total number of engines managed.</summary>
        public int EngineCount => engineControllers.Count;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates audio for a specific engine by index.
        /// </summary>
        /// <param name="engineIndex">Zero-based engine index.</param>
        /// <param name="throttle">Throttle position (0–1).</param>
        /// <param name="rpm">Current RPM.</param>
        /// <param name="afterburner">Whether afterburner is active.</param>
        public void UpdateEngine(int engineIndex, float throttle, float rpm, bool afterburner = false)
        {
            if (!IsValidIndex(engineIndex)) return;
            if (_failedEngines.Contains(engineIndex))
            {
                engineControllers[engineIndex].MuteAll();
                return;
            }
            engineControllers[engineIndex].UpdateEngineAudio(throttle, rpm, afterburner);
        }

        /// <summary>
        /// Updates all engines with the same throttle/RPM values (symmetric thrust).
        /// </summary>
        public void UpdateAllEngines(float throttle, float rpm, bool afterburner = false)
        {
            for (int i = 0; i < engineControllers.Count; i++)
                UpdateEngine(i, throttle, rpm, afterburner);
        }

        /// <summary>
        /// Triggers engine failure audio for the specified engine.
        /// </summary>
        public void TriggerEngineFailure(int engineIndex)
        {
            if (!IsValidIndex(engineIndex)) return;
            _failedEngines.Add(engineIndex);
            engineControllers[engineIndex].MuteAll();

            if (engineFailureSource != null && engineFailureClip != null)
                engineFailureSource.PlayOneShot(engineFailureClip);
        }

        /// <summary>Restores a previously failed engine (after repair/restart).</summary>
        public void RestoreEngine(int engineIndex)
        {
            _failedEngines.Remove(engineIndex);
        }

        /// <summary>Returns whether the given engine has failed.</summary>
        public bool IsEngineFailed(int engineIndex) => _failedEngines.Contains(engineIndex);

        // ── Private ───────────────────────────────────────────────────────────────

        private bool IsValidIndex(int idx) =>
            idx >= 0 && idx < engineControllers.Count && engineControllers[idx] != null;
    }
}
