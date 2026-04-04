// Phase 98 — PC Input & Controls Polish
// Assets/SWEF/Scripts/PCInput/PCCameraController.cs
using System;
using UnityEngine;

namespace SWEF.PCInput
{
    /// <summary>
    /// PC-optimised camera controller. Provides orbit, zoom, preset angles,
    /// free camera mode, and cockpit head-tracking simulation.
    /// </summary>
    /// <remarks>
    /// Controls:
    /// <list type="bullet">
    ///   <item>Middle mouse button + drag — orbit camera around aircraft</item>
    ///   <item>Scroll wheel — zoom in/out</item>
    ///   <item>Number keys 1–9 — preset camera angles</item>
    ///   <item>F key — toggle free camera mode</item>
    ///   <item>Mouse (cockpit view) — head-tracking simulation</item>
    /// </list>
    /// </remarks>
    [DisallowMultipleComponent]
    public class PCCameraController : MonoBehaviour
    {
        #region Singleton
        /// <summary>Shared PC camera controller instance.</summary>
        public static PCCameraController Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        #endregion

        #region Inspector
        [Header("Target")]
        [Tooltip("Aircraft transform to orbit around. Auto-found if null.")]
        [SerializeField] private Transform target;

        [Header("Orbit")]
        [Tooltip("Mouse sensitivity for orbit drag.")]
        [SerializeField, Range(0.1f, 5f)] private float orbitSensitivity = 2f;

        [Header("Zoom")]
        [Tooltip("Scroll wheel zoom speed.")]
        [SerializeField, Range(0.5f, 20f)] private float zoomSpeed = 5f;

        [Tooltip("Minimum zoom distance from target.")]
        [SerializeField] private float minZoom = 2f;

        [Tooltip("Maximum zoom distance from target.")]
        [SerializeField] private float maxZoom = 200f;

        [Header("Transition")]
        [Tooltip("Camera transition smoothing speed.")]
        [SerializeField, Range(1f, 20f)] private float transitionSpeed = 8f;

        [Header("Head Tracking")]
        [Tooltip("Head tracking intensity in cockpit view (0 = off).")]
        [SerializeField, Range(0f, 2f)] private float headTrackIntensity = 0.5f;

        [Tooltip("Maximum head rotation angle (degrees).")]
        [SerializeField, Range(5f, 45f)] private float headTrackMaxAngle = 15f;

        [Header("Presets")]
        [Tooltip("Camera preset angles: index 0 = key 1 (cockpit), 1 = key 2 (chase), etc.")]
        [SerializeField] private CameraPreset[] presets = new CameraPreset[]
        {
            new CameraPreset { name = "Cockpit",   offset = new Vector3(0f, 1.2f, 0.5f),  distance = 0f,   pitch = 0f,   yaw = 0f   },
            new CameraPreset { name = "Chase",     offset = new Vector3(0f, 2f,   -12f),  distance = 12f,  pitch = 10f,  yaw = 0f   },
            new CameraPreset { name = "External",  offset = new Vector3(4f, 3f,   -10f),  distance = 15f,  pitch = 15f,  yaw = 20f  },
            new CameraPreset { name = "Top-Down",  offset = new Vector3(0f, 50f,  0f),    distance = 50f,  pitch = 90f,  yaw = 0f   },
            new CameraPreset { name = "Wing",      offset = new Vector3(8f, 1f,   0f),    distance = 8f,   pitch = 5f,   yaw = 90f  },
            new CameraPreset { name = "Front",     offset = new Vector3(0f, 1f,   15f),   distance = 15f,  pitch = 5f,   yaw = 180f },
            new CameraPreset { name = "Orbit-Low", offset = new Vector3(0f, 0f,   -20f),  distance = 20f,  pitch = 0f,   yaw = 0f   },
            new CameraPreset { name = "Drone",     offset = new Vector3(0f, 30f,  -20f),  distance = 35f,  pitch = 40f,  yaw = 0f   },
            new CameraPreset { name = "Cinematic", offset = new Vector3(-10f, 5f, -15f),  distance = 20f,  pitch = 20f,  yaw = -30f },
        };
        #endregion

        #region Events
        /// <summary>Fired when the active preset index changes.</summary>
        public event Action<int> OnPresetChanged;

        /// <summary>Fired when free camera mode is toggled.</summary>
        public event Action<bool> OnFreeCameraToggled;
        #endregion

        #region Public State
        /// <summary>Whether free camera mode is active.</summary>
        public bool IsFreeCameraActive { get; private set; }

        /// <summary>Index of the currently active camera preset (-1 if none).</summary>
        public int ActivePresetIndex { get; private set; } = 1; // Chase by default
        #endregion

        #region Private State
        private float _currentYaw;
        private float _currentPitch;
        private float _currentDistance = 12f;
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private Camera _camera;
        private PCKeybindConfig _keybindConfig;
        private bool _orbitDragging;
        private Vector3 _freeCameraVelocity;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null) _camera = Camera.main;
            _keybindConfig = FindFirstObjectByType<PCKeybindConfig>();

            if (target == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) target = go.transform;
            }

            ApplyPreset(ActivePresetIndex);
        }

        private void LateUpdate()
        {
#if !UNITY_ANDROID && !UNITY_IOS
            HandlePresetKeys();
            HandleFreeCameraToggle();

            if (IsFreeCameraActive)
                UpdateFreeCamera();
            else
                UpdateOrbitCamera();
#endif
        }
        #endregion

        #region Orbit Camera
#if !UNITY_ANDROID && !UNITY_IOS
        private void UpdateOrbitCamera()
        {
            if (target == null) return;

            // Orbit with middle mouse drag
            if (Input.GetMouseButtonDown(2)) _orbitDragging = true;
            if (Input.GetMouseButtonUp(2))   _orbitDragging = false;

            if (_orbitDragging)
            {
                _currentYaw   += Input.GetAxis("Mouse X") * orbitSensitivity;
                _currentPitch -= Input.GetAxis("Mouse Y") * orbitSensitivity;
                _currentPitch  = Mathf.Clamp(_currentPitch, -89f, 89f);
            }

            // Scroll wheel zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            _currentDistance -= scroll * zoomSpeed;
            _currentDistance = Mathf.Clamp(_currentDistance, minZoom, maxZoom);

            // Cockpit head-tracking (preset 0)
            if (ActivePresetIndex == 0 && headTrackIntensity > 0f)
                ApplyCockpitHeadTracking();
            else
                UpdateOrbitTransform();
        }

        private void UpdateOrbitTransform()
        {
            if (target == null) return;
            Quaternion rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -_currentDistance);
            _targetPosition = target.position + offset;
            _targetRotation = rotation;

            if (_camera != null)
            {
                _camera.transform.position = Vector3.Lerp(_camera.transform.position, _targetPosition, Time.deltaTime * transitionSpeed);
                _camera.transform.rotation = Quaternion.Slerp(_camera.transform.rotation, _targetRotation, Time.deltaTime * transitionSpeed);
            }
        }

        private void ApplyCockpitHeadTracking()
        {
            if (target == null || _camera == null) return;
            Vector2 mouseNorm = new Vector2(
                (Input.mousePosition.x / Screen.width  - 0.5f) * 2f,
                (Input.mousePosition.y / Screen.height - 0.5f) * 2f);

            float yaw   = mouseNorm.x * headTrackMaxAngle * headTrackIntensity;
            float pitch = -mouseNorm.y * headTrackMaxAngle * headTrackIntensity;

            Quaternion localHead = Quaternion.Euler(pitch, yaw, 0f);
            CameraPreset preset  = presets[0];

            _targetPosition = target.TransformPoint(preset.offset);
            _targetRotation = target.rotation * localHead;

            _camera.transform.position = Vector3.Lerp(_camera.transform.position, _targetPosition, Time.deltaTime * transitionSpeed);
            _camera.transform.rotation = Quaternion.Slerp(_camera.transform.rotation, _targetRotation, Time.deltaTime * transitionSpeed);
        }

        private void HandlePresetKeys()
        {
            for (int i = 0; i < Mathf.Min(9, presets.Length); i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    ApplyPreset(i);
                    return;
                }
            }
        }

        private void HandleFreeCameraToggle()
        {
            KeyCode freeKey = _keybindConfig != null ? _keybindConfig.GetKey("FreeCamera", KeyCode.F) : KeyCode.F;
            if (Input.GetKeyDown(freeKey))
                ToggleFreeCamera();
        }
#endif
        #endregion

        #region Free Camera
        private void UpdateFreeCamera()
        {
#if !UNITY_ANDROID && !UNITY_IOS
            if (_camera == null) return;
            const float speed = 20f;
            Vector3 move = new Vector3(
                Input.GetAxis("Horizontal"),
                Input.GetKey(KeyCode.E) ? 1f : Input.GetKey(KeyCode.Q) ? -1f : 0f,
                Input.GetAxis("Vertical")) * speed * Time.deltaTime;

            if (Input.GetMouseButton(1))
            {
                float lookX = Input.GetAxis("Mouse X") * orbitSensitivity;
                float lookY = Input.GetAxis("Mouse Y") * orbitSensitivity;
                _currentYaw   += lookX;
                _currentPitch -= lookY;
                _currentPitch  = Mathf.Clamp(_currentPitch, -89f, 89f);
            }

            _camera.transform.rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
            _camera.transform.position += _camera.transform.TransformDirection(move);
#endif
        }
        #endregion

        #region Public API
        /// <summary>Activate a camera preset by index.</summary>
        /// <param name="index">Preset index (0-based, corresponds to number keys 1–9).</param>
        public void ApplyPreset(int index)
        {
            if (presets == null || index < 0 || index >= presets.Length) return;
            ActivePresetIndex = index;

            CameraPreset p = presets[index];
            _currentYaw      = p.yaw;
            _currentPitch    = p.pitch;
            _currentDistance = p.distance > 0f ? p.distance : minZoom;

            OnPresetChanged?.Invoke(index);
        }

        /// <summary>Toggle free camera mode on or off.</summary>
        public void ToggleFreeCamera()
        {
            IsFreeCameraActive = !IsFreeCameraActive;
            if (!IsFreeCameraActive && _camera != null)
                ApplyPreset(ActivePresetIndex);
            OnFreeCameraToggled?.Invoke(IsFreeCameraActive);
        }
        #endregion

        #region Data Types
        /// <summary>Defines a named camera preset position/orientation.</summary>
        [Serializable]
        public class CameraPreset
        {
            /// <summary>Display name for this preset.</summary>
            public string name;
            /// <summary>Position offset relative to the aircraft.</summary>
            public Vector3 offset;
            /// <summary>Orbit distance from the target.</summary>
            public float distance;
            /// <summary>Initial pitch angle (degrees).</summary>
            public float pitch;
            /// <summary>Initial yaw angle (degrees).</summary>
            public float yaw;
        }
        #endregion
    }
}
