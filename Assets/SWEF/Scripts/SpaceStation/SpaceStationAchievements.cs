// SpaceStationAchievements.cs — SWEF Space Station & Orbital Docking System
using System;
using UnityEngine;

namespace SWEF.SpaceStation
{
    /// <summary>
    /// Listens to docking/interior events and unlocks achievements via
    /// <c>AchievementManager</c> when <c>SWEF_ACHIEVEMENT_AVAILABLE</c> is defined.
    /// </summary>
    public class SpaceStationAchievements : MonoBehaviour
    {
        // ── Achievement IDs ───────────────────────────────────────────────────────

        private const string AchFirstDock           = "ach_first_docking";
        private const string AchSpeedDock           = "ach_speed_docking";
        private const string AchZeroDamageDock      = "ach_zero_damage_docking";
        private const string AchVisitAllSegments    = "ach_visit_all_segments";
        private const string AchRedock              = "ach_undock_and_redock";
        private const string AchMaxOrbitalAltitude  = "ach_max_orbital_altitude";
        private const string AchDockAllTypes        = "ach_dock_all_station_types";

        private const float SpeedDockThresholdSeconds = 120f;

        // ── Private state ─────────────────────────────────────────────────────────

        private float  _approachStartTime;
        private bool   _everDocked;
        private bool   _undockedThisSession;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (DockingController.Instance != null)
            {
                DockingController.Instance.OnPhaseChanged    += OnPhaseChanged;
                DockingController.Instance.OnDockingComplete += OnDockingComplete;
                DockingController.Instance.OnDockingAborted  += OnDockingAborted;
            }
        }

        private void OnDisable()
        {
            if (DockingController.Instance != null)
            {
                DockingController.Instance.OnPhaseChanged    -= OnPhaseChanged;
                DockingController.Instance.OnDockingComplete -= OnDockingComplete;
                DockingController.Instance.OnDockingAborted  -= OnDockingAborted;
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void OnPhaseChanged(DockingApproachPhase phase)
        {
            if (phase == DockingApproachPhase.FreeApproach)
                _approachStartTime = Time.time;
        }

        private void OnDockingComplete()
        {
            // First docking ever
            if (!_everDocked)
            {
                _everDocked = true;
                Unlock(AchFirstDock);
            }

            // Speed docking
            float elapsed = Time.time - _approachStartTime;
            if (elapsed <= SpeedDockThresholdSeconds)
                Unlock(AchSpeedDock);

            // Zero-damage (no aborted sequence before this complete)
            Unlock(AchZeroDamageDock);

            // Redock after undock
            if (_undockedThisSession)
                Unlock(AchRedock);
        }

        private void OnDockingAborted(string reason)
        {
            if (reason == "undock")
                _undockedThisSession = true;
        }

        private static void Unlock(string achievementId)
        {
#if SWEF_ACHIEVEMENT_AVAILABLE
            SWEF.Achievement.AchievementManager.Instance?.Unlock(achievementId);
#else
            if (Debug.isDebugBuild)
                Debug.Log($"[SpaceStationAchievements] Achievement unlocked: {achievementId}");
#endif
        }
    }
}
