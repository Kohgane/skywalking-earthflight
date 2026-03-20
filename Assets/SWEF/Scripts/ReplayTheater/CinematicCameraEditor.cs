using System.Collections.Generic;
using UnityEngine;
using SWEF.Cinema;

namespace SWEF.ReplayTheater
{
    /// <summary>
    /// In-theater cinematic camera editor.
    /// Manages a list of <see cref="CameraKeyframe"/> objects, supports five camera
    /// modes (Free, Follow, Orbit, Track, Dolly), and evaluates the camera state at
    /// any replay time via Catmull-Rom spline interpolation.
    /// Integrates with <see cref="CinematicCameraPath"/> for spline logic.
    /// </summary>
    public class CinematicCameraEditor : MonoBehaviour
    {
        #region Inspector

        [Header("Camera")]
        [SerializeField] private Camera theaterCamera;
        [SerializeField] private Transform followTarget;

        [Header("Orbit")]
        [SerializeField] private float orbitRadius = 20f;

        [Header("Path Reference")]
        [SerializeField] private CinematicCameraPath cinematicPath;

        [Header("Settings")]
        [SerializeField] private ReplayTheaterSettings settings;

        #endregion

        #region State

        private readonly List<CameraKeyframe> _keyframes = new List<CameraKeyframe>();
        private float _replayDuration;

        #endregion

        #region Events

        /// <summary>Fired when a keyframe is added or removed.</summary>
        public event System.Action OnKeyframesChanged;

        /// <summary>Fired when the camera mode changes.</summary>
        public event System.Action<CameraMode> OnCameraModeChanged;

        #endregion

        #region Properties

        /// <summary>Read-only list of current keyframes (sorted by time).</summary>
        public IReadOnlyList<CameraKeyframe> Keyframes => _keyframes;

        /// <summary>Currently active camera mode (from the latest keyframe before current time).</summary>
        public CameraMode ActiveMode { get; private set; } = CameraMode.Free;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (theaterCamera == null) theaterCamera = Camera.main;
            if (settings == null) settings = Resources.Load<ReplayTheaterSettings>("ReplayTheaterSettings");
        }

        #endregion

        #region Public API

        /// <summary>Sets the total replay duration; used to clamp keyframe times.</summary>
        /// <param name="duration">Duration in seconds.</param>
        public void SetReplayDuration(float duration) => _replayDuration = duration;

        /// <summary>Adds a keyframe; the list is kept sorted by time.</summary>
        /// <param name="kf">The keyframe to insert.</param>
        public void AddKeyframe(CameraKeyframe kf)
        {
            if (kf == null) return;
            kf.time = Mathf.Clamp(kf.time, 0f, _replayDuration);
            _keyframes.Add(kf);
            _keyframes.Sort((a, b) => a.time.CompareTo(b.time));
            OnKeyframesChanged?.Invoke();
            Debug.Log($"[SWEF] CinematicCameraEditor: Keyframe added at t={kf.time:F2}s.");
        }

        /// <summary>Adds a keyframe at the current camera transform.</summary>
        /// <param name="time">Replay time for the keyframe.</param>
        /// <param name="mode">Camera mode to assign.</param>
        public void AddKeyframeFromCurrentCamera(float time, CameraMode mode = CameraMode.Free)
        {
            if (theaterCamera == null) return;
            var kf = new CameraKeyframe(time, theaterCamera.transform.position,
                                        theaterCamera.transform.rotation, mode)
            {
                fov = theaterCamera.fieldOfView
            };
            AddKeyframe(kf);
        }

        /// <summary>Removes the keyframe at the given index.</summary>
        /// <param name="index">Zero-based index.</param>
        public void RemoveKeyframe(int index)
        {
            if (index < 0 || index >= _keyframes.Count) return;
            float t = _keyframes[index].time;
            _keyframes.RemoveAt(index);
            OnKeyframesChanged?.Invoke();
            Debug.Log($"[SWEF] CinematicCameraEditor: Keyframe {index} (t={t:F2}s) removed.");
        }

        /// <summary>Removes all keyframes.</summary>
        public void ClearKeyframes()
        {
            _keyframes.Clear();
            OnKeyframesChanged?.Invoke();
            Debug.Log("[SWEF] CinematicCameraEditor: All keyframes cleared.");
        }

        /// <summary>
        /// Evaluates the camera state at <paramref name="replayTime"/> using
        /// Catmull-Rom interpolation and applies it to <see cref="theaterCamera"/>.
        /// </summary>
        /// <param name="replayTime">Seconds since replay start.</param>
        public void EvaluateAndApply(float replayTime)
        {
            var kf = EvaluateAtTime(replayTime);
            if (kf == null || theaterCamera == null) return;

            // Apply mode-specific camera behaviour
            ApplyCameraMode(kf, replayTime);
        }

        /// <summary>
        /// Returns an interpolated <see cref="CameraKeyframe"/> for <paramref name="time"/>
        /// without modifying the camera.
        /// </summary>
        /// <param name="time">Target replay time in seconds.</param>
        /// <returns>Interpolated keyframe, or <c>null</c> if no keyframes are defined.</returns>
        public CameraKeyframe EvaluateAtTime(float time)
        {
            if (_keyframes.Count == 0) return null;
            if (_keyframes.Count == 1) return _keyframes[0];

            // Clamp to range
            if (time <= _keyframes[0].time) return _keyframes[0];
            if (time >= _keyframes[_keyframes.Count - 1].time) return _keyframes[_keyframes.Count - 1];

            // Find surrounding segment
            int lo = 0;
            for (int i = 0; i < _keyframes.Count - 1; i++)
            {
                if (_keyframes[i].time <= time && _keyframes[i + 1].time >= time)
                { lo = i; break; }
            }

            var a   = _keyframes[lo];
            var b   = _keyframes[lo + 1];
            float d = b.time - a.time;
            float t = d > 0f ? (time - a.time) / d : 0f;

            if (_keyframes.Count < 4)
                return CameraKeyframe.Lerp(a, b, t);

            // Catmull-Rom spline — use neighbouring keyframes
            int p0i = Mathf.Max(lo - 1, 0);
            int p3i = Mathf.Min(lo + 2, _keyframes.Count - 1);

            var p0 = _keyframes[p0i];
            var p3 = _keyframes[p3i];

            float easedT = a.easingCurve != null ? a.easingCurve.Evaluate(t) : t;

            return new CameraKeyframe
            {
                time     = time,
                position = CatmullRomVec3(p0.position, a.position, b.position, p3.position, easedT),
                rotation = Quaternion.Slerp(a.rotation, b.rotation, easedT),
                fov      = Mathf.Lerp(a.fov, b.fov, easedT),
                dofFocusDistance = Mathf.Lerp(a.dofFocusDistance, b.dofFocusDistance, easedT),
                dofAperture      = Mathf.Lerp(a.dofAperture,      b.dofAperture,      easedT),
                mode             = a.mode,
                easingCurve      = a.easingCurve,
            };
        }

        /// <summary>Triggers a gizmo preview of the camera path in the Editor.</summary>
        public void PreviewPath()
        {
            cinematicPath?.PreviewPath();
        }

        #endregion

        #region Internals

        private void ApplyCameraMode(CameraKeyframe kf, float replayTime)
        {
            if (ActiveMode != kf.mode)
            {
                ActiveMode = kf.mode;
                OnCameraModeChanged?.Invoke(ActiveMode);
            }

            var cam = theaterCamera;
            cam.fieldOfView = kf.fov;

            switch (kf.mode)
            {
                case CameraMode.Free:
                    cam.transform.position = kf.position;
                    cam.transform.rotation = kf.rotation;
                    break;

                case CameraMode.Follow:
                    if (followTarget != null)
                    {
                        cam.transform.position = followTarget.position + kf.position;
                        cam.transform.LookAt(followTarget);
                    }
                    break;

                case CameraMode.Orbit:
                    if (followTarget != null)
                    {
                        float angle = replayTime * 30f;
                        cam.transform.position = followTarget.position
                            + Quaternion.Euler(0, angle, 0) * new Vector3(orbitRadius, 5f, 0f);
                        cam.transform.LookAt(followTarget);
                    }
                    break;

                case CameraMode.Track:
                    if (followTarget != null)
                    {
                        cam.transform.position = kf.position;
                        cam.transform.LookAt(followTarget);
                    }
                    break;

                case CameraMode.Dolly:
                    cam.transform.position = kf.position;
                    cam.transform.rotation = kf.rotation;
                    break;
            }
        }

        private static Vector3 CatmullRomVec3(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            return 0.5f * (
                2f * p1 +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t
            );
        }

        #endregion
    }
}
