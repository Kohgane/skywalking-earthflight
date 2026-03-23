using System;
using UnityEngine;

namespace SWEF.Replay
{
    /// <summary>
    /// Phase 48 — Manages multiple camera angles during replay playback.
    /// Attach to the main camera (or a camera-controller GameObject).
    /// </summary>
    public class ReplayCameraController : MonoBehaviour
    {
        #region Constants

        private const float DefaultFollowDistance  = 15f;
        private const float DefaultFollowHeight    =  5f;
        private const float DefaultChaseLerp       =  5f;
        private const float DefaultOrbitRadius     = 20f;
        private const float DefaultOrbitSpeed      = 30f;   // degrees/s
        private const float DefaultTransitionDur   =  1.5f;
        private const float FreeCamSpeed           = 20f;
        private const float FreeCamSensitivity     =  3f;
        private const float CinematicSwitchInterval = 8f;
        private const float ShakeDecayRate         =  5f;

        #endregion

        #region Inspector

        [Header("Target")]
        [Tooltip("Transform of the aircraft being watched during replay.")]
        [SerializeField] private Transform target;

        [Header("Follow Cam")]
        [SerializeField] private float followDistance = DefaultFollowDistance;
        [SerializeField] private float followHeight   = DefaultFollowHeight;

        [Header("Chase Cam")]
        [SerializeField] private float chaseLerpSpeed = DefaultChaseLerp;

        [Header("Orbit Cam")]
        [SerializeField] private float orbitRadius = DefaultOrbitRadius;
        [SerializeField] private float orbitSpeed  = DefaultOrbitSpeed;

        [Header("Transitions")]
        [SerializeField] private float transitionDuration = DefaultTransitionDur;

        [Header("Camera Shake")]
        [SerializeField] private float maxShakeAmplitude = 0.3f;

        [Header("DOF Hints (read by post-process stack)")]
        [Tooltip("Focus distance hint in world units — read by your post-process volume.")]
        public float DofFocusDistance = 50f;
        [Tooltip("Aperture f-number hint.")]
        public float DofAperture = 5.6f;

        #endregion

        #region Events

        /// <summary>Fired whenever the active camera angle changes.</summary>
        public event Action<CameraAngle> OnCameraAngleChanged;

        #endregion

        #region Public Properties

        /// <summary>Currently active camera angle mode.</summary>
        public CameraAngle CurrentAngle { get; private set; } = CameraAngle.FollowCam;

        #endregion

        #region Private State

        private Camera     _cam;
        private CameraAngle _previousAngle;
        private float      _transitionTimer;
        private bool       _transitioning;

        // Orbit state
        private float _orbitAngle;

        // Free cam state
        private float _freePitch, _freeYaw;
        private Vector3 _freePosition;

        // Shake state
        private float   _shakeAmount;
        private Vector3 _shakeOffset;

        // Cinematic auto-switch
        private float _cinematicTimer;
        private int   _cinematicIndex;
        private static readonly CameraAngle[] CinematicSequence =
        {
            CameraAngle.FollowCam,
            CameraAngle.ChaseCam,
            CameraAngle.OrbitCam,
            CameraAngle.FollowCam
        };

        // Transition interpolation
        private Vector3    _transFrom;
        private Quaternion _transFromRot;

        // Playback reference (for speed-based chase distance)
        private FlightPlaybackController _playback;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _cam     = GetComponent<Camera>() ?? Camera.main;
            _playback = FindFirstObjectByType<FlightPlaybackController>();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            if (_transitioning)
            {
                UpdateTransition();
                return;
            }

            switch (CurrentAngle)
            {
                case CameraAngle.FollowCam:    UpdateFollowCam();    break;
                case CameraAngle.CockpitCam:   UpdateCockpitCam();   break;
                case CameraAngle.ChaseCam:     UpdateChaseCam();     break;
                case CameraAngle.OrbitCam:     UpdateOrbitCam();     break;
                case CameraAngle.FreeCam:      UpdateFreeCam();      break;
                case CameraAngle.CinematicCam: UpdateCinematicCam(); break;
            }

            ApplyShake();
            UpdateDofHints();
        }

        #endregion

        #region Public API

        /// <summary>Switches to the specified camera angle with a smooth transition.</summary>
        public void SetAngle(CameraAngle angle)
        {
            if (angle == CurrentAngle) return;

            _previousAngle = CurrentAngle;
            CurrentAngle   = angle;

            if (angle == CameraAngle.FreeCam && _cam != null)
            {
                _freePosition = _cam.transform.position;
                _freePitch    = _cam.transform.eulerAngles.x;
                _freeYaw      = _cam.transform.eulerAngles.y;
            }

            BeginTransition();
            OnCameraAngleChanged?.Invoke(angle);
        }

        /// <summary>Sets the target aircraft transform.</summary>
        public void SetTarget(Transform t) => target = t;

        /// <summary>
        /// Adds a camera shake impulse.
        /// <paramref name="amount"/> is clamped to [0, <see cref="maxShakeAmplitude"/>].
        /// </summary>
        public void AddShake(float amount)
        {
            _shakeAmount = Mathf.Clamp(amount, 0f, maxShakeAmplitude);
        }

        /// <summary>Sets the orbit radius at runtime.</summary>
        public void SetOrbitRadius(float r) => orbitRadius = Mathf.Max(1f, r);

        /// <summary>Sets the orbit angular speed at runtime (degrees/s).</summary>
        public void SetOrbitSpeed(float s) => orbitSpeed = s;

        #endregion

        #region Private — Per-Mode Updates

        private void UpdateFollowCam()
        {
            if (_cam == null || target == null) return;
            Vector3 desired = target.position
                            - target.forward * followDistance
                            + Vector3.up      * followHeight;
            _cam.transform.position = Vector3.Lerp(_cam.transform.position, desired, Time.deltaTime * 5f);
            _cam.transform.LookAt(target.position + target.forward * 2f);
        }

        private void UpdateCockpitCam()
        {
            if (_cam == null || target == null) return;
            _cam.transform.position = target.position + target.forward * 0.5f + target.up * 0.3f;
            _cam.transform.rotation = target.rotation;
        }

        private void UpdateChaseCam()
        {
            if (_cam == null || target == null) return;
            float speed        = _playback != null
                ? (_playback.ActiveRecording != null ? GetCurrentSpeed() : 0f)
                : 0f;
            float dynamicDist  = followDistance + speed * 0.05f;
            Vector3 desired    = target.position
                               - target.forward * dynamicDist
                               + Vector3.up      * (followHeight * 0.5f);
            _cam.transform.position = Vector3.Lerp(_cam.transform.position, desired,
                                                    Time.deltaTime * chaseLerpSpeed);
            _cam.transform.LookAt(target.position);
        }

        private void UpdateOrbitCam()
        {
            if (_cam == null || target == null) return;
            _orbitAngle += orbitSpeed * Time.deltaTime;
            float rad    = _orbitAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Sin(rad), 0.4f, Mathf.Cos(rad)) * orbitRadius;
            _cam.transform.position = target.position + offset;
            _cam.transform.LookAt(target.position);
        }

        private void UpdateFreeCam()
        {
            if (_cam == null) return;
#if UNITY_EDITOR || !UNITY_EDITOR
            float h    = Input.GetAxis("Mouse X") * FreeCamSensitivity;
            float v    = Input.GetAxis("Mouse Y") * FreeCamSensitivity;
            _freeYaw  += h;
            _freePitch = Mathf.Clamp(_freePitch - v, -89f, 89f);

            Quaternion rot = Quaternion.Euler(_freePitch, _freeYaw, 0f);
            Vector3 move   = rot * new Vector3(
                Input.GetAxis("Horizontal"),
                (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f),
                Input.GetAxis("Vertical")) * FreeCamSpeed * Time.deltaTime;

            _freePosition           += move;
            _cam.transform.position  = _freePosition;
            _cam.transform.rotation  = rot;
#endif
        }

        private void UpdateCinematicCam()
        {
            _cinematicTimer += Time.deltaTime;
            if (_cinematicTimer >= CinematicSwitchInterval)
            {
                _cinematicTimer = 0f;
                _cinematicIndex = (_cinematicIndex + 1) % CinematicSequence.Length;
                CurrentAngle    = CinematicSequence[_cinematicIndex];
                BeginTransition();
            }

            // Drive the currently selected sub-mode.
            switch (CurrentAngle)
            {
                case CameraAngle.FollowCam: UpdateFollowCam(); break;
                case CameraAngle.ChaseCam:  UpdateChaseCam();  break;
                case CameraAngle.OrbitCam:  UpdateOrbitCam();  break;
            }
        }

        #endregion

        #region Private — Transitions

        private void BeginTransition()
        {
            if (_cam == null) return;
            _transFrom        = _cam.transform.position;
            _transFromRot     = _cam.transform.rotation;
            _transitionTimer  = 0f;
            _transitioning    = true;
        }

        private void UpdateTransition()
        {
            if (_cam == null) return;
            _transitionTimer += Time.deltaTime;
            float t           = Mathf.SmoothStep(0f, 1f, _transitionTimer / Mathf.Max(transitionDuration, 0.01f));

            // Compute target position for new angle without yet committing.
            Vector3    targetPos = _cam.transform.position;
            Quaternion targetRot = _cam.transform.rotation;
            ComputeTargetPose(CurrentAngle, out targetPos, out targetRot);

            _cam.transform.position = Vector3.Lerp(_transFrom, targetPos, t);
            _cam.transform.rotation = Quaternion.Slerp(_transFromRot, targetRot, t);

            if (_transitionTimer >= transitionDuration) _transitioning = false;
        }

        private void ComputeTargetPose(CameraAngle angle, out Vector3 pos, out Quaternion rot)
        {
            pos = _cam != null ? _cam.transform.position : Vector3.zero;
            rot = _cam != null ? _cam.transform.rotation : Quaternion.identity;
            if (target == null) return;

            switch (angle)
            {
                case CameraAngle.FollowCam:
                    pos = target.position - target.forward * followDistance + Vector3.up * followHeight;
                    rot = Quaternion.LookRotation(target.position - pos);
                    break;
                case CameraAngle.CockpitCam:
                    pos = target.position + target.forward * 0.5f + target.up * 0.3f;
                    rot = target.rotation;
                    break;
                case CameraAngle.ChaseCam:
                    pos = target.position - target.forward * (followDistance + 5f) + Vector3.up * followHeight;
                    rot = Quaternion.LookRotation(target.position - pos);
                    break;
                case CameraAngle.OrbitCam:
                    float rad = _orbitAngle * Mathf.Deg2Rad;
                    pos = target.position + new Vector3(Mathf.Sin(rad), 0.4f, Mathf.Cos(rad)) * orbitRadius;
                    rot = Quaternion.LookRotation(target.position - pos);
                    break;
            }
        }

        #endregion

        #region Private — Helpers

        private void ApplyShake()
        {
            if (_shakeAmount <= 0f || _cam == null) return;
            _shakeOffset = UnityEngine.Random.insideUnitSphere * _shakeAmount;
            _cam.transform.position += _shakeOffset;
            _shakeAmount = Mathf.MoveTowards(_shakeAmount, 0f, Time.deltaTime * ShakeDecayRate);
        }

        private void UpdateDofHints()
        {
            if (target == null || _cam == null) return;
            DofFocusDistance = Vector3.Distance(_cam.transform.position, target.position);
        }

        private float GetCurrentSpeed()
        {
            if (_playback?.ActiveRecording == null) return 0f;
            int idx = _playback.ActiveRecording.FindFrameIndex(_playback.CurrentTime);
            if (idx < 0) return 0f;
            return _playback.ActiveRecording.frames[idx].speed;
        }

        #endregion
    }
}
