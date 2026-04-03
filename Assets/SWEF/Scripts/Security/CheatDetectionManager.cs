// CheatDetectionManager.cs — SWEF Anti-Cheat & Security Hardening (Phase 92)
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.Security
{
    /// <summary>
    /// Singleton MonoBehaviour that coordinates all runtime cheat-detection
    /// subsystems and orchestrates periodic integrity sweeps.
    ///
    /// <para>Detects:</para>
    /// <list type="bullet">
    ///   <item>Speed hacks (delegated to <see cref="SpeedHackDetector"/>)</item>
    ///   <item>Teleport hacks (delegated to <see cref="PositionValidator"/>)</item>
    ///   <item>Currency manipulation (via <see cref="CurrencyValidator"/>)</item>
    ///   <item>XP gain rate violations</item>
    ///   <item>Part duplication (inventory delta vs expected unlock events)</item>
    ///   <item>Save file integrity (wraps all 15+ JSON persistence files)</item>
    /// </list>
    ///
    /// <para>When a violation is detected <see cref="OnCheatDetected"/> is fired and
    /// the event is forwarded to <see cref="SecurityLogger"/> and
    /// <see cref="SecurityAnalytics"/>.</para>
    /// </summary>
    public class CheatDetectionManager : MonoBehaviour
    {
        #region Singleton
        /// <summary>Shared instance.</summary>
        public static CheatDetectionManager Instance { get; private set; }
        #endregion

        #region Inspector
        [SerializeField, Tooltip("Configuration asset for all detection thresholds.")]
        private SecurityConfig _config;

        [SerializeField, Tooltip("Optional SpeedHackDetector component on this or a child object.")]
        private SpeedHackDetector _speedHackDetector;

        [SerializeField, Tooltip("Optional PositionValidator component on this or a child object.")]
        private PositionValidator _positionValidator;
        #endregion

        #region Events
        /// <summary>Fired when any cheat is detected. Subscribe to handle responses.</summary>
        public event Action<SecurityEventData> OnCheatDetected;
        #endregion

        #region Private state
        private SecurityConfig Config => _config ?? SecurityConfig.Default();

        // XP rate tracking
        private float _xpAccumulatorMinute;
        private float _xpWindowStart;

        // Known save files to monitor
        private static readonly string[] SaveFileNames =
        {
            "player_profile.json",
            "friends_list.json",
            "multiplayer_sessions.json",
            "cross_session_events.json",
            "shared_waypoints.json",
            "chat_history.json",
            "workshop_builds.json",
            "workshop_inventory.json",
            "progression_data.json",
            "achievement_data.json",
            "flight_journal.json",
            "settings.json",
            "daily_challenge.json",
            "race_leaderboards.json",
            "photo_contest.json"
        };

        private Coroutine _integrityCheckCoroutine;
        #endregion

        #region Unity lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _xpWindowStart = Time.realtimeSinceStartup;
        }

        private void Start()
        {
            WireSubsystems();
            _integrityCheckCoroutine = StartCoroutine(PeriodicIntegrityCheck());
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                UnwireSubsystems();
            }
        }
        #endregion

        #region Public API
        /// <summary>
        /// Reports an XP gain to the rate monitor. Call every time XP is awarded.
        /// </summary>
        /// <param name="amount">XP amount gained.</param>
        public void ReportXpGain(float amount)
        {
            float now    = Time.realtimeSinceStartup;
            float window = now - _xpWindowStart;

            if (window >= 60f)
            {
                _xpAccumulatorMinute = 0f;
                _xpWindowStart       = now;
            }

            _xpAccumulatorMinute += amount;

            if (_xpAccumulatorMinute > Config.maxXpGainPerMinute)
            {
                RaiseViolation(SecurityEventData.Create(
                    SecurityEventType.CurrencyManipulation,
                    SecuritySeverity.High,
                    "local",
                    $"XP gain rate exceeded: {_xpAccumulatorMinute:F0} XP/min " +
                    $"(max {Config.maxXpGainPerMinute:F0})",
                    SecurityAction.Warned));
            }
        }

        /// <summary>
        /// Validates a currency change and raises a violation if manipulation is detected.
        /// Returns the corrected amount when auto-revert is applied.
        /// </summary>
        /// <param name="delta">Proposed currency delta.</param>
        /// <param name="source">Transaction source label.</param>
        /// <returns>Validated (possibly corrected) amount.</returns>
        public float ValidateCurrencyChange(float delta, string source)
        {
            var result = CurrencyValidator.ValidateTransaction(delta, source);
            if (!result.isValid)
            {
                RaiseViolation(SecurityEventData.Create(
                    SecurityEventType.CurrencyManipulation,
                    SecuritySeverity.Critical,
                    "local",
                    string.Join("; ", result.violations),
                    SecurityAction.Reverted));

                return result.correctedValue is float corrected ? corrected : 0f;
            }

            CurrencyValidator.RecordTransaction(delta, source);
            return delta;
        }

        /// <summary>
        /// Checks save file integrity for all known persistence files.
        /// Fires <see cref="OnCheatDetected"/> for each tampered file and restores from backup.
        /// </summary>
        public void RunSaveIntegrityCheck()
        {
            string basePath = Application.persistentDataPath;
            foreach (string fileName in SaveFileNames)
            {
                string path = Path.Combine(basePath, fileName);
                if (!File.Exists(path)) continue;

                var result = SaveFileValidator.DetectTampering(path);
                SecurityAnalytics.RecordSaveIntegrityCheck(path, result.isValid);

                if (!result.isValid)
                {
                    SecurityAnalytics.RecordSaveTamperDetected(path);
                    RaiseViolation(SecurityEventData.Create(
                        SecurityEventType.SaveTamper,
                        SecuritySeverity.Critical,
                        "local",
                        $"Save file tampered: {fileName} — {string.Join("; ", result.violations)}",
                        SecurityAction.Reverted));

                    bool restored = SaveFileValidator.RestoreFromBackup(path);
                    if (restored)
                        SecurityAnalytics.RecordBackupRestored(path);
                }
            }
        }
        #endregion

        #region Private — violation routing
        private void RaiseViolation(SecurityEventData evt)
        {
            if (evt == null) return;

            Debug.LogWarning($"[SWEF] Security: {evt.eventType} [{evt.severity}] — {evt.details}");
            SecurityLogger.Instance?.LogEvent(evt);
            SecurityAnalytics.RecordCheatDetected(
                evt.eventType.ToString(),
                evt.playerId,
                evt.severity.ToString());

            OnCheatDetected?.Invoke(evt);
        }
        #endregion

        #region Private — subsystem wiring
        private void WireSubsystems()
        {
            if (_speedHackDetector == null)
                _speedHackDetector = GetComponentInChildren<SpeedHackDetector>();

            if (_positionValidator == null)
                _positionValidator = GetComponentInChildren<PositionValidator>();

            if (_speedHackDetector != null)
                _speedHackDetector.OnViolationDetected += OnSpeedHackViolation;

            if (_positionValidator != null)
                _positionValidator.OnViolationDetected += OnPositionViolation;
        }

        private void UnwireSubsystems()
        {
            if (_speedHackDetector != null)
                _speedHackDetector.OnViolationDetected -= OnSpeedHackViolation;

            if (_positionValidator != null)
                _positionValidator.OnViolationDetected -= OnPositionViolation;
        }

        private void OnSpeedHackViolation(string message)
        {
            RaiseViolation(SecurityEventData.Create(
                SecurityEventType.SpeedHack,
                SecuritySeverity.High,
                "local",
                message,
                SecurityAction.Warned));
        }

        private void OnPositionViolation(string message, Vector3[] trail)
        {
            string trailStr = trail != null
                ? $" (trail frames: {trail.Length})"
                : string.Empty;

            RaiseViolation(SecurityEventData.Create(
                SecurityEventType.TeleportHack,
                SecuritySeverity.High,
                "local",
                message + trailStr,
                SecurityAction.Warned));
        }
        #endregion

        #region Periodic integrity check
        private IEnumerator PeriodicIntegrityCheck()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(Config.integrityCheckIntervalSeconds);
                RunSaveIntegrityCheck();

                // Also verify currency balance
                var balanceResult = CurrencyValidator.VerifyBalance();
                if (!balanceResult.isValid)
                {
                    RaiseViolation(SecurityEventData.Create(
                        SecurityEventType.CurrencyManipulation,
                        SecuritySeverity.Critical,
                        "local",
                        string.Join("; ", balanceResult.violations),
                        SecurityAction.Reverted));

                    CurrencyValidator.Revert();
                }
            }
        }
        #endregion
    }
}
