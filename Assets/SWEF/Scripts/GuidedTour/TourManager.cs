using System;
using System.Collections;
using UnityEngine;
using static SWEF.GuidedTour.TourData;

namespace SWEF.GuidedTour
{
    /// <summary>
    /// Manages the lifecycle of a guided tour: start, pause, resume, cancel, and complete.
    /// Auto-advances to the next waypoint when the player enters the trigger radius
    /// or after the waypoint's <see cref="WaypointData.stayDurationSeconds"/> expires.
    /// </summary>
    public class TourManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Singleton instance.</summary>
        public static TourManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Settings")]
        [SerializeField] private float triggerCheckInterval = 0.1f;

        // ── State ─────────────────────────────────────────────────────────────────
        /// <summary>The currently active tour, or <c>null</c> if no tour is running.</summary>
        public TourData ActiveTour { get; private set; }

        /// <summary>Zero-based index of the waypoint the player is currently heading to.</summary>
        public int CurrentWaypointIndex { get; private set; }

        /// <summary>Tour progress in [0, 1] range.</summary>
        public float Progress => ActiveTour == null || ActiveTour.waypoints.Count == 0
            ? 0f
            : (float)CurrentWaypointIndex / ActiveTour.waypoints.Count;

        /// <summary>Whether a tour is currently active (not paused and not null).</summary>
        public bool IsRunning { get; private set; }

        /// <summary>Whether the current tour is paused.</summary>
        public bool IsPaused { get; private set; }

        private Coroutine _tourCoroutine;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when a tour begins.</summary>
        public event Action<TourData> OnTourStarted;

        /// <summary>Fired when a waypoint is reached.</summary>
        public event Action<int, WaypointData> OnWaypointReached;

        /// <summary>Fired when the final waypoint is completed and the tour ends.</summary>
        public event Action<TourData> OnTourCompleted;

        /// <summary>Fired when the tour is cancelled before completion.</summary>
        public event Action<TourData> OnTourCancelled;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Starts the specified tour from the first waypoint.
        /// Any tour currently in progress is cancelled first.
        /// </summary>
        /// <param name="tour">Tour definition to run.</param>
        public void StartTour(TourData tour)
        {
            if (tour == null)
            {
                Debug.LogWarning("[SWEF] TourManager.StartTour: tour is null.");
                return;
            }
            if (tour.waypoints == null || tour.waypoints.Count == 0)
            {
                Debug.LogWarning($"[SWEF] TourManager.StartTour: tour '{tour.tourId}' has no waypoints.");
                return;
            }

            if (IsRunning) CancelTour();

            ActiveTour            = tour;
            CurrentWaypointIndex  = 0;
            IsRunning             = true;
            IsPaused              = false;

            Debug.Log($"[SWEF] TourManager: Starting tour '{tour.tourId}'.");
            OnTourStarted?.Invoke(tour);

            _tourCoroutine = StartCoroutine(RunTourCoroutine());
        }

        /// <summary>Pauses the active tour. Has no effect if no tour is running.</summary>
        public void PauseTour()
        {
            if (!IsRunning || IsPaused) return;
            IsPaused = true;
            Debug.Log("[SWEF] TourManager: Tour paused.");
        }

        /// <summary>Resumes a paused tour. Has no effect if the tour is not paused.</summary>
        public void ResumeTour()
        {
            if (!IsRunning || !IsPaused) return;
            IsPaused = false;
            Debug.Log("[SWEF] TourManager: Tour resumed.");
        }

        /// <summary>Cancels the active tour and fires <see cref="OnTourCancelled"/>.</summary>
        public void CancelTour()
        {
            if (!IsRunning) return;

            if (_tourCoroutine != null)
            {
                StopCoroutine(_tourCoroutine);
                _tourCoroutine = null;
            }

            var tour = ActiveTour;
            IsRunning  = false;
            IsPaused   = false;
            ActiveTour = null;

            Debug.Log("[SWEF] TourManager: Tour cancelled.");
            OnTourCancelled?.Invoke(tour);
        }

        /// <summary>
        /// Skips directly to the waypoint at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">Zero-based waypoint index to jump to.</param>
        public void SkipToWaypoint(int index)
        {
            if (ActiveTour == null) return;
            if (index < 0 || index >= ActiveTour.waypoints.Count)
            {
                Debug.LogWarning($"[SWEF] TourManager.SkipToWaypoint: index {index} out of range.");
                return;
            }

            if (_tourCoroutine != null)
            {
                StopCoroutine(_tourCoroutine);
                _tourCoroutine = null;
            }

            CurrentWaypointIndex = index;
            _tourCoroutine = StartCoroutine(RunTourCoroutine());
            Debug.Log($"[SWEF] TourManager: Skipped to waypoint {index}.");
        }

        // ── Internal ──────────────────────────────────────────────────────────────
        private IEnumerator RunTourCoroutine()
        {
            var navigator = FindFirstObjectByType<WaypointNavigator>();

            while (CurrentWaypointIndex < ActiveTour.waypoints.Count)
            {
                // Wait while paused.
                while (IsPaused)
                    yield return null;

                var wp = ActiveTour.waypoints[CurrentWaypointIndex];

                // Wait for player to enter the trigger radius.
                if (navigator != null)
                {
                    while (navigator.DistanceToNextWaypoint > wp.triggerRadius)
                    {
                        while (IsPaused) yield return null;
                        yield return new WaitForSeconds(triggerCheckInterval);
                    }
                }

                // Notify listeners that this waypoint was reached.
                OnWaypointReached?.Invoke(CurrentWaypointIndex, wp);
                Debug.Log($"[SWEF] TourManager: Reached waypoint {CurrentWaypointIndex} '{wp.waypointName}'.");

                // Optionally dwell at the waypoint.
                if (wp.stayDurationSeconds > 0f)
                {
                    float elapsed = 0f;
                    while (elapsed < wp.stayDurationSeconds)
                    {
                        while (IsPaused) yield return null;
                        elapsed += Time.deltaTime;
                        yield return null;
                    }
                }

                CurrentWaypointIndex++;
            }

            // All waypoints visited — tour complete.
            var completedTour = ActiveTour;
            IsRunning  = false;
            IsPaused   = false;
            ActiveTour = null;

            Debug.Log("[SWEF] TourManager: Tour completed.");
            OnTourCompleted?.Invoke(completedTour);
        }
    }
}
