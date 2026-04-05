// SpectatorCameraController.cs — SWEF Phase 107: Live Streaming & Spectator Mode
using System.Collections;
using UnityEngine;

namespace SWEF.Spectator
{
    /// <summary>
    /// Phase 107 — Advanced camera controller for Spectator Mode.
    ///
    /// <para>Implements five distinct camera behaviours driven by
    /// <see cref="SpectatorCameraMode"/>:</para>
    /// <list type="bullet">
    ///   <item><term>FreeCam</term><description>WASD + mouse free movement.</description></item>
    ///   <item><term>FollowCam</term><description>Smooth chase camera behind the target aircraft.</description></item>
    ///   <item><term>OrbitCam</term><description>Continuous orbit at configurable radius/elevation.</description></item>
    ///   <item><term>CinematicCam</term><description>Auto-selected dramatic shots cycling through <see cref="CinematicShotType"/>.</description></item>
    ///   <item><term>PilotView</term><description>First-person cockpit perspective from the target aircraft.</description></item>
    /// </list>
    ///
    /// <para>Attach this component to the spectator camera GameObject and wire the
    /// <c>config</c> reference via the Inspector.</para>
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class SpectatorCameraController : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────
        [SerializeField] private SpectatorConfig config;

        /// <summary>Local offset applied for <see cref="SpectatorCameraMode.PilotView"/>.</summary>
        [SerializeField] private Vector3 pilotViewOffset = new Vector3(0f, 1.5f, 1f);

        // ── Internal state ─────────────────────────────────────────────────────
        private Camera _camera;
        private SpectatorCameraMode _currentMode;
        private Transform _target;

        // FreeCam
        private float _freePitch;
        private float _freeYaw;

        // OrbitCam
        private float _orbitAngle;

        // CinematicCam
        private CinematicShotType _cinematicShot;
        private float _cinematicTimer;

        // Camera shake
        private Vector3 _shakeOffset;
        private bool _shaking;

        // Applied shake offset tracking (to prevent accumulation across frames)
        private Vector3 _appliedShakeOffset;

        // FOV
        private float _baseTargetFov;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            if (config != null)
                _baseTargetFov = config.defaultFov;
        }

        private void Update()
        {
            if (config == null) return;

            // Remove the shake offset applied in the previous frame before mode updates,
            // so that lerp/follow mode calculations start from the clean camera position.
            transform.position -= _appliedShakeOffset;
            _appliedShakeOffset  = Vector3.zero;

            switch (_currentMode)
            {
                case SpectatorCameraMode.FreeCam:     UpdateFreeCam();     break;
                case SpectatorCameraMode.FollowCam:   UpdateFollowCam();   break;
                case SpectatorCameraMode.OrbitCam:    UpdateOrbitCam();    break;
                case SpectatorCameraMode.CinematicCam: UpdateCinematicCam(); break;
                case SpectatorCameraMode.PilotView:   UpdatePilotView();   break;
            }

            UpdateFov();
            ApplyShakeOffset();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Applies the specified camera <paramref name="mode"/> and assigns the
        /// <paramref name="target"/> aircraft transform.
        /// </summary>
        public void ApplyMode(SpectatorCameraMode mode, Transform target)
        {
            _currentMode  = mode;
            _target       = target;
            _cinematicTimer = 0f;

            // Initialise FreeCam yaw/pitch from current rotation
            if (mode == SpectatorCameraMode.FreeCam)
            {
                _freeYaw   = transform.eulerAngles.y;
                _freePitch = transform.eulerAngles.x;
            }
        }

        /// <summary>Sets the observed target without changing the camera mode.</summary>
        public void SetTarget(Transform target) => _target = target;

        /// <summary>Deactivates the camera controller (e.g. when exiting spectator mode).</summary>
        public void Deactivate()
        {
            _currentMode = SpectatorCameraMode.FreeCam;
            _target = null;
        }

        /// <summary>
        /// Triggers a brief camera shake effect to emphasise a dramatic moment.
        /// </summary>
        public void TriggerShake()
        {
            if (!_shaking && config != null)
                StartCoroutine(ShakeCoroutine());
        }

        // ── Camera update methods ──────────────────────────────────────────────

        private void UpdateFreeCam()
        {
            float speed = config.freeCamSpeed;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                speed *= config.freeCamBoostMultiplier;

            float dt = Time.deltaTime;

            // Translation
            Vector3 move = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) move += transform.forward;
            if (Input.GetKey(KeyCode.S)) move -= transform.forward;
            if (Input.GetKey(KeyCode.A)) move -= transform.right;
            if (Input.GetKey(KeyCode.D)) move += transform.right;
            if (Input.GetKey(KeyCode.Q)) move -= transform.up;
            if (Input.GetKey(KeyCode.E)) move += transform.up;
            transform.position += move * (speed * dt);

            // Rotation (only when right mouse button held)
            if (Input.GetMouseButton(1))
            {
                _freeYaw   += Input.GetAxis("Mouse X") * config.freeCamMouseSensitivity;
                _freePitch -= Input.GetAxis("Mouse Y") * config.freeCamMouseSensitivity;
                _freePitch  = Mathf.Clamp(_freePitch, -89f, 89f);
                transform.rotation = Quaternion.Euler(_freePitch, _freeYaw, 0f);
            }
        }

        private void UpdateFollowCam()
        {
            if (_target == null) return;

            Vector3 desired = _target.TransformPoint(config.followOffset);
            float posT = 1f - Mathf.Pow(config.followPositionSmoothing, Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, posT);

            Quaternion lookRot = Quaternion.LookRotation(_target.position - transform.position);
            float rotT = 1f - Mathf.Pow(config.followRotationSmoothing, Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotT);
        }

        private void UpdateOrbitCam()
        {
            if (_target == null) return;

            _orbitAngle += config.orbitSpeed * Time.deltaTime;

            float elevRad = config.orbitElevation * Mathf.Deg2Rad;
            float x = Mathf.Cos(elevRad) * Mathf.Cos(_orbitAngle * Mathf.Deg2Rad);
            float y = Mathf.Sin(elevRad);
            float z = Mathf.Cos(elevRad) * Mathf.Sin(_orbitAngle * Mathf.Deg2Rad);

            transform.position = _target.position + new Vector3(x, y, z) * config.orbitRadius;
            transform.LookAt(_target.position);
        }

        private void UpdateCinematicCam()
        {
            if (_target == null) return;

            _cinematicTimer += Time.deltaTime;
            if (_cinematicTimer >= config.cinematicShotDuration)
            {
                _cinematicTimer = 0f;
                _cinematicShot  = (CinematicShotType)(((int)_cinematicShot + 1) %
                    System.Enum.GetValues(typeof(CinematicShotType)).Length);
            }

            switch (_cinematicShot)
            {
                case CinematicShotType.Chase:
                    ApplyChaseShotSmooth();
                    break;

                case CinematicShotType.Flyby:
                    // Hold a fixed world position; aircraft flies through the frame
                    transform.LookAt(_target.position);
                    break;

                case CinematicShotType.Dramatic:
                    // Low, wide-angle shot below and in front of the aircraft
                    Vector3 dramPos = _target.position - _target.forward * 25f - Vector3.up * 8f;
                    transform.position = Vector3.Lerp(transform.position, dramPos, 2f * Time.deltaTime);
                    transform.LookAt(_target.position);
                    break;

                case CinematicShotType.TopDown:
                    Vector3 topPos = _target.position + Vector3.up * config.orbitRadius;
                    transform.position = Vector3.Lerp(transform.position, topPos, 1.5f * Time.deltaTime);
                    transform.LookAt(_target.position);
                    break;
            }
        }

        private void ApplyChaseShotSmooth()
        {
            Vector3 desired = _target.TransformPoint(config.cinematicChaseOffset);
            transform.position = Vector3.Lerp(transform.position, desired, 2f * Time.deltaTime);
            transform.LookAt(_target.position);
        }

        private void UpdatePilotView()
        {
            if (_target == null) return;
            transform.position = _target.TransformPoint(pilotViewOffset);
            transform.rotation = _target.rotation;
        }

        // ── FOV ───────────────────────────────────────────────────────────────

        private void UpdateFov()
        {
            if (_camera == null || config == null) return;

            float targetFov = config.defaultFov;

#if SWEF_FLIGHT_AVAILABLE
            // Scale FOV with aircraft speed if the flight system is present
            if (_target != null)
            {
                var fc = _target.GetComponent<SWEF.Flight.FlightController>();
                if (fc != null)
                {
                    float t = Mathf.Clamp01(fc.CurrentSpeedKph / config.fovMaxSpeedKph);
                    targetFov = config.defaultFov + t * config.maxFovBoost;
                }
            }
#endif

            _baseTargetFov    = targetFov;
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, _baseTargetFov, 4f * Time.deltaTime);
        }

        // ── Camera shake ──────────────────────────────────────────────────────

        private IEnumerator ShakeCoroutine()
        {
            _shaking = true;
            float elapsed = 0f;
            while (elapsed < config.shakeDuration)
            {
                _shakeOffset = Random.insideUnitSphere * config.shakeAmplitude;
                elapsed     += Time.deltaTime;
                yield return null;
            }
            _shakeOffset = Vector3.zero;
            _shaking = false;
        }

        private void ApplyShakeOffset()
        {
            // Additively apply the shake offset for this frame and remember it
            // so it can be removed at the start of the next frame before mode updates.
            _appliedShakeOffset  = _shakeOffset;
            transform.position  += _appliedShakeOffset;
        }
    }
}
