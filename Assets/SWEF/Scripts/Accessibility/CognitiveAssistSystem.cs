using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Accessibility
{
    // ── Enums ────────────────────────────────────────────────────────────────────

    /// <summary>HUD information density level.</summary>
    public enum HUDInfoLevel
    {
        /// <summary>All HUD elements visible.</summary>
        Full = 0,
        /// <summary>Secondary elements hidden; critical info shown.</summary>
        Reduced = 1,
        /// <summary>Only altitude, speed, and waypoint arrow visible.</summary>
        Minimal = 2
    }

    /// <summary>
    /// Cognitive accessibility aids: simplified flight, pace control, information
    /// reduction, reminder system, tutorial replay, and auto-difficulty adjustment.
    /// </summary>
    public class CognitiveAssistSystem : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static CognitiveAssistSystem Instance { get; private set; }

        // ── PlayerPrefs keys ─────────────────────────────────────────────────────
        private const string KeySimplified     = "SWEF_CogSimplified";
        private const string KeyGameSpeed      = "SWEF_CogGameSpeed";
        private const string KeyInfoLevel      = "SWEF_CogInfoLevel";
        private const string KeyReminders      = "SWEF_CogReminders";
        private const string KeyReminderInt    = "SWEF_CogReminderInterval";

        // ── Serialised fields ────────────────────────────────────────────────────
        [Header("Simplified Flight")]
        [SerializeField] private bool simplifiedFlightEnabled = false;
        [Tooltip("When enabled, auto-manages altitude and speed; player only steers direction.")]
        [SerializeField] private float targetAltitude = 1000f;
        [SerializeField] private float targetSpeed    = 150f;
        [SerializeField] [Range(0f, 1f)] private float autoManageStrength = 0.5f;

        [Header("Pace Control")]
        [SerializeField] private float gameSpeed = 1f;
        private static readonly float[] AllowedSpeeds = { 0.25f, 0.5f, 0.75f, 1.0f };

        [Header("Information Reduction")]
        [SerializeField] private HUDInfoLevel hudInfoLevel = HUDInfoLevel.Full;
        [SerializeField] private List<GameObject> secondaryHUDElements = new List<GameObject>();
        [SerializeField] private List<GameObject> minimalHUDElements   = new List<GameObject>();

        [Header("Reminder System")]
        [SerializeField] private bool  remindersEnabled   = true;
        [SerializeField] [Range(10f, 300f)] private float reminderInterval = 30f;
        [SerializeField] private float reminderCooldown   = 60f;

        [Header("Pause Anywhere")]
        [SerializeField] private bool allowPauseAnywhere = true;

        // ── Runtime state ─────────────────────────────────────────────────────────
        private Coroutine _reminderRoutine;
        private readonly Dictionary<string, float> _lastReminderTime =
            new Dictionary<string, float>(StringComparer.Ordinal);

        // Performance tracking for auto-difficulty
        private int   _deathCount;
        private int   _retryCount;
        private float _sessionStartTime;
        private float _currentDifficultyMultiplier = 1f;

        /// <summary>Current game speed multiplier.</summary>
        public float GameSpeed => gameSpeed;

        /// <summary>Whether simplified flight mode is active.</summary>
        public bool SimplifiedFlightEnabled => simplifiedFlightEnabled;

        /// <summary>Current HUD information density level.</summary>
        public HUDInfoLevel HUDLevel => hudInfoLevel;

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>Fired when simplified-flight mode is toggled.</summary>
        public event Action<bool> OnSimplifiedModeToggled;

        /// <summary>Fired when game speed changes.</summary>
        public event Action<float> OnGameSpeedChanged;

        /// <summary>Fired when the HUD info level changes.</summary>
        public event Action<HUDInfoLevel> OnInfoLevelChanged;

        /// <summary>Fired when auto-difficulty adjusts the multiplier.</summary>
        public event Action<float> OnDifficultyAdjusted;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadPreferences();
            _sessionStartTime = Time.realtimeSinceStartup;
        }

        private void Start()
        {
            ApplyGameSpeed(gameSpeed);
            ApplyInfoLevel(hudInfoLevel);

            if (remindersEnabled)
                _reminderRoutine = StartCoroutine(ReminderLoop());
        }

        private void OnDestroy()
        {
            // Restore time scale if this object is destroyed
            Time.timeScale = 1f;
        }

        // ── Simplified flight ─────────────────────────────────────────────────────

        /// <summary>
        /// Enables or disables simplified-flight mode. When enabled, call
        /// <see cref="UpdateSimplifiedFlight"/> each FixedUpdate from the flight controller.
        /// </summary>
        public void SetSimplifiedFlight(bool enabled)
        {
            simplifiedFlightEnabled = enabled;
            PlayerPrefs.SetInt(KeySimplified, enabled ? 1 : 0);
            PlayerPrefs.Save();
            OnSimplifiedModeToggled?.Invoke(enabled);
        }

        /// <summary>
        /// Returns corrective forces for altitude and speed management.
        /// Call from the flight controller's FixedUpdate when simplified flight is active.
        /// </summary>
        /// <param name="currentAltitude">Current aircraft altitude in metres.</param>
        /// <param name="currentSpeed">Current airspeed in km/h.</param>
        /// <returns>(<c>throttleDelta</c>, <c>pitchDelta</c>) corrections.</returns>
        public (float throttleDelta, float pitchDelta) UpdateSimplifiedFlight(float currentAltitude, float currentSpeed)
        {
            if (!simplifiedFlightEnabled) return (0f, 0f);

            float altError   = (targetAltitude - currentAltitude) * 0.001f;
            float speedError = (targetSpeed    - currentSpeed)    * 0.01f;

            float pitchDelta    = Mathf.Clamp(altError   * autoManageStrength, -1f, 1f);
            float throttleDelta = Mathf.Clamp(speedError * autoManageStrength, -1f, 1f);

            return (throttleDelta, pitchDelta);
        }

        // ── Pace control ─────────────────────────────────────────────────────────

        /// <summary>
        /// Sets the game speed to one of the allowed values: 0.25×, 0.5×, 0.75×, or 1.0×.
        /// </summary>
        public void SetGameSpeed(float speed)
        {
            float closest = 1f;
            float minDist = float.MaxValue;
            foreach (float allowed in AllowedSpeeds)
            {
                float dist = Mathf.Abs(speed - allowed);
                if (dist < minDist) { minDist = dist; closest = allowed; }
            }
            gameSpeed = closest;
            ApplyGameSpeed(gameSpeed);
            PlayerPrefs.SetFloat(KeyGameSpeed, gameSpeed);
            PlayerPrefs.Save();
            OnGameSpeedChanged?.Invoke(gameSpeed);
        }

        private static void ApplyGameSpeed(float speed) => Time.timeScale = speed;

        // ── Information reduction ─────────────────────────────────────────────────

        /// <summary>
        /// Changes the HUD information density level.
        /// </summary>
        public void SetInfoLevel(HUDInfoLevel level)
        {
            hudInfoLevel = level;
            ApplyInfoLevel(level);
            PlayerPrefs.SetInt(KeyInfoLevel, (int)level);
            PlayerPrefs.Save();
            OnInfoLevelChanged?.Invoke(level);
        }

        private void ApplyInfoLevel(HUDInfoLevel level)
        {
            bool showSecondary = level == HUDInfoLevel.Full;
            bool showMinimal   = level != HUDInfoLevel.Minimal;

            foreach (var go in secondaryHUDElements)
                if (go != null) go.SetActive(showSecondary);

            foreach (var go in minimalHUDElements)
                if (go != null) go.SetActive(showMinimal);
        }

        // ── Reminder system ──────────────────────────────────────────────────────

        /// <summary>Enables or disables the objective reminder system.</summary>
        public void SetReminders(bool enabled)
        {
            remindersEnabled = enabled;
            PlayerPrefs.SetInt(KeyReminders, enabled ? 1 : 0);
            PlayerPrefs.Save();

            if (enabled && _reminderRoutine == null)
                _reminderRoutine = StartCoroutine(ReminderLoop());
            else if (!enabled && _reminderRoutine != null)
            {
                StopCoroutine(_reminderRoutine);
                _reminderRoutine = null;
            }
        }

        /// <summary>
        /// Sends a reminder message if the cooldown for that message key has expired.
        /// </summary>
        /// <param name="key">Unique reminder key to enforce cooldown.</param>
        /// <param name="message">Message text to display / speak.</param>
        public void TriggerReminder(string key, string message)
        {
            float now = Time.realtimeSinceStartup;
            if (_lastReminderTime.TryGetValue(key, out float last) && (now - last) < reminderCooldown)
                return;

            _lastReminderTime[key] = now;
            Debug.Log($"[SWEF CogAssist] Reminder: {message}");

            // Route to screen reader if available
            ScreenReaderBridge.Instance?.Announce(message, SpeechPriority.Low);
        }

        private IEnumerator ReminderLoop()
        {
            while (remindersEnabled)
            {
                yield return new WaitForSecondsRealtime(reminderInterval);
                // Subclasses or mission systems register their reminder callbacks externally
                OnReminderTick?.Invoke();
            }
            _reminderRoutine = null;
        }

        /// <summary>
        /// Invoked each reminder interval tick.
        /// Subscribe to this from mission systems to fire context-sensitive reminders.
        /// </summary>
        public event Action OnReminderTick;

        // ── Pause anywhere ───────────────────────────────────────────────────────

        /// <summary>
        /// Forces a pause regardless of current game state when <c>allowPauseAnywhere</c> is set.
        /// </summary>
        public bool TryForcePause()
        {
            if (!allowPauseAnywhere) return false;
            Time.timeScale = 0f;
            Debug.Log("[SWEF CogAssist] Force-paused.");
            return true;
        }

        /// <summary>Resumes from a force-paused state.</summary>
        public void ForceResume()
        {
            Time.timeScale = gameSpeed;
        }

        // ── Auto-difficulty ──────────────────────────────────────────────────────

        /// <summary>Records a player death for auto-difficulty tracking.</summary>
        public void RecordDeath()
        {
            _deathCount++;
            EvaluateAutoDifficulty();
        }

        /// <summary>Records a level retry for auto-difficulty tracking.</summary>
        public void RecordRetry()
        {
            _retryCount++;
            EvaluateAutoDifficulty();
        }

        private void EvaluateAutoDifficulty()
        {
            float elapsed = Time.realtimeSinceStartup - _sessionStartTime;
            float deathRate = elapsed > 0f ? _deathCount / elapsed : 0f;

            float newMultiplier;
            if (deathRate > 0.05f || _retryCount > 5)
                newMultiplier = Mathf.Max(0.5f, _currentDifficultyMultiplier - 0.1f);
            else if (deathRate < 0.005f && _retryCount == 0)
                newMultiplier = Mathf.Min(1.5f, _currentDifficultyMultiplier + 0.05f);
            else
                return;

            if (Mathf.Approximately(newMultiplier, _currentDifficultyMultiplier)) return;
            _currentDifficultyMultiplier = newMultiplier;
            OnDifficultyAdjusted?.Invoke(_currentDifficultyMultiplier);
            Debug.Log($"[SWEF CogAssist] Auto-difficulty adjusted: {_currentDifficultyMultiplier:F2}×");
        }

        /// <summary>Current auto-difficulty multiplier (0.5–1.5).</summary>
        public float DifficultyMultiplier => _currentDifficultyMultiplier;

        // ── Persistence ──────────────────────────────────────────────────────────
        private void LoadPreferences()
        {
            simplifiedFlightEnabled = PlayerPrefs.GetInt(KeySimplified, 0) == 1;
            gameSpeed               = PlayerPrefs.GetFloat(KeyGameSpeed, 1f);
            int infoRaw = PlayerPrefs.GetInt(KeyInfoLevel, 0);
            hudInfoLevel = Enum.IsDefined(typeof(HUDInfoLevel), infoRaw)
                ? (HUDInfoLevel)infoRaw
                : HUDInfoLevel.Full;
            remindersEnabled        = PlayerPrefs.GetInt(KeyReminders, 1) == 1;
            reminderInterval        = PlayerPrefs.GetFloat(KeyReminderInt, 30f);
        }
    }
}
