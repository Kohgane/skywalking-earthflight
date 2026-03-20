using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Cinema
{
    /// <summary>
    /// Defines a spline-based cinematic camera path composed of <see cref="CameraWaypoint"/> nodes
    /// and plays it back with Catmull-Rom or linear interpolation.
    /// Integrates with <see cref="SWEF.Flight.CameraController"/> to hand off / return camera control.
    /// Phase 18 — Cinematic Camera.
    /// </summary>
    public class CinematicCameraPath : MonoBehaviour
    {
        // ── Waypoint ──────────────────────────────────────────────────────────────
        [System.Serializable]
        public class CameraWaypoint
        {
            public Vector3        position;
            public Quaternion     rotation     = Quaternion.identity;
            public float          fov          = 60f;
            public float          timeAtWaypoint = 0f;
            public float          holdDuration = 0f;
            public AnimationCurve speedCurve   = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }

        // ── Enums ─────────────────────────────────────────────────────────────────
        public enum PlaybackState { Stopped, Playing, Paused }
        public enum LoopMode      { Once, Loop, PingPong }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Path")]
        [SerializeField] private List<CameraWaypoint> waypoints = new List<CameraWaypoint>();

        [Header("Interpolation")]
        [SerializeField] private bool  useCatmullRom             = true;
        [SerializeField] private float defaultTransitionDuration = 3.0f;

        [Header("Playback")]
        [SerializeField] private LoopMode loopMode = LoopMode.Once;
        [SerializeField] private float    playbackSpeed = 1.0f;

        [Header("Target")]
        [SerializeField] private Transform cameraTarget;

        // ── State ─────────────────────────────────────────────────────────────────
        public PlaybackState CurrentState { get; private set; } = PlaybackState.Stopped;

        /// <summary>Number of waypoints in the path.</summary>
        public int WaypointCount => waypoints.Count;

        /// <summary>Current playback position in seconds along the path.</summary>
        public float CurrentPlaybackTime => _playbackTime;

        private float _playbackTime;
        private bool  _pingPongForward = true;
        private float _basePlaybackSpeed;

        // ── Events ────────────────────────────────────────────────────────────────
        public event Action     OnPlaybackStarted;
        public event Action     OnPlaybackCompleted;
        public event Action<int> OnWaypointReached;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (cameraTarget == null && Camera.main != null)
                cameraTarget = Camera.main.transform;
        }

        private void Update()
        {
            if (CurrentState != PlaybackState.Playing) return;
            AdvancePlayback(Time.deltaTime * playbackSpeed);
        }

        // ── Playback API ──────────────────────────────────────────────────────────
        /// <summary>Starts playback from the beginning.</summary>
        public void Play()
        {
            if (waypoints.Count < 2)
            {
                Debug.LogWarning("[SWEF] CinematicCameraPath: Need at least 2 waypoints to play.");
                return;
            }

            _playbackTime    = 0f;
            _pingPongForward = true;
            _basePlaybackSpeed = Mathf.Abs(playbackSpeed);
            CurrentState    = PlaybackState.Playing;

            // Hand camera control to cinematic system
            var camCtrl = FindFirstObjectByType<Flight.CameraController>();
            if (camCtrl != null) camCtrl.EnableCinematicOverride();

            Debug.Log("[SWEF] CinematicCameraPath: Playback started.");
            OnPlaybackStarted?.Invoke();
        }

        /// <summary>Pauses playback at the current position.</summary>
        public void Pause()
        {
            if (CurrentState == PlaybackState.Playing)
                CurrentState = PlaybackState.Paused;
        }

        /// <summary>Resumes playback from the current position.</summary>
        public void Resume()
        {
            if (CurrentState == PlaybackState.Paused)
                CurrentState = PlaybackState.Playing;
        }

        /// <summary>Stops playback and returns camera control to the player.</summary>
        public void Stop()
        {
            if (CurrentState == PlaybackState.Stopped) return;
            CurrentState = PlaybackState.Stopped;
            ReturnCameraControl();
            Debug.Log("[SWEF] CinematicCameraPath: Playback stopped.");
        }

        /// <summary>Jumps to the given time along the path.</summary>
        public void Seek(float time)
        {
            _playbackTime = Mathf.Clamp(time, 0f, GetTotalDuration());
        }

        // ── Path editing ──────────────────────────────────────────────────────────
        /// <summary>Appends the current camera position/rotation as a new waypoint.</summary>
        public void AddWaypoint()
        {
            if (cameraTarget == null) return;

            var wp = new CameraWaypoint
            {
                position       = cameraTarget.position,
                rotation       = cameraTarget.rotation,
                fov            = cameraTarget.GetComponent<Camera>()?.fieldOfView ?? 60f,
                timeAtWaypoint = GetTotalDuration()
            };
            waypoints.Add(wp);
            Debug.Log($"[SWEF] CinematicCameraPath: Waypoint {waypoints.Count - 1} added.");
        }

        /// <summary>Inserts the current camera state as a waypoint at the given index.</summary>
        public void InsertWaypoint(int index)
        {
            if (cameraTarget == null) return;
            index = Mathf.Clamp(index, 0, waypoints.Count);

            var wp = new CameraWaypoint
            {
                position = cameraTarget.position,
                rotation = cameraTarget.rotation,
                fov      = cameraTarget.GetComponent<Camera>()?.fieldOfView ?? 60f
            };
            waypoints.Insert(index, wp);
            RebuildTimestamps();
            Debug.Log($"[SWEF] CinematicCameraPath: Waypoint inserted at index {index}.");
        }

        /// <summary>Removes the waypoint at the given index.</summary>
        public void RemoveWaypoint(int index)
        {
            if (index < 0 || index >= waypoints.Count) return;
            waypoints.RemoveAt(index);
            RebuildTimestamps();
            Debug.Log($"[SWEF] CinematicCameraPath: Waypoint {index} removed.");
        }

        /// <summary>Updates the waypoint at the given index with the current camera position/rotation.</summary>
        public void UpdateWaypoint(int index)
        {
            if (index < 0 || index >= waypoints.Count || cameraTarget == null) return;
            waypoints[index].position = cameraTarget.position;
            waypoints[index].rotation = cameraTarget.rotation;
            waypoints[index].fov      = cameraTarget.GetComponent<Camera>()?.fieldOfView ?? 60f;
            Debug.Log($"[SWEF] CinematicCameraPath: Waypoint {index} updated.");
        }

        // ── Query API ─────────────────────────────────────────────────────────────
        /// <summary>Returns the total duration of the camera path in seconds.</summary>
        public float GetTotalDuration()
        {
            if (waypoints.Count < 2) return 0f;
            return waypoints[waypoints.Count - 1].timeAtWaypoint;
        }

        /// <summary>
        /// Evaluates the camera path at the given timeline time and returns a
        /// <see cref="(Vector3 position, Quaternion rotation, float fov)"/> tuple.
        /// This overload is designed for Replay Theater timeline integration; it does
        /// not move the <see cref="cameraTarget"/> — use <see cref="ApplyAtTime"/> for that.
        /// </summary>
        /// <param name="t">Replay time in seconds (will be clamped to [0, total duration]).</param>
        /// <returns>Interpolated position, rotation, and field of view.</returns>
        public (Vector3 position, Quaternion rotation, float fov) EvaluateAtTime(float t)
        {
            float clamped = Mathf.Clamp(t, 0f, GetTotalDuration());
            return (SamplePosition(clamped), SampleRotation(clamped), SampleFOV(clamped));
        }

        // ── Serialisation ─────────────────────────────────────────────────────────
        /// <summary>Serialises the path to a JSON string.</summary>
        public string ToJson()
        {
            return JsonUtility.ToJson(new PathData { waypoints = waypoints }, true);
        }

        /// <summary>Deserialises a path from JSON. Returns null on failure.</summary>
        public static CinematicCameraPath FromJson(string json)
        {
            try
            {
                var data = JsonUtility.FromJson<PathData>(json);
                var go   = new GameObject("CinematicCameraPath");
                var path = go.AddComponent<CinematicCameraPath>();
                path.waypoints = data.waypoints ?? new List<CameraWaypoint>();
                return path;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SWEF] CinematicCameraPath: Failed to deserialise path — {e.Message}");
                return null;
            }
        }

        // ── Gizmos ────────────────────────────────────────────────────────────────
        /// <summary>Draws the path in the editor as Gizmos.</summary>
        public void PreviewPath()
        {
#if UNITY_EDITOR
            if (waypoints.Count < 2) return;

            Gizmos.color = Color.cyan;
            int steps = waypoints.Count * 20;
            float totalDuration = GetTotalDuration();
            if (totalDuration <= 0f) return;

            Vector3 prev = SamplePosition(0f);
            for (int i = 1; i <= steps; i++)
            {
                float t      = (float)i / steps * totalDuration;
                Vector3 curr = SamplePosition(t);
                Gizmos.DrawLine(prev, curr);
                prev = curr;
            }

            Gizmos.color = Color.yellow;
            foreach (var wp in waypoints)
                Gizmos.DrawSphere(wp.position, 1f);
#endif
        }

        private void OnDrawGizmos() => PreviewPath();

        // ── Internals ────────────────────────────────────────────────────────────
        private void AdvancePlayback(float dt)
        {
            float total = GetTotalDuration();
            if (total <= 0f) { Stop(); return; }

            _playbackTime += dt;

            int prevWpIndex = GetWaypointIndexAtTime(_playbackTime - dt);
            int currWpIndex = GetWaypointIndexAtTime(_playbackTime);
            if (currWpIndex > prevWpIndex)
                OnWaypointReached?.Invoke(currWpIndex);

            if (_playbackTime >= total)
            {
                switch (loopMode)
                {
                    case LoopMode.Loop:
                        _playbackTime -= total;
                        break;
                    case LoopMode.PingPong:
                        _pingPongForward = !_pingPongForward;
                        _playbackTime = total - (_playbackTime - total);
                        playbackSpeed = _pingPongForward ? _basePlaybackSpeed : -_basePlaybackSpeed;
                        break;
                    default: // Once
                        _playbackTime = total;
                        CurrentState  = PlaybackState.Stopped;
                        ApplyAtTime(_playbackTime);
                        ReturnCameraControl();
                        OnPlaybackCompleted?.Invoke();
                        Debug.Log("[SWEF] CinematicCameraPath: Playback completed.");
                        return;
                }
            }

            ApplyAtTime(_playbackTime);
        }

        private void ApplyAtTime(float time)
        {
            if (cameraTarget == null || waypoints.Count < 2) return;

            Vector3    pos = SamplePosition(time);
            Quaternion rot = SampleRotation(time);
            float      fov = SampleFOV(time);

            cameraTarget.position = pos;
            cameraTarget.rotation = rot;

            var cam = cameraTarget.GetComponent<Camera>();
            if (cam != null) cam.fieldOfView = fov;
        }

        private Vector3 SamplePosition(float time)
        {
            float t01 = GetNormalisedT(time);

            if (!useCatmullRom || waypoints.Count < 4)
                return LerpPosition(t01);

            return CatmullRomPosition(t01);
        }

        private Quaternion SampleRotation(float time)
        {
            if (waypoints.Count < 2) return Quaternion.identity;

            float t01 = GetNormalisedT(time);
            int   idx = Mathf.Min(Mathf.FloorToInt(t01 * (waypoints.Count - 1)), waypoints.Count - 2);
            float seg = t01 * (waypoints.Count - 1) - idx;

            return Quaternion.Slerp(waypoints[idx].rotation, waypoints[idx + 1].rotation, seg);
        }

        private float SampleFOV(float time)
        {
            if (waypoints.Count < 2) return 60f;

            float t01 = GetNormalisedT(time);
            int   idx = Mathf.Min(Mathf.FloorToInt(t01 * (waypoints.Count - 1)), waypoints.Count - 2);
            float seg = t01 * (waypoints.Count - 1) - idx;

            return Mathf.Lerp(waypoints[idx].fov, waypoints[idx + 1].fov, seg);
        }

        private float GetNormalisedT(float time)
        {
            float total = GetTotalDuration();
            return total > 0f ? Mathf.Clamp01(time / total) : 0f;
        }

        private Vector3 LerpPosition(float t01)
        {
            int   idx = Mathf.Min(Mathf.FloorToInt(t01 * (waypoints.Count - 1)), waypoints.Count - 2);
            float seg = t01 * (waypoints.Count - 1) - idx;
            return Vector3.Lerp(waypoints[idx].position, waypoints[idx + 1].position, seg);
        }

        private Vector3 CatmullRomPosition(float t01)
        {
            int n   = waypoints.Count;
            float f = t01 * (n - 1);
            int   i = Mathf.Clamp(Mathf.FloorToInt(f), 0, n - 2);
            float u = f - i;

            Vector3 p0 = waypoints[Mathf.Max(i - 1, 0)].position;
            Vector3 p1 = waypoints[i].position;
            Vector3 p2 = waypoints[Mathf.Min(i + 1, n - 1)].position;
            Vector3 p3 = waypoints[Mathf.Min(i + 2, n - 1)].position;

            return 0.5f * (
                2f * p1 +
                (-p0 + p2) * u +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * u * u +
                (-p0 + 3f * p1 - 3f * p2 + p3) * u * u * u
            );
        }

        private int GetWaypointIndexAtTime(float time)
        {
            float t01 = GetNormalisedT(time);
            return Mathf.Clamp(Mathf.FloorToInt(t01 * (waypoints.Count - 1)), 0, waypoints.Count - 1);
        }

        private void RebuildTimestamps()
        {
            for (int i = 0; i < waypoints.Count; i++)
                waypoints[i].timeAtWaypoint = i * defaultTransitionDuration;
        }

        private void ReturnCameraControl()
        {
            var camCtrl = FindFirstObjectByType<Flight.CameraController>();
            if (camCtrl != null) camCtrl.DisableCinematicOverride();
        }

        // ── Serialisation helper ─────────────────────────────────────────────────
        [System.Serializable]
        private class PathData
        {
            public List<CameraWaypoint> waypoints;
        }
    }
}
