// EngineStartupSequence.cs — Phase 118: Spatial Audio & 3D Soundscape
// Engine start/shutdown audio sequence: starter motor, ignition, spool up/down.
// Namespace: SWEF.SpatialAudio

using System.Collections;
using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Plays a sequential engine startup or shutdown audio event:
    /// starter motor → ignition → spool up → running, and the reverse for shutdown.
    /// </summary>
    public class EngineStartupSequence : MonoBehaviour
    {
        // ── Sequence State ────────────────────────────────────────────────────────

        /// <summary>Possible states of the engine startup/shutdown sequence.</summary>
        public enum SequenceState
        {
            /// <summary>Engine is fully off and silent.</summary>
            Off,
            /// <summary>Starter motor cranking before ignition.</summary>
            StarterMotor,
            /// <summary>Ignition spark and first combustion.</summary>
            Ignition,
            /// <summary>Engine spooling up to idle RPM.</summary>
            SpoolUp,
            /// <summary>Engine at stable idle/running state.</summary>
            Running,
            /// <summary>Engine winding down after shutdown.</summary>
            SpoolDown,
            /// <summary>Residual whine after full shutdown.</summary>
            ShutdownWhine
        }

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Sequence Clips")]
        [SerializeField] private AudioClip starterMotorClip;
        [SerializeField] private AudioClip ignitionClip;
        [SerializeField] private AudioClip spoolUpClip;
        [SerializeField] private AudioClip spoolDownClip;
        [SerializeField] private AudioClip shutdownWhineClip;

        [Header("Config")]
        [SerializeField] private SpatialAudioConfig config;

        [Header("Playback")]
        [SerializeField] private AudioSource sequenceSource;

        // ── State ─────────────────────────────────────────────────────────────────

        /// <summary>Current state of the sequence.</summary>
        public SequenceState CurrentState { get; private set; } = SequenceState.Off;

        private Coroutine _activeSequence;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Triggers the engine startup audio sequence.</summary>
        public void StartEngine()
        {
            if (CurrentState != SequenceState.Off) return;
            if (_activeSequence != null) StopCoroutine(_activeSequence);
            _activeSequence = StartCoroutine(StartupCoroutine());
        }

        /// <summary>Triggers the engine shutdown audio sequence.</summary>
        public void ShutdownEngine()
        {
            if (CurrentState == SequenceState.Off) return;
            if (_activeSequence != null) StopCoroutine(_activeSequence);
            _activeSequence = StartCoroutine(ShutdownCoroutine());
        }

        // ── Coroutines ────────────────────────────────────────────────────────────

        private IEnumerator StartupCoroutine()
        {
            float baseDuration = config != null ? config.engineStartupDuration : 8f;

            CurrentState = SequenceState.StarterMotor;
            PlayClip(starterMotorClip);
            yield return new WaitForSeconds(baseDuration * 0.2f);

            CurrentState = SequenceState.Ignition;
            PlayClip(ignitionClip);
            yield return new WaitForSeconds(baseDuration * 0.15f);

            CurrentState = SequenceState.SpoolUp;
            PlayClip(spoolUpClip);
            yield return new WaitForSeconds(baseDuration * 0.65f);

            CurrentState = SequenceState.Running;
        }

        private IEnumerator ShutdownCoroutine()
        {
            float baseDuration = config != null ? config.engineStartupDuration : 8f;

            CurrentState = SequenceState.SpoolDown;
            PlayClip(spoolDownClip);
            yield return new WaitForSeconds(baseDuration * 0.5f);

            CurrentState = SequenceState.ShutdownWhine;
            PlayClip(shutdownWhineClip);
            yield return new WaitForSeconds(baseDuration * 0.5f);

            CurrentState = SequenceState.Off;
        }

        private void PlayClip(AudioClip clip)
        {
            if (sequenceSource == null || clip == null) return;
            sequenceSource.PlayOneShot(clip);
        }
    }
}
