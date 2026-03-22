using System.Collections.Generic;
using UnityEngine;

namespace SWEF.TimeCapsule
{
    /// <summary>
    /// Monitors flight telemetry and automatically creates <see cref="TimeCapsule"/> entries
    /// when the player crosses altitude or distance milestones. Requires a
    /// <see cref="TimeCapsuleManager"/> to be present in the scene.
    /// </summary>
    public class TimeCapsuleAutoCapture : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Altitude Milestones")]
        [Tooltip("Creates an auto-capsule every time the player's altitude increases by this many metres.")]
        [SerializeField] private float altitudeMilestoneInterval = 10000f;

        [Header("Distance Milestones")]
        [Tooltip("Creates an auto-capsule every time the player travels this many metres.")]
        [SerializeField] private float distanceMilestoneInterval = 100000f;

        [Header("Cooldown")]
        [Tooltip("Minimum time in seconds that must elapse between any two auto-captures.")]
        [SerializeField] private float minTimeBetweenAutoCaptures = 60f;

        // ── Internal state ────────────────────────────────────────────────────────
        private float _lastAltitudeMilestone;
        private float _lastDistanceMilestone;
        private float _lastCaptureTime = float.NegativeInfinity;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            // Reset milestones so the first real reading establishes the baseline.
            _lastAltitudeMilestone  = float.NegativeInfinity;
            _lastDistanceMilestone  = 0f;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Called each frame (or at any suitable interval) with the player's current altitude in
        /// metres. Creates an auto-capsule when the altitude has increased by
        /// <see cref="altitudeMilestoneInterval"/> metres beyond the last milestone.
        /// </summary>
        /// <param name="currentAltitude">Current altitude in metres above sea level.</param>
        public void CheckAltitudeMilestone(float currentAltitude)
        {
            if (!IsCooldownElapsed()) return;

            if (_lastAltitudeMilestone == float.NegativeInfinity)
            {
                // Establish the first baseline without triggering a capture.
                _lastAltitudeMilestone = Mathf.Floor(currentAltitude / altitudeMilestoneInterval)
                                         * altitudeMilestoneInterval;
                return;
            }

            float nextMilestone = _lastAltitudeMilestone + altitudeMilestoneInterval;
            if (currentAltitude >= nextMilestone)
            {
                _lastAltitudeMilestone = nextMilestone;
                TriggerAutoCapture(
                    $"Altitude Milestone: {nextMilestone:N0} m",
                    $"Reached {nextMilestone:N0} m altitude during this flight.",
                    TimeCapsuleType.Milestone,
                    currentAltitude);
            }
        }

        /// <summary>
        /// Called each frame (or at any suitable interval) with the total distance flown in
        /// metres during the current session. Creates an auto-capsule when the distance has
        /// exceeded the next <see cref="distanceMilestoneInterval"/> multiple.
        /// </summary>
        /// <param name="distance">Total distance travelled in metres.</param>
        public void CheckDistanceMilestone(float distance)
        {
            if (!IsCooldownElapsed()) return;

            float nextMilestone = _lastDistanceMilestone + distanceMilestoneInterval;
            if (distance >= nextMilestone)
            {
                _lastDistanceMilestone = nextMilestone;
                TriggerAutoCapture(
                    $"Distance Milestone: {nextMilestone / 1000f:N0} km",
                    $"Covered {nextMilestone / 1000f:N0} km of total flight distance.",
                    TimeCapsuleType.Milestone,
                    0f);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private bool IsCooldownElapsed()
            => Time.unscaledTime - _lastCaptureTime >= minTimeBetweenAutoCaptures;

        private void TriggerAutoCapture(string title, string description, TimeCapsuleType type, float altitude)
        {
            if (TimeCapsuleManager.Instance == null)
            {
                Debug.LogWarning("[TimeCapsuleAutoCapture] TimeCapsuleManager.Instance is null — skipping auto-capture.");
                return;
            }

            _lastCaptureTime = Time.unscaledTime;

            // Build lightweight snapshots — callers can extend these with real data.
            var location = new CapsuleLocation
            {
                altitude     = altitude,
                locationName = "In Flight"
            };

            TimeCapsuleManager.Instance.CreateCapsule(
                title:           title,
                description:     description,
                type:            type,
                location:        location,
                weather:         new CapsuleWeatherSnapshot(),
                flight:          new CapsuleFlightSnapshot(),
                screenshotPath:  string.Empty,
                tags:            new List<string> { "auto", "milestone" },
                personalNote:    string.Empty,
                delayDays:       0f,
                isAutoGenerated: true);

            Debug.Log($"[TimeCapsuleAutoCapture] Auto-capsule created: \"{title}\"");
        }
    }
}
