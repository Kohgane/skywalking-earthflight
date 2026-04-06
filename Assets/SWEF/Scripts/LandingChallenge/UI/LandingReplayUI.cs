// LandingReplayUI.cs — Phase 120: Precision Landing Challenge System
// Replay viewer: camera controls, speed controls, overlay data, save/share.
// Namespace: SWEF.LandingChallenge

using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Replay viewer UI controller.
    /// Provides play/pause/rewind controls, camera angle switching,
    /// playback speed control, data overlay, and save/share functionality.
    /// </summary>
    public class LandingReplayUI : MonoBehaviour
    {
        // ── Camera Mode ───────────────────────────────────────────────────────

        public enum ReplayCameraMode { Chase, Cockpit, Tower, External, SlowMotion }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Replay Controller")]
        [SerializeField] private ReplayRecorder recorder;

        [Header("Slow Motion")]
        [SerializeField] [Range(0.1f, 1f)] private float slowMotionSpeed = 0.25f;

        // ── State ─────────────────────────────────────────────────────────────

        private bool             _isPlaying;
        private ReplayCameraMode _cameraMode = ReplayCameraMode.Chase;
        private float            _currentPlaybackSpeed = 1f;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Raised when the player requests to share the replay.</summary>
        public event System.Action OnShareRequested;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Active camera angle mode.</summary>
        public ReplayCameraMode CameraMode => _cameraMode;

        /// <summary>Whether the replay is currently playing.</summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>Current playback speed multiplier.</summary>
        public float PlaybackSpeed => _currentPlaybackSpeed;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Start replay playback.</summary>
        public void Play()
        {
            if (recorder == null) return;
            recorder.PlaybackSpeed = _currentPlaybackSpeed;
            recorder.StartPlayback();
            _isPlaying = true;
        }

        /// <summary>Pause playback.</summary>
        public void Pause()
        {
            _isPlaying = false;
            if (recorder != null) recorder.PlaybackSpeed = 0f;
        }

        /// <summary>Rewind to the start of the replay.</summary>
        public void Rewind()
        {
            if (recorder != null)
            {
                recorder.StopPlayback();
                recorder.StartPlayback();
            }
        }

        /// <summary>Switch to a different camera angle.</summary>
        public void SetCameraMode(ReplayCameraMode mode) => _cameraMode = mode;

        /// <summary>Set playback speed (0.1x – 2x).</summary>
        public void SetPlaybackSpeed(float speed)
        {
            _currentPlaybackSpeed = Mathf.Clamp(speed, 0.1f, 2f);
            if (recorder != null) recorder.PlaybackSpeed = _currentPlaybackSpeed;
        }

        /// <summary>Activate slow-motion playback.</summary>
        public void ActivateSlowMotion() => SetPlaybackSpeed(slowMotionSpeed);

        /// <summary>Restore normal playback speed.</summary>
        public void ResetSpeed() => SetPlaybackSpeed(1f);

        /// <summary>Request to share the current replay.</summary>
        public void Share() => OnShareRequested?.Invoke();
    }
}
