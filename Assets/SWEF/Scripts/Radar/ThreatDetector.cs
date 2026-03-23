// ThreatDetector.cs — SWEF Radar & Threat Detection System (Phase 67)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Radar
{
    /// <summary>
    /// Analyzes the contacts tracked by <see cref="RadarSystem"/> and
    /// computes dynamic threat levels based on classification, distance,
    /// closing speed, and heading-toward-player.
    /// <para>
    /// Attach to the same persistent scene object as <see cref="RadarSystem"/>.
    /// Integrates with the Phase 65 <c>WarningSystem</c> via the conditional
    /// compile guard <c>SWEF_WARNINGSYSTEM_AVAILABLE</c>.
    /// </para>
    /// </summary>
    public class ThreatDetector : MonoBehaviour
    {
        #region Inspector

        [Header("Threat Detector — Timing")]
        [Tooltip("Seconds between full threat re-evaluations.")]
        [Min(0.1f)]
        /// <summary>Seconds between full threat re-evaluation passes.</summary>
        public float threatUpdateInterval = 1f;

        [Header("Threat Detector — Thresholds")]
        [Tooltip("Closing speed in m/s above which the threat level is escalated.")]
        [Min(0f)]
        /// <summary>Closing speed in m/s above which the threat level is escalated.</summary>
        public float closingSpeedThreshold = RadarConfig.ClosingSpeedThreshold;

        [Tooltip("Range in metres below which the threat level escalates to High.")]
        [Min(0f)]
        /// <summary>Range in metres below which the threat level escalates to High.</summary>
        public float threatRangeClose = RadarConfig.CloseRange;

        [Tooltip("Range in metres below which the threat level escalates to Medium.")]
        [Min(0f)]
        /// <summary>Range in metres below which the threat level escalates to Medium.</summary>
        public float threatRangeMedium = RadarConfig.MediumRange;

        [Header("Threat Detector — References")]
        [Tooltip("Transform of the player aircraft used for heading-toward calculations.")]
        /// <summary>Player transform used as the threat analysis origin.</summary>
        [SerializeField] private Transform _playerTransform;

        #endregion

        #region Runtime State

        /// <summary>All hostile/unknown contacts sorted by threat level (highest first).</summary>
        public List<RadarContact> prioritizedThreats { get; } = new List<RadarContact>();

        /// <summary>Number of contacts currently classified as Hostile.</summary>
        public int hostileCount { get; private set; }

        /// <summary>Number of contacts currently at <see cref="ThreatLevel.Imminent"/>.</summary>
        public int imminentThreatCount { get; private set; }

        private RadarSystem _radar;
        private Coroutine   _updateRoutine;

        #endregion

        #region Events

        /// <summary>
        /// Raised when a contact's threat level changes.  Provides the contact
        /// and its new threat level.
        /// </summary>
        public event Action<RadarContact, ThreatLevel> OnThreatLevelChanged;

        /// <summary>Raised when a contact reaches <see cref="ThreatLevel.Imminent"/>.</summary>
        public event Action<RadarContact> OnImminentThreat;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _radar = RadarSystem.Instance != null ? RadarSystem.Instance : FindFirstObjectByType<RadarSystem>();
            if (_playerTransform == null) _playerTransform = transform;
            _updateRoutine = StartCoroutine(ThreatUpdateRoutine());
        }

        private void OnDestroy()
        {
            if (_updateRoutine != null) StopCoroutine(_updateRoutine);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Computes the threat level for the given contact based on its
        /// classification, distance, closing speed, and heading toward player.
        /// </summary>
        /// <param name="contact">Contact to evaluate.</param>
        /// <returns>Computed <see cref="ThreatLevel"/>.</returns>
        public ThreatLevel EvaluateThreat(RadarContact contact)
        {
            if (contact == null) return ThreatLevel.None;

            // Non-hostile classifications are low-threat by default.
            if (contact.classification == ContactClassification.Friendly ||
                contact.classification == ContactClassification.Civilian ||
                contact.classification == ContactClassification.Landmark ||
                contact.classification == ContactClassification.Event)
                return ThreatLevel.None;

            if (contact.classification == ContactClassification.Neutral)
                return ThreatLevel.Low;

            // Hostile or Unknown — evaluate by distance and kinematics.
            ThreatLevel level = ThreatLevel.Low;

            if (contact.distance <= threatRangeClose)
                level = ThreatLevel.High;
            else if (contact.distance <= threatRangeMedium)
                level = ThreatLevel.Medium;

            // Escalate based on closing speed.
            if (_playerTransform != null)
            {
                Vector3 toPlayer = _playerTransform.position - contact.position;
                float closingSpeed = Vector3.Dot(contact.velocity, toPlayer.normalized);

                if (closingSpeed >= closingSpeedThreshold)
                    level = EscalateThreat(level);

                // Heading-toward check: contact's velocity is pointing at the player.
                if (contact.velocity.magnitude > 1f)
                {
                    float headingDot = Vector3.Dot(contact.velocity.normalized, toPlayer.normalized);
                    if (headingDot > 0.85f && contact.distance <= threatRangeClose)
                        level = ThreatLevel.Imminent;
                }
            }

            return level;
        }

        /// <summary>Returns the contact with the highest threat level, or <c>null</c> if none.</summary>
        public RadarContact GetHighestThreat()
        {
            if (prioritizedThreats.Count == 0) return null;
            return prioritizedThreats[0];
        }

        #endregion

        #region Private — Threat Update Loop

        private IEnumerator ThreatUpdateRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(threatUpdateInterval);

                if (_radar == null) continue;

                prioritizedThreats.Clear();
                hostileCount      = 0;
                imminentThreatCount = 0;

                foreach (RadarContact c in _radar.contacts)
                {
                    ThreatLevel newLevel = EvaluateThreat(c);

                    if (newLevel != c.threat)
                    {
                        c.threat = newLevel;
                        OnThreatLevelChanged?.Invoke(c, newLevel);

                        if (newLevel == ThreatLevel.Imminent)
                        {
                            OnImminentThreat?.Invoke(c);
                            TriggerWarningSystemAlert(c);
                        }
                    }

                    if (newLevel > ThreatLevel.None)
                        prioritizedThreats.Add(c);

                    if (c.classification == ContactClassification.Hostile)
                        hostileCount++;

                    if (newLevel == ThreatLevel.Imminent)
                        imminentThreatCount++;
                }

                // Sort highest threat first.
                prioritizedThreats.Sort((a, b) => ((int)b.threat).CompareTo((int)a.threat));
            }
        }

        private static ThreatLevel EscalateThreat(ThreatLevel level)
        {
            if (level < ThreatLevel.Imminent)
                return level + 1;
            return level;
        }

        // Integration with Phase 65 WarningSystem — guarded by compile define.
        private void TriggerWarningSystemAlert(RadarContact contact)
        {
#if SWEF_WARNINGSYSTEM_AVAILABLE
            var ws = FindFirstObjectByType<SWEF.CockpitHUD.WarningSystem>();
            if (ws != null)
                ws.AddWarning("THREAT", $"IMMINENT THREAT: {contact.displayName}", SWEF.CockpitHUD.WarningLevel.Critical);
#endif
        }

        #endregion
    }
}
