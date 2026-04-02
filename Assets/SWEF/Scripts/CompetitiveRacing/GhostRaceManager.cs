// GhostRaceManager.cs — SWEF Competitive Racing & Time Trial System (Phase 88)
using System;
using System.Collections.Generic;
using UnityEngine;
using SWEF.Replay;

namespace SWEF.CompetitiveRacing
{
    /// <summary>
    /// Phase 88 — MonoBehaviour that extends ghost racing for course-based races.
    /// Manages up to <see cref="CompetitiveRacingConfig.MaxSimultaneousGhosts"/>
    /// simultaneous ghost racers (personal best, global best, friend best) and
    /// forwards per-checkpoint comparison data to subscribers.
    ///
    /// <para>Works alongside <see cref="RaceManager"/>: subscribe to
    /// <see cref="RaceManager.OnRaceStarted"/> to auto-load ghosts for the active
    /// course.</para>
    /// </summary>
    public class GhostRaceManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Ghost Prefab")]
        [Tooltip("Prefab with a GhostRacer component.  Instantiated for each ghost slot.")]
        [SerializeField] private GameObject _ghostRacerPrefab;

        // ── Public State ──────────────────────────────────────────────────────────

        /// <summary>All currently active ghost racer instances.</summary>
        public IReadOnlyList<GhostRacer> activeGhosts => _activeGhosts;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when any ghost passes a checkpoint. Args: (ghostIndex, checkpointIndex, splitTime).</summary>
        public event Action<int, int, float> OnGhostCheckpoint;

        /// <summary>Raised when a ghost racer finishes the course.</summary>
        public event Action<int>             OnGhostFinished;

        // ── Private State ─────────────────────────────────────────────────────────

        private readonly List<GhostRacer> _activeGhosts = new List<GhostRacer>();
        private RaceCourse _currentCourse;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (RaceManager.Instance != null)
                RaceManager.Instance.OnRaceStarted += HandleRaceStarted;
        }

        private void OnDisable()
        {
            if (RaceManager.Instance != null)
                RaceManager.Instance.OnRaceStarted -= HandleRaceStarted;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Starts ghost racing for <paramref name="course"/> using the provided
        /// <paramref name="ghostReplays"/> (up to <see cref="CompetitiveRacingConfig.MaxSimultaneousGhosts"/>).
        /// </summary>
        public void StartGhostRace(RaceCourse course, IList<ReplayData> ghostReplays)
        {
            if (course == null) return;
            _currentCourse = course;

            StopAllGhosts();

            int count = Mathf.Min(ghostReplays?.Count ?? 0,
                                  CompetitiveRacingConfig.MaxSimultaneousGhosts);

            for (int i = 0; i < count; i++)
            {
                if (ghostReplays![i] == null) continue;

                GhostRacer ghost = SpawnGhost(i);
                if (ghost == null) continue;

                int capturedIndex = i; // closure capture
                ghost.OnRaceFinished += () => HandleGhostFinished(capturedIndex);
                ghost.StartRace(ghostReplays[i]);
            }

            Debug.Log($"[SWEF] GhostRaceManager: Started {_activeGhosts.Count} ghost(s) for '{course.courseName}'.");
        }

        /// <summary>Stops and destroys all active ghost instances.</summary>
        public void StopAllGhosts()
        {
            foreach (var g in _activeGhosts)
            {
                if (g != null)
                {
                    g.FinishRace();
                    Destroy(g.gameObject);
                }
            }
            _activeGhosts.Clear();
        }

        /// <summary>Returns the per-checkpoint time delta for the ghost at <paramref name="ghostIndex"/>.</summary>
        public float GetGhostTimeDelta(int ghostIndex)
        {
            if (ghostIndex < 0 || ghostIndex >= _activeGhosts.Count) return 0f;
            return _activeGhosts[ghostIndex] != null ? _activeGhosts[ghostIndex].TimeDelta : 0f;
        }

        // ── Private Helpers ───────────────────────────────────────────────────────

        private void HandleRaceStarted(RaceCourse course)
        {
            // No-op: caller should explicitly call StartGhostRace with desired replays.
        }

        private void HandleGhostFinished(int ghostIndex)
        {
            OnGhostFinished?.Invoke(ghostIndex);
            Debug.Log($"[SWEF] GhostRaceManager: Ghost {ghostIndex} finished.");
        }

        private GhostRacer SpawnGhost(int slot)
        {
            if (_ghostRacerPrefab == null)
            {
                Debug.LogWarning("[SWEF] GhostRaceManager: No ghost prefab assigned.");
                return null;
            }

            var go    = Instantiate(_ghostRacerPrefab);
            var ghost = go.GetComponent<GhostRacer>();
            if (ghost == null)
            {
                Debug.LogWarning("[SWEF] GhostRaceManager: Ghost prefab missing GhostRacer component.");
                Destroy(go);
                return null;
            }

            _activeGhosts.Add(ghost);
            return ghost;
        }
    }
}
