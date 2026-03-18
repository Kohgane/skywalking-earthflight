using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Replay
{
    /// <summary>
    /// Drives a semi-transparent ghost aircraft along a saved replay, enabling players
    /// to race against their own previous runs. Performs binary-search frame
    /// interpolation in <c>Update</c> for smooth, frame-rate-independent playback.
    /// </summary>
    public class GhostRacer : MonoBehaviour
    {
        // ── State machine ─────────────────────────────────────────────────────────
        /// <summary>Possible states of the ghost racer.</summary>
        public enum GhostState { Idle, Loading, Racing, Paused, Finished }

        /// <summary>Current state of the ghost racer.</summary>
        public GhostState CurrentState { get; private set; } = GhostState.Idle;

        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Ghost Visuals")]
        [SerializeField] private GameObject ghostPrefab;
        [SerializeField] private Transform  ghostInstance;
        [SerializeField] private Color      ghostColor        = new Color(0.3f, 0.8f, 1f, 0.5f);
        [SerializeField] private float      ghostTrailLength  = 5.0f;

        [Header("Playback")]
        [SerializeField] private float playbackSpeed = 1.0f;

        [Header("Live Player Source (optional)")]
        [SerializeField] private Flight.FlightController playerFlight;
        [SerializeField] private Flight.AltitudeController playerAltitude;

        // ── Private state ─────────────────────────────────────────────────────────
        private ReplayData _activeReplay;
        private float      _elapsedTime;
        private Renderer[] _ghostRenderers;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when a ghost race begins.</summary>
        public event Action OnRaceStarted;

        /// <summary>Fired when the replay reaches its end.</summary>
        public event Action OnRaceFinished;

        /// <summary>Fired whenever the time-delta comparison value changes.</summary>
        public event Action<float> OnTimeDeltaChanged;

        // ── Live comparison stats ─────────────────────────────────────────────────
        /// <summary>
        /// Seconds the local player is ahead of (positive) or behind (negative)
        /// the ghost at the same altitude.
        /// </summary>
        public float TimeDelta     { get; private set; }

        /// <summary>Altitude difference (player − ghost) in metres at the same elapsed time.</summary>
        public float AltitudeDelta { get; private set; }

        /// <summary>Speed difference (player − ghost) in m/s at the same elapsed time.</summary>
        public float SpeedDelta    { get; private set; }

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (playerFlight == null)
                playerFlight = FindFirstObjectByType<Flight.FlightController>();
            if (playerAltitude == null)
                playerAltitude = FindFirstObjectByType<Flight.AltitudeController>();
        }

        private void Update()
        {
            if (CurrentState != GhostState.Racing) return;

            _elapsedTime += Time.deltaTime * Mathf.Max(playbackSpeed, 0.001f);

            float duration = _activeReplay.GetDuration();
            if (_elapsedTime >= duration)
            {
                SnapToFrame(_activeReplay.frames[_activeReplay.frames.Count - 1]);
                FinishRace();
                return;
            }

            InterpolateGhost(_elapsedTime);
            UpdateComparisonStats(_elapsedTime);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Spawns the ghost and begins playback of <paramref name="replay"/>.
        /// If <see cref="ghostInstance"/> is null, a new instance is created from
        /// <see cref="ghostPrefab"/>.
        /// </summary>
        public void StartRace(ReplayData replay)
        {
            if (replay == null || replay.frames.Count < 2)
            {
                Debug.LogWarning("[SWEF] GhostRacer: Cannot start race — replay is null or has fewer than 2 frames.");
                return;
            }

            CurrentState  = GhostState.Loading;
            _activeReplay = replay;
            _elapsedTime  = 0f;

            SpawnGhost();
            ApplyGhostMaterial();

            CurrentState = GhostState.Racing;
            Debug.Log($"[SWEF] GhostRacer: Race started against '{replay.playerName}'.");
            OnRaceStarted?.Invoke();
        }

        /// <summary>Stops and hides the ghost, returning to <see cref="GhostState.Idle"/>.</summary>
        public void StopRace()
        {
            CurrentState = GhostState.Idle;
            if (ghostInstance != null) ghostInstance.gameObject.SetActive(false);
            _activeReplay = null;
            _elapsedTime  = 0f;
            Debug.Log("[SWEF] GhostRacer: Race stopped.");
        }

        /// <summary>Pauses ghost movement without hiding it.</summary>
        public void PauseRace()
        {
            if (CurrentState != GhostState.Racing) return;
            CurrentState = GhostState.Paused;
            Debug.Log("[SWEF] GhostRacer: Race paused.");
        }

        /// <summary>Resumes a paused race.</summary>
        public void ResumeRace()
        {
            if (CurrentState != GhostState.Paused) return;
            CurrentState = GhostState.Racing;
            Debug.Log("[SWEF] GhostRacer: Race resumed.");
        }

        /// <summary>Sets the playback speed multiplier, clamped to [0.25, 4.0].</summary>
        public void SetPlaybackSpeed(float speed)
        {
            playbackSpeed = Mathf.Clamp(speed, 0.25f, 4.0f);
        }

        /// <summary>Returns playback progress as a 0–1 fraction.</summary>
        public float PlaybackProgress01
        {
            get
            {
                if (_activeReplay == null) return 0f;
                float dur = _activeReplay.GetDuration();
                return dur > 0f ? Mathf.Clamp01(_elapsedTime / dur) : 0f;
            }
        }

        /// <summary>Total duration of the active replay in seconds, or 0 when idle.</summary>
        public float ReplayDuration => _activeReplay != null ? _activeReplay.GetDuration() : 0f;

        // ── Private helpers ───────────────────────────────────────────────────────

        private void SpawnGhost()
        {
            if (ghostInstance != null)
            {
                ghostInstance.gameObject.SetActive(true);
                return;
            }

            if (ghostPrefab == null)
            {
                Debug.LogWarning("[SWEF] GhostRacer: ghostPrefab is not assigned; ghost will be invisible.");
                return;
            }

            ghostInstance = Instantiate(ghostPrefab).transform;
            ghostInstance.name = "GhostRacer_Instance";

            // Add a TrailRenderer for a visual trail
            var trail = ghostInstance.GetComponent<TrailRenderer>();
            if (trail == null) trail = ghostInstance.gameObject.AddComponent<TrailRenderer>();
            trail.time        = ghostTrailLength;
            trail.startColor  = ghostColor;
            trail.endColor    = new Color(ghostColor.r, ghostColor.g, ghostColor.b, 0f);
            trail.startWidth  = 1f;
            trail.endWidth    = 0f;

            _ghostRenderers = ghostInstance.GetComponentsInChildren<Renderer>();
        }

        private void ApplyGhostMaterial()
        {
            if (_ghostRenderers == null) return;
            foreach (var r in _ghostRenderers)
            {
                foreach (var mat in r.materials)
                {
                    // Switch to a transparent rendering mode if available
                    mat.SetFloat("_Mode",      3f);
                    mat.SetInt("_SrcBlend",    (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend",    (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite",      0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 3000;
                    mat.color       = ghostColor;
                }
            }
        }

        private void InterpolateGhost(float t)
        {
            if (ghostInstance == null) return;

            var frames = _activeReplay.frames;
            int lo = BinarySearchFrame(frames, t);
            int hi = Mathf.Min(lo + 1, frames.Count - 1);

            var fLo = frames[lo];
            var fHi = frames[hi];

            float span  = fHi.time - fLo.time;
            float blend = span > 0f ? (t - fLo.time) / span : 1f;

            ghostInstance.position = Vector3.Lerp(fLo.Position, fHi.Position, blend);
            ghostInstance.rotation = Quaternion.Slerp(fLo.Rotation, fHi.Rotation, blend);
        }

        private void SnapToFrame(ReplayFrame frame)
        {
            if (ghostInstance == null) return;
            ghostInstance.position = frame.Position;
            ghostInstance.rotation = frame.Rotation;
        }

        private void UpdateComparisonStats(float t)
        {
            var frames = _activeReplay.frames;
            int idx    = BinarySearchFrame(frames, t);
            var gFrame = frames[idx];

            float playerAlt   = playerAltitude != null ? playerAltitude.CurrentAltitudeMeters : 0f;
            float playerSpeed = playerFlight   != null ? playerFlight.CurrentSpeedMps          : 0f;

            AltitudeDelta = playerAlt   - gFrame.altitude;
            SpeedDelta    = playerSpeed - gFrame.speed;

            // Time delta: how much sooner/later the player reaches the ghost's current altitude
            float newTimeDelta = ComputeTimeDelta(playerAlt, t, frames);
            if (Math.Abs(newTimeDelta - TimeDelta) > 0.05f)
            {
                TimeDelta = newTimeDelta;
                OnTimeDeltaChanged?.Invoke(TimeDelta);
            }
        }

        /// <summary>
        /// Estimates how far ahead (positive) or behind (negative) the player is
        /// compared to the ghost at the same altitude. Finds the ghost frame with
        /// the closest altitude to the player's current altitude.
        /// </summary>
        private float ComputeTimeDelta(float playerAlt, float currentTime, List<ReplayFrame> frames)
        {
            int bestIdx     = 0;
            float bestDelta = float.MaxValue;
            for (int i = 0; i < frames.Count; i++)
            {
                float delta = Mathf.Abs(frames[i].altitude - playerAlt);
                if (delta < bestDelta)
                {
                    bestDelta = delta;
                    bestIdx   = i;
                }
            }
            return currentTime - frames[bestIdx].time;
        }

        private void FinishRace()
        {
            CurrentState = GhostState.Finished;
            Debug.Log("[SWEF] GhostRacer: Race finished.");
            OnRaceFinished?.Invoke();

            // Phase 17 — Achievement
            Achievement.AchievementManager.Instance?.TryUnlock("first_ghost_race");
        }

        /// <summary>Binary search returning the index of the last frame with time ≤ <paramref name="t"/>.</summary>
        private static int BinarySearchFrame(List<ReplayFrame> frames, float t)
        {
            int lo = 0, hi = frames.Count - 1;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (frames[mid].time <= t) lo = mid;
                else                       hi = mid - 1;
            }
            return lo;
        }
    }
}
