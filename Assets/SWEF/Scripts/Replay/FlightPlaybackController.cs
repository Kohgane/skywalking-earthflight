using System;
using System.Collections;
using UnityEngine;

namespace SWEF.Replay
{
    /// <summary>
    /// Phase 48 — Controls playback of a saved <see cref="FlightRecording"/>.
    /// Handles play/pause/stop/seek, variable playback speed, loop mode,
    /// smooth frame interpolation, and optional ghost-aircraft spawning.
    /// </summary>
    public class FlightPlaybackController : MonoBehaviour
    {
        #region Constants

        /// <summary>Speed multipliers indexed by <see cref="PlaybackSpeed"/>.</summary>
        private static readonly float[] SpeedMultipliers = { 0.25f, 0.5f, 1f, 2f, 4f };

        private const float SeekThresholdSec = 0.001f;

        #endregion

        #region Inspector

        [Header("Ghost Aircraft")]
        [Tooltip("Prefab instantiated as the ghost aircraft during playback.  Optional.")]
        [SerializeField] private GameObject ghostAircraftPrefab;

        [Header("Playback")]
        [SerializeField] private PlaybackSpeed initialSpeed = PlaybackSpeed.Normal;
        [SerializeField] private bool          loopMode     = false;

        #endregion

        #region Events

        /// <summary>Fired each frame with the current normalised playback progress [0, 1].</summary>
        public event Action<float> OnPlaybackTimeChanged;

        /// <summary>Fired whenever the playback speed changes.</summary>
        public event Action<PlaybackSpeed> OnPlaybackSpeedChanged;

        #endregion

        #region Public Properties

        /// <summary>Whether playback is currently running.</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>Current playback position in seconds.</summary>
        public float CurrentTime { get; private set; }

        /// <summary>Total duration of the loaded recording in seconds.</summary>
        public float TotalDuration { get; private set; }

        /// <summary>Playback progress as a normalised value [0, 1].</summary>
        public float Progress => TotalDuration > 0f ? Mathf.Clamp01(CurrentTime / TotalDuration) : 0f;

        /// <summary>Active playback speed setting.</summary>
        public PlaybackSpeed Speed { get; private set; }

        /// <summary>Whether the playback will restart after reaching the end.</summary>
        public bool LoopMode
        {
            get => loopMode;
            set => loopMode = value;
        }

        /// <summary>The recording currently loaded for playback.</summary>
        public FlightRecording ActiveRecording { get; private set; }

        #endregion

        #region Private State

        private Coroutine  _playbackCoroutine;
        private GameObject _ghostInstance;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Speed = initialSpeed;
        }

        private void OnDestroy()
        {
            StopPlayback();
            DestroyGhost();
        }

        #endregion

        #region Public API

        /// <summary>Loads <paramref name="recording"/> and prepares it for playback.</summary>
        public void LoadRecording(FlightRecording recording)
        {
            if (recording == null) return;
            StopPlayback();

            ActiveRecording = recording;
            TotalDuration   = recording.duration;
            CurrentTime     = 0f;

            SpawnGhost(recording);

            FlightRecorderManager.Instance?.NotifyPlaybackStarted(recording);
        }

        /// <summary>Starts or resumes playback from the current position.</summary>
        public void Play()
        {
            if (ActiveRecording == null || IsPlaying) return;
            IsPlaying         = true;
            _playbackCoroutine = StartCoroutine(PlaybackLoop());
        }

        /// <summary>Pauses playback without resetting the current position.</summary>
        public void Pause()
        {
            if (!IsPlaying) return;
            IsPlaying = false;
            if (_playbackCoroutine != null)
            {
                StopCoroutine(_playbackCoroutine);
                _playbackCoroutine = null;
            }
        }

        /// <summary>Stops playback and resets the position to zero.</summary>
        public void StopPlayback()
        {
            Pause();
            CurrentTime = 0f;
            OnPlaybackTimeChanged?.Invoke(0f);
            DestroyGhost();

            FlightRecorderManager.Instance?.NotifyPlaybackFinished();
        }

        /// <summary>Jumps to <paramref name="time"/> seconds into the recording.</summary>
        public void Seek(float time)
        {
            if (ActiveRecording == null) return;
            CurrentTime = Mathf.Clamp(time, 0f, TotalDuration);
            ApplyFrameAtCurrentTime();
            OnPlaybackTimeChanged?.Invoke(Progress);
        }

        /// <summary>Seeks to a normalised position <paramref name="t"/> in [0, 1].</summary>
        public void SeekNormalised(float t) => Seek(t * TotalDuration);

        /// <summary>Changes the playback speed.</summary>
        public void SetSpeed(PlaybackSpeed speed)
        {
            Speed = speed;
            OnPlaybackSpeedChanged?.Invoke(speed);
        }

        #endregion

        #region Private — Playback Loop

        private IEnumerator PlaybackLoop()
        {
            while (IsPlaying)
            {
                float delta = Time.deltaTime * SpeedMultipliers[(int)Speed];
                CurrentTime += delta;

                if (CurrentTime >= TotalDuration)
                {
                    if (loopMode)
                    {
                        CurrentTime = 0f;
                    }
                    else
                    {
                        CurrentTime = TotalDuration;
                        ApplyFrameAtCurrentTime();
                        OnPlaybackTimeChanged?.Invoke(1f);
                        IsPlaying = false;
                        FlightRecorderManager.Instance?.NotifyPlaybackFinished();
                        yield break;
                    }
                }

                ApplyFrameAtCurrentTime();
                OnPlaybackTimeChanged?.Invoke(Progress);
                yield return null;
            }
        }

        #endregion

        #region Private — Frame Interpolation

        private void ApplyFrameAtCurrentTime()
        {
            if (ActiveRecording == null || ActiveRecording.FrameCount < 2) return;

            int idx = ActiveRecording.FindFrameIndex(CurrentTime);
            idx = Mathf.Clamp(idx, 0, ActiveRecording.FrameCount - 2);

            FlightFrame a = ActiveRecording.frames[idx];
            FlightFrame b = ActiveRecording.frames[idx + 1];

            float span = b.timestamp - a.timestamp;
            float t    = span > SeekThresholdSec
                       ? Mathf.InverseLerp(a.timestamp, b.timestamp, CurrentTime)
                       : 0f;

            Vector3    pos = Vector3.Lerp(a.position, b.position, t);
            Quaternion rot = Quaternion.Slerp(a.rotation, b.rotation, t);

            if (_ghostInstance != null)
            {
                _ghostInstance.transform.position = pos;
                _ghostInstance.transform.rotation = rot;
            }
        }

        #endregion

        #region Private — Ghost Aircraft

        private void SpawnGhost(FlightRecording recording)
        {
            DestroyGhost();
            if (ghostAircraftPrefab == null) return;

            _ghostInstance = Instantiate(ghostAircraftPrefab);
            _ghostInstance.name = $"Ghost_{recording.aircraftType}";

            // Disable any physics or input components on the ghost.
            foreach (var rb in _ghostInstance.GetComponentsInChildren<Rigidbody>())
                rb.isKinematic = true;

            var ghost = _ghostInstance.GetComponent<ReplayGhostAircraft>();
            if (ghost != null) ghost.Initialise(recording);
        }

        private void DestroyGhost()
        {
            if (_ghostInstance == null) return;
            Destroy(_ghostInstance);
            _ghostInstance = null;
        }

        #endregion
    }
}
