// NPCVisualController.cs — Phase 110: Dynamic NPC & Air Traffic Ecosystem
// LOD management, navigation lights, contrails, and livery for NPC aircraft.
// Namespace: SWEF.NPCTraffic

using System;
using UnityEngine;

namespace SWEF.NPCTraffic
{
    /// <summary>
    /// Phase 110 — Controls the visual representation of a single NPC aircraft.
    /// Manages LOD switching (icon → low-poly → full model), navigation light
    /// animation, contrail visibility, and livery tinting based on category.
    /// Attach to the same GameObject as <see cref="NPCAircraftController"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NPCVisualController : MonoBehaviour
    {
        #region Events

        /// <summary>Fired when the LOD tier changes. Argument is the new LOD level.</summary>
        public event Action<NPCVisualLOD> OnLODChanged;

        #endregion

        #region Inspector

        [Header("LOD Objects")]
        [Tooltip("Transform to enable when rendering as an icon blip.")]
        [SerializeField] private GameObject _iconObject;

        [Tooltip("Transform to enable when rendering as a low-poly mesh.")]
        [SerializeField] private GameObject _lowPolyObject;

        [Tooltip("Transform to enable when rendering the full model.")]
        [SerializeField] private GameObject _fullModelObject;

        [Header("Navigation Lights")]
        [Tooltip("Port (left) position light — red.")]
        [SerializeField] private Light _portLight;

        [Tooltip("Starboard (right) position light — green.")]
        [SerializeField] private Light _starboardLight;

        [Tooltip("Tail light — white.")]
        [SerializeField] private Light _tailLight;

        [Tooltip("Anti-collision beacon — red, flashing.")]
        [SerializeField] private Light _beaconLight;

        [Tooltip("Beacon flash interval in seconds.")]
        [Range(0.5f, 2f)]
        [SerializeField] private float _beaconFlashInterval = 1.2f;

        [Header("Contrail")]
        [Tooltip("Particle system used for the contrail effect.")]
        [SerializeField] private ParticleSystem _contrailParticles;

        [Tooltip("Minimum altitude in metres MSL above which contrails are visible.")]
        [SerializeField] private float _contrailMinAltitudeMetres = 8000f;

        #endregion

        #region Private State

        private NPCAircraftController _controller;
        private NPCVisualLOD          _currentLOD = (NPCVisualLOD)(-1);
        private float                 _nextBeaconFlip;
        private bool                  _beaconOn;
        private Transform             _playerTransform;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _controller = GetComponent<NPCAircraftController>();
        }

        private void Update()
        {
            if (_controller == null || _controller.Data == null) return;

            UpdateLOD();
            UpdateBeacon();
            UpdateContrail();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Supplies the player transform used for LOD distance computation.
        /// </summary>
        /// <param name="playerTransform">Player's transform.</param>
        public void SetPlayerTransform(Transform playerTransform)
        {
            _playerTransform = playerTransform;
        }

        /// <summary>
        /// Applies a category-appropriate livery tint to the full model renderer.
        /// </summary>
        /// <param name="category">NPC aircraft category.</param>
        public void ApplyLivery(NPCAircraftCategory category)
        {
            if (_fullModelObject == null) return;

            Color liveryColor = CategoryToColor(category);
            foreach (Renderer rend in _fullModelObject.GetComponentsInChildren<Renderer>())
            {
                if (rend.material != null)
                    rend.material.color = liveryColor;
            }
        }

        #endregion

        #region Private — LOD

        private void UpdateLOD()
        {
            if (_playerTransform == null)
            {
                // Try to lazily resolve player
                Component fc = FindObjectOfType(
                    Type.GetType("SWEF.Flight.FlightController, Assembly-CSharp") ?? typeof(MonoBehaviour)) as Component;
                if (fc != null) _playerTransform = fc.transform;
                else return;
            }

            float dist = Vector3.Distance(transform.position, _playerTransform.position);

            float iconDist    = 50000f;
            float lowPolyDist = 10000f;

            NPCVisualLOD target;
            if      (dist > iconDist)    target = NPCVisualLOD.Icon;
            else if (dist > lowPolyDist) target = NPCVisualLOD.LowPoly;
            else                         target = NPCVisualLOD.FullModel;

            if (target == _currentLOD) return;

            _currentLOD = target;
            SetLOD(target);
            OnLODChanged?.Invoke(target);
        }

        private void SetLOD(NPCVisualLOD lod)
        {
            if (_iconObject     != null) _iconObject.SetActive(lod == NPCVisualLOD.Icon);
            if (_lowPolyObject  != null) _lowPolyObject.SetActive(lod == NPCVisualLOD.LowPoly);
            if (_fullModelObject != null) _fullModelObject.SetActive(lod == NPCVisualLOD.FullModel);

            bool showLights = lod == NPCVisualLOD.FullModel;
            if (_portLight      != null) _portLight.enabled      = showLights;
            if (_starboardLight != null) _starboardLight.enabled = showLights;
            if (_tailLight      != null) _tailLight.enabled      = showLights;
            if (_beaconLight    != null) _beaconLight.enabled    = showLights;
        }

        #endregion

        #region Private — Beacon

        private void UpdateBeacon()
        {
            if (_beaconLight == null || _currentLOD != NPCVisualLOD.FullModel) return;

            if (Time.time >= _nextBeaconFlip)
            {
                _beaconOn          = !_beaconOn;
                _beaconLight.enabled = _beaconOn;
                _nextBeaconFlip    = Time.time + _beaconFlashInterval * 0.5f;
            }
        }

        #endregion

        #region Private — Contrail

        private void UpdateContrail()
        {
            if (_contrailParticles == null || _controller.Data == null) return;

            bool shouldEmit = _controller.Data.AltitudeMetres >= _contrailMinAltitudeMetres;
            ParticleSystem.EmissionModule emission = _contrailParticles.emission;
            emission.enabled = shouldEmit;
        }

        #endregion

        #region Private — Helpers

        private static Color CategoryToColor(NPCAircraftCategory category) =>
            category switch
            {
                NPCAircraftCategory.CommercialAirline => Color.white,
                NPCAircraftCategory.PrivateJet        => new Color(0.8f, 0.8f, 1f),
                NPCAircraftCategory.CargoPlane        => new Color(0.7f, 0.5f, 0.3f),
                NPCAircraftCategory.MilitaryAircraft  => new Color(0.4f, 0.5f, 0.4f),
                NPCAircraftCategory.Helicopter        => new Color(1f, 0.9f, 0.3f),
                NPCAircraftCategory.TrainingAircraft  => new Color(1f, 0.4f, 0.1f),
                _                                    => Color.grey
            };

        #endregion
    }
}
