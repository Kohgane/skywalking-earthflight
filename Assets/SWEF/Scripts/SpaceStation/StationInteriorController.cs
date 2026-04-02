// StationInteriorController.cs — SWEF Space Station & Orbital Docking System
using System;
using UnityEngine;

namespace SWEF.SpaceStation
{
    /// <summary>
    /// Activated after a successful dock.  Switches the camera to first-person
    /// interior mode, enables zero-gravity movement (push-off, grab-handles, float),
    /// and provides hatch-based navigation between station modules.
    /// Call <see cref="ExitStation"/> to trigger the undock sequence.
    /// </summary>
    public class StationInteriorController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Tooltip("Root GameObject of the station interior scene.")]
        [SerializeField] private GameObject _interiorRoot;

        [Tooltip("Transform the first-person camera will be parented to while inside.")]
        [SerializeField] private Transform _fpsCameraAnchor;

        [Tooltip("Zero-G movement speed (m/s) when floating between handles.")]
        [Range(0.1f, 20f)]
        [SerializeField] private float _floatSpeed = 3f;

        [Tooltip("Impulse speed (m/s) when the player pushes off a wall.")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _pushOffSpeed = 6f;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Fired when the player fully enters the station interior.</summary>
        public event Action OnInteriorEntered;

        /// <summary>Fired when the player exits the station (undock sequence starts).</summary>
        public event Action OnInteriorExited;

        // ── Public read-only ──────────────────────────────────────────────────────

        public bool IsInsideStation { get; private set; }
        public string CurrentSegmentId { get; private set; } = string.Empty;

        // ── Private state ─────────────────────────────────────────────────────────

        private Camera _mainCamera;
        private Vector3 _floatVelocity;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (_interiorRoot != null)
                _interiorRoot.SetActive(false);
        }

        private void Update()
        {
            if (!IsInsideStation) return;
            ApplyZeroGMovement();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Activates interior mode.  Called by <see cref="DockingController"/> after a hard-dock.</summary>
        public void EnterStation(StationDefinition station, string startSegmentId = "")
        {
            IsInsideStation  = true;
            CurrentSegmentId = startSegmentId;

            if (_interiorRoot != null)
                _interiorRoot.SetActive(true);

            _mainCamera = Camera.main;
            if (_mainCamera != null && _fpsCameraAnchor != null)
            {
                _mainCamera.transform.SetParent(_fpsCameraAnchor, false);
                _mainCamera.transform.localPosition = Vector3.zero;
                _mainCamera.transform.localRotation = Quaternion.identity;
            }

            OnInteriorEntered?.Invoke();
        }

        /// <summary>Navigates the player through a hatch to an adjacent segment.</summary>
        public void NavigateToSegment(string segmentId)
        {
            if (!IsInsideStation) return;
            CurrentSegmentId = segmentId;
            _floatVelocity   = Vector3.zero;
        }

        /// <summary>Exits the station and triggers the undock sequence.</summary>
        public void ExitStation()
        {
            if (!IsInsideStation) return;

            IsInsideStation  = false;
            CurrentSegmentId = string.Empty;
            _floatVelocity   = Vector3.zero;

            if (_interiorRoot != null)
                _interiorRoot.SetActive(false);

            if (_mainCamera != null)
                _mainCamera.transform.SetParent(null);

            OnInteriorExited?.Invoke();

            if (DockingController.Instance != null)
                DockingController.Instance.Undock();
        }

        /// <summary>Applies an instantaneous push-off impulse in world space direction.</summary>
        public void PushOff(Vector3 worldDirection)
        {
            if (!IsInsideStation) return;
            _floatVelocity = worldDirection.normalized * _pushOffSpeed;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void ApplyZeroGMovement()
        {
            // Dampen float velocity gradually (simulate catching a handle)
            _floatVelocity = Vector3.Lerp(_floatVelocity, Vector3.zero, Time.deltaTime * 0.5f);
            transform.position += _floatVelocity * Time.deltaTime;
        }
    }
}
