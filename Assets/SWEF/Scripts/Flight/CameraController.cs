using System;
using UnityEngine;

namespace SWEF.Flight
{
    /// <summary>
    /// Manages multiple camera modes for the player: FirstPerson, ThirdPerson, Orbit, and Cinematic.
    /// Auto-finds <see cref="FlightController"/> and the main camera if not assigned in Inspector.
    /// Persists the selected mode across sessions via PlayerPrefs.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        public enum CameraMode { FirstPerson, ThirdPerson, Orbit, Cinematic }

        private const string PrefKey = "SWEF_CameraMode";

        [SerializeField] private Transform playerRig;
        [SerializeField] private Camera mainCamera;

        [Header("Third Person")]
        [SerializeField] private Vector3 thirdPersonOffset = new Vector3(0f, 5f, -15f);
        [SerializeField] private float smoothTime = 0.3f;

        [Header("Orbit")]
        [SerializeField] private float orbitDistance = 20f;
        [SerializeField] private float orbitSpeed = 100f;

        [Header("Cinematic")]
        [SerializeField] private float cinematicSpeed = 10f;

        /// <summary>Raised whenever the camera mode changes.</summary>
        public event Action<CameraMode> OnCameraModeChanged;

        /// <summary>The currently active camera mode.</summary>
        public CameraMode CurrentMode { get; private set; } = CameraMode.FirstPerson;

        // Third-person smooth-damp velocity
        private Vector3 _tpVelocity;

        // Orbit state
        private float _orbitAngle;
        private float _orbitHeight;

        // Cinematic state
        private float _cinematicAngle;
        private float _cinematicHeight;
        private float _cinematicHeightDir = 1f;

        // Track whether camera is already parented in first-person
        private bool _firstPersonParented;

        private void Awake()
        {
            if (playerRig == null)
            {
                var fc = FindFirstObjectByType<FlightController>();
                if (fc != null) playerRig = fc.transform;
            }

            if (mainCamera == null)
                mainCamera = Camera.main;

            int saved = PlayerPrefs.GetInt(PrefKey, (int)CameraMode.FirstPerson);
            CurrentMode = (CameraMode)saved;
        }

        /// <summary>Sets the active camera mode and saves it to PlayerPrefs.</summary>
        public void SetCameraMode(CameraMode mode)
        {
            if (mode != CameraMode.FirstPerson)
                _firstPersonParented = false;

            CurrentMode = mode;
            PlayerPrefs.SetInt(PrefKey, (int)mode);
            OnCameraModeChanged?.Invoke(mode);
        }

        /// <summary>Cycles to the next camera mode in enum order.</summary>
        public void CycleCamera()
        {
            int next = ((int)CurrentMode + 1) % Enum.GetValues(typeof(CameraMode)).Length;
            SetCameraMode((CameraMode)next);
        }

        // ── Phase 18 — Cinematic camera override ─────────────────────────────────
        [Header("Phase 18 — Cinema")]
        private bool _cinematicOverride = false;

        /// <summary>Disables player camera updates so the cinematic system can take over.</summary>
        public void EnableCinematicOverride()
        {
            _cinematicOverride = true;
            Debug.Log("[SWEF] Camera control handed to cinematic system");
        }

        /// <summary>Re-enables player camera updates.</summary>
        public void DisableCinematicOverride()
        {
            _cinematicOverride = false;
            Debug.Log("[SWEF] Camera control returned to player");
        }

        /// <summary>Whether the cinematic system currently has camera control.</summary>
        public bool IsCinematicActive => _cinematicOverride;

        private void LateUpdate()
        {
            if (_cinematicOverride) return;
            if (playerRig == null || mainCamera == null) return;

            switch (CurrentMode)
            {
                case CameraMode.FirstPerson:
                    ApplyFirstPerson();
                    break;
                case CameraMode.ThirdPerson:
                    ApplyThirdPerson();
                    break;
                case CameraMode.Orbit:
                    ApplyOrbit();
                    break;
                case CameraMode.Cinematic:
                    ApplyCinematic();
                    break;
            }
        }

        private void ApplyFirstPerson()
        {
            if (!_firstPersonParented)
            {
                mainCamera.transform.SetParent(playerRig, false);
                mainCamera.transform.localPosition = new Vector3(0f, 1.5f, 0f);
                mainCamera.transform.localRotation = Quaternion.identity;
                _firstPersonParented = true;
            }
        }

        private void ApplyThirdPerson()
        {
            mainCamera.transform.SetParent(null);
            Vector3 target = playerRig.TransformPoint(thirdPersonOffset);
            mainCamera.transform.position = Vector3.SmoothDamp(
                mainCamera.transform.position, target, ref _tpVelocity, smoothTime);
            mainCamera.transform.LookAt(playerRig.position + Vector3.up * 1.5f);
        }

        private void ApplyOrbit()
        {
            mainCamera.transform.SetParent(null);

            // Update orbit angle from touch drag
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Moved)
                {
                    _orbitAngle  += touch.deltaPosition.x * orbitSpeed * Time.deltaTime;
                    _orbitHeight += touch.deltaPosition.y * orbitSpeed * Time.deltaTime * 0.5f;
                }
            }

            Quaternion rotation = Quaternion.Euler(_orbitHeight, _orbitAngle, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -orbitDistance);
            mainCamera.transform.position = playerRig.position + offset;
            mainCamera.transform.LookAt(playerRig.position);
        }

        private void ApplyCinematic()
        {
            mainCamera.transform.SetParent(null);

            _cinematicAngle += cinematicSpeed * Time.deltaTime;

            // Gradually vary height between -20 and 20 degrees
            _cinematicHeight += cinematicSpeed * 0.3f * _cinematicHeightDir * Time.deltaTime;
            if (_cinematicHeight >  20f) { _cinematicHeight =  20f; _cinematicHeightDir = -1f; }
            if (_cinematicHeight < -20f) { _cinematicHeight = -20f; _cinematicHeightDir =  1f; }

            Quaternion rotation = Quaternion.Euler(_cinematicHeight, _cinematicAngle, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -orbitDistance);
            mainCamera.transform.position = playerRig.position + offset;
            mainCamera.transform.LookAt(playerRig.position);
        }
    }
}
