// SpaceStationInterior.cs — Phase 114: Satellite & Space Debris Tracking
// Post-docking interior exploration: modules, cupola view, microgravity movement.
// Namespace: SWEF.SatelliteTracking

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SatelliteTracking
{
    /// <summary>
    /// Manages the ISS interior exploration experience after a successful hard-dock.
    /// Handles module navigation, cupola Earth observation, and microgravity movement simulation.
    /// </summary>
    public class SpaceStationInterior : MonoBehaviour
    {
        // ── Enums ─────────────────────────────────────────────────────────────────

        /// <summary>Available ISS modules the player can visit.</summary>
        public enum ISSModule
        {
            /// <summary>Unity Node 1 — central hub and first US element.</summary>
            Unity,
            /// <summary>Zvezda Service Module — Russian segment, crew quarters.</summary>
            Zvezda,
            /// <summary>Zarya Functional Cargo Block — first ISS element.</summary>
            Zarya,
            /// <summary>Destiny Laboratory — primary US science module.</summary>
            Destiny,
            /// <summary>Harmony Node 2 — connecting node for Japanese and European modules.</summary>
            Harmony,
            /// <summary>Columbus Laboratory — ESA science module.</summary>
            Columbus,
            /// <summary>Kibo Japanese Experiment Module.</summary>
            Kibo,
            /// <summary>Cupola observation module — panoramic Earth view.</summary>
            Cupola
        }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Module Transforms")]
        [Tooltip("Root transforms for each ISS module interior (indexed by ISSModule enum).")]
        [SerializeField] private Transform[] moduleRoots;

        [Header("Player")]
        [Tooltip("Player character transform for microgravity movement.")]
        [SerializeField] private Transform playerTransform;

        [Tooltip("Microgravity movement speed (m/s).")]
        [SerializeField] private float microgravitySpeed = 1.5f;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when the player enters a new module.</summary>
        public event Action<ISSModule> OnModuleEntered;

        /// <summary>Raised when the interior exploration session ends.</summary>
        public event Action OnExplorationEnded;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>The module currently occupied by the player.</summary>
        public ISSModule CurrentModule { get; private set; } = ISSModule.Unity;

        /// <summary>Whether the interior exploration mode is active.</summary>
        public bool IsActive { get; private set; }

        // ── Private state ─────────────────────────────────────────────────────────
        private readonly List<ISSModule> _visitedModules = new List<ISSModule>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            // Subscribe to docking events
            var dockingCtrl = DockingScenarioController.Instance;
            if (dockingCtrl != null) dockingCtrl.OnHardDock += HandleHardDock;
        }

        private void OnDestroy()
        {
            var dockingCtrl = DockingScenarioController.Instance;
            if (dockingCtrl != null) dockingCtrl.OnHardDock -= HandleHardDock;
        }

        private void Update()
        {
            if (!IsActive || playerTransform == null) return;
            HandleMicrogravityMovement();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Activates interior exploration mode.</summary>
        public void EnterInterior()
        {
            IsActive = true;
            NavigateTo(ISSModule.Unity);
        }

        /// <summary>Exits interior exploration mode.</summary>
        public void ExitInterior()
        {
            IsActive = false;
            OnExplorationEnded?.Invoke();
        }

        /// <summary>Navigates the player to the specified module.</summary>
        public void NavigateTo(ISSModule module)
        {
            CurrentModule = module;
            if (!_visitedModules.Contains(module))
                _visitedModules.Add(module);

            // Move player to module root
            int idx = (int)module;
            if (playerTransform != null && moduleRoots != null && idx < moduleRoots.Length
                && moduleRoots[idx] != null)
            {
                playerTransform.position = moduleRoots[idx].position;
                playerTransform.rotation = moduleRoots[idx].rotation;
            }

            OnModuleEntered?.Invoke(module);
            Debug.Log($"[SpaceStationInterior] Entered module: {module}");
        }

        /// <summary>Returns all modules the player has visited this session.</summary>
        public IReadOnlyList<ISSModule> GetVisitedModules() => _visitedModules.AsReadOnly();

        /// <summary>True if the player is currently in the Cupola module.</summary>
        public bool IsInCupola => CurrentModule == ISSModule.Cupola;

        // ── Private helpers ───────────────────────────────────────────────────────

        private void HandleHardDock() => EnterInterior();

        private void HandleMicrogravityMovement()
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            float upDown = Input.GetKey(KeyCode.Space)    ? 1f
                         : Input.GetKey(KeyCode.LeftShift) ? -1f : 0f;

            var move = new Vector3(h, upDown, v) * (microgravitySpeed * Time.deltaTime);
            playerTransform.Translate(move, Space.Self);
        }
    }
}
