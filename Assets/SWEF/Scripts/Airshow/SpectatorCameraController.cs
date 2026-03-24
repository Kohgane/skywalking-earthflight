// Phase 73 — Flight Formation Display & Airshow System
// Assets/SWEF/Scripts/Airshow/SpectatorCameraController.cs
using System;
using UnityEngine;

namespace SWEF.Airshow
{
    /// <summary>
    /// Manages all spectator camera angles during an airshow.
    /// Supports eight distinct modes including an auto-switching Cinematic mode
    /// and a SlowMotion mode that adjusts <see cref="Time.timeScale"/>.
    /// </summary>
    public class SpectatorCameraController : MonoBehaviour
    {
        #region Inspector
        [Header("Camera Points")]
        [Tooltip("Array of pre-placed ground-level audience camera positions.")]
        [SerializeField] private Transform[] groundCameraPoints;
        [SerializeField] private Transform towerCamPoint;

        [Header("Chase Camera")]
        [SerializeField] private Vector3 chaseOffset = new Vector3(0f, 5f, -20f);
        [SerializeField] private float chaseLerpSpeed = 3f;

        [Header("Birds Eye")]
        [SerializeField] private float birdsEyeAltitude = 800f;

        [Header("Cinematic")]
        [SerializeField] private float cinematicSwitchInterval = 5f;

        [Header("Free Roam")]
        [SerializeField] private float freeRoamSpeed = 50f;

        [Header("Slow Motion")]
        [SerializeField] private float slowMotionScale = 0.25f;
        #endregion

        #region Public State
        /// <summary>Currently active spectator camera mode.</summary>
        public SpectatorCameraMode CurrentMode { get; private set; } = SpectatorCameraMode.GroundLevel;

        /// <summary>Index of the performer being tracked (relevant to Chase/Cockpit modes).</summary>
        public int TargetPerformerSlot { get; private set; }
        #endregion

        #region Events
        /// <summary>Fired when the active camera mode changes.</summary>
        public event Action<SpectatorCameraMode> OnCameraModeChanged;
        #endregion

        #region Private
        private Camera _camera;
        private float _cinematicTimer;
        private int _groundCamIndex;
        private float _groundLerpTimer;
        private const float GroundCamSwitchInterval = 8f;
        private readonly SpectatorCameraMode[] _cinematicPool =
        {
            SpectatorCameraMode.GroundLevel,
            SpectatorCameraMode.TowerCam,
            SpectatorCameraMode.ChaseCamera,
            SpectatorCameraMode.BirdsEye
        };
        private int _cinematicPoolIndex;
        private bool _prevSlowMo;
        #endregion

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            _camera = GetComponentInChildren<Camera>();
            if (_camera == null) _camera = Camera.main;
        }

        private void OnDisable()
        {
            if (_prevSlowMo) RestoreTimeScale();
        }

        private void Update()
        {
            switch (CurrentMode)
            {
                case SpectatorCameraMode.GroundLevel:   UpdateGroundLevel();   break;
                case SpectatorCameraMode.TowerCam:      UpdateTowerCam();      break;
                case SpectatorCameraMode.ChaseCamera:   UpdateChaseCamera();   break;
                case SpectatorCameraMode.CockpitCam:    UpdateCockpitCam();    break;
                case SpectatorCameraMode.BirdsEye:      UpdateBirdsEye();      break;
                case SpectatorCameraMode.Cinematic:     UpdateCinematic();     break;
                case SpectatorCameraMode.FreeRoam:      UpdateFreeRoam();      break;
                case SpectatorCameraMode.SlowMotion:    UpdateSlowMotion();    break;
            }
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Switches to the specified spectator camera mode.</summary>
        public void SetMode(SpectatorCameraMode mode)
        {
            // Exit slow-motion if leaving that mode
            if (CurrentMode == SpectatorCameraMode.SlowMotion && mode != SpectatorCameraMode.SlowMotion)
                RestoreTimeScale();

            CurrentMode = mode;
            _cinematicTimer = 0f;
            OnCameraModeChanged?.Invoke(mode);
        }

        /// <summary>Sets which performer slot the camera follows in Chase/Cockpit modes.</summary>
        public void SetTargetPerformer(int slotIndex)
        {
            TargetPerformerSlot = slotIndex;
        }

        // ── Private update methods ───────────────────────────────────────────

        private void UpdateGroundLevel()
        {
            if (groundCameraPoints == null || groundCameraPoints.Length == 0) return;

            _groundLerpTimer += Time.deltaTime;
            if (_groundLerpTimer >= GroundCamSwitchInterval)
            {
                _groundLerpTimer = 0f;
                _groundCamIndex  = (_groundCamIndex + 1) % groundCameraPoints.Length;
            }

            Transform pt = groundCameraPoints[_groundCamIndex];
            if (pt == null) return;
            transform.position = Vector3.Lerp(transform.position, pt.position, Time.deltaTime * 2f);
            LookAtFormationCenter();
        }

        private void UpdateTowerCam()
        {
            if (towerCamPoint == null) return;
            transform.position = towerCamPoint.position;
            LookAtFormationCenter();
        }

        private void UpdateChaseCamera()
        {
            AirshowPerformer performer = FindTargetPerformer();
            if (performer == null) return;
            Vector3 desired = performer.transform.TransformPoint(chaseOffset);
            transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * chaseLerpSpeed);
            transform.LookAt(performer.transform.position);
        }

        private void UpdateCockpitCam()
        {
            AirshowPerformer performer = FindTargetPerformer();
            if (performer == null) return;
            transform.SetPositionAndRotation(performer.transform.position, performer.transform.rotation);
        }

        private void UpdateBirdsEye()
        {
            Vector3 center = GetFormationCenter();
            transform.position = new Vector3(center.x, center.y + birdsEyeAltitude, center.z);
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private void UpdateCinematic()
        {
            _cinematicTimer += Time.deltaTime;
            if (_cinematicTimer >= cinematicSwitchInterval)
            {
                _cinematicTimer = 0f;
                _cinematicPoolIndex = (_cinematicPoolIndex + 1) % _cinematicPool.Length;
            }

            switch (_cinematicPool[_cinematicPoolIndex])
            {
                case SpectatorCameraMode.GroundLevel: UpdateGroundLevel(); break;
                case SpectatorCameraMode.TowerCam:    UpdateTowerCam();    break;
                case SpectatorCameraMode.ChaseCamera: UpdateChaseCamera(); break;
                case SpectatorCameraMode.BirdsEye:    UpdateBirdsEye();    break;
            }
        }

        private void UpdateFreeRoam()
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            float up = Input.GetKey(KeyCode.E) ? 1f : Input.GetKey(KeyCode.Q) ? -1f : 0f;
            transform.Translate(new Vector3(h, up, v) * (freeRoamSpeed * Time.deltaTime), Space.Self);
        }

        private void UpdateSlowMotion()
        {
            if (!_prevSlowMo)
            {
                Time.timeScale = slowMotionScale;
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
                _prevSlowMo = true;
            }
            // Dramatic sweep toward formation center
            LookAtFormationCenter();
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private void LookAtFormationCenter()
        {
            Vector3 center = GetFormationCenter();
            if (center != transform.position)
                transform.LookAt(center);
        }

        private Vector3 GetFormationCenter()
        {
            if (AirshowManager.Instance == null) return Vector3.zero;
            var performers = AirshowManager.Instance.Performers;
            if (performers == null || performers.Count == 0) return Vector3.zero;

            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (AirshowPerformer p in performers)
            {
                if (p == null) continue;
                sum += p.transform.position;
                count++;
            }
            return count > 0 ? sum / count : Vector3.zero;
        }

        private AirshowPerformer FindTargetPerformer()
        {
            if (AirshowManager.Instance == null) return null;
            foreach (AirshowPerformer p in AirshowManager.Instance.Performers)
                if (p != null && p.SlotIndex == TargetPerformerSlot) return p;
            return null;
        }

        private void RestoreTimeScale()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            _prevSlowMo = false;
        }
    }
}
