// MaritimeMissionUI.cs — Phase 117: Advanced Ocean & Maritime System
// Mission interface: SAR grid, rescue checklist, patrol waypoints.
// Namespace: SWEF.OceanSystem

using UnityEngine;
using UnityEngine.UI;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — UI panel for active maritime missions.
    /// Displays SAR search grid progress, rescue checklist items,
    /// patrol waypoint status, and cargo delivery timer.
    /// </summary>
    public class MaritimeMissionUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Panel Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("SAR Panel")]
        [SerializeField] private GameObject sarPanel;
        [SerializeField] private Text       sarMissionTitle;
        [SerializeField] private Text       sarSurvivorStatus;
        [SerializeField] private Text       sarSearchPattern;
        [SerializeField] private Text       sarTimeRemaining;
        [SerializeField] private Slider     sarSearchProgress;

        [Header("Patrol Panel")]
        [SerializeField] private GameObject patrolPanel;
        [SerializeField] private Text       patrolWaypointText;
        [SerializeField] private Text       patrolIllegalText;

        [Header("Cargo Panel")]
        [SerializeField] private GameObject cargoPanel;
        [SerializeField] private Text       cargoStateText;
        [SerializeField] private Text       cargoTimerText;
        [SerializeField] private Text       cargoWeightText;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Update()
        {
            if (panelRoot == null || !panelRoot.activeSelf) return;
            RefreshMissionData();
        }

        // ── Refresh ───────────────────────────────────────────────────────────────

        private void RefreshMissionData()
        {
            var msnMgr = MaritimeMissionManager.Instance;
            if (msnMgr == null) return;

            bool hasSAR = msnMgr.ActiveMissions.Count > 0;
            if (sarPanel != null) sarPanel.SetActive(hasSAR);

            if (hasSAR)
            {
                var sar = msnMgr.ActiveMissions[0];
                if (sarMissionTitle   != null) sarMissionTitle.text   = "SAR: " + sar.missionId;
                if (sarSurvivorStatus != null) sarSurvivorStatus.text = $"Rescued: {sar.rescuedCount}/{sar.survivorCount}";
                if (sarSearchPattern  != null) sarSearchPattern.text  = "Pattern: " + sar.searchPattern;
                if (sarTimeRemaining  != null) sarTimeRemaining.text  = $"Time: {sar.timeLimitSeconds:F0}s";
                if (sarSearchProgress != null) sarSearchProgress.value = sar.survivorCount > 0
                    ? (float)sar.rescuedCount / sar.survivorCount
                    : 0f;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Shows the mission UI panel.</summary>
        public void Show() { if (panelRoot != null) panelRoot.SetActive(true); }

        /// <summary>Hides the mission UI panel.</summary>
        public void Hide() { if (panelRoot != null) panelRoot.SetActive(false); }

        /// <summary>Updates the cargo delivery panel with current state.</summary>
        public void UpdateCargo(CargoDeliveryMission.DeliveryState state, float timerSec, float weightKg)
        {
            if (cargoPanel != null) cargoPanel.SetActive(true);
            if (cargoStateText != null) cargoStateText.text = "State: " + state;
            if (cargoTimerText != null) cargoTimerText.text = $"Time: {timerSec:F0}s";
            if (cargoWeightText!= null) cargoWeightText.text = $"Cargo: {weightKg:F0}kg";
        }

        /// <summary>Updates the patrol panel.</summary>
        public void UpdatePatrol(int currentWp, int totalWps, int illegalFound)
        {
            if (patrolPanel      != null) patrolPanel.SetActive(true);
            if (patrolWaypointText != null) patrolWaypointText.text = $"WP: {currentWp}/{totalWps}";
            if (patrolIllegalText  != null) patrolIllegalText.text  = "Illegal: " + illegalFound;
        }
    }
}
