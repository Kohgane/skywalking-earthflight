using System;
using System.Collections;
using UnityEngine;
using SWEF.Flight;
using SWEF.Teleport;
using SWEF.Screenshot;
using SWEF.Achievement;

namespace SWEF.Haptic
{
    /// <summary>
    /// Bridges gameplay events to <see cref="HapticManager"/> triggers.
    /// Auto-wires to existing managers via <c>FindFirstObjectByType</c> and subscribes to
    /// their events in <c>Start</c>.  A per-pattern cooldown system prevents haptic spam.
    /// </summary>
    public class HapticTriggerZone : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────────
        [Header("Altitude Thresholds (metres)")]
        [SerializeField] private float altitudeWarningThreshold = 100000f;   // Kármán line approach
        [SerializeField] private float spaceEntryThreshold      = 120000f;   // Space entry

        [Header("Flight Thresholds")]
        [SerializeField] private float stallSpeedThreshold      = 10f;       // m/s
        [SerializeField] private float boostThrottleThreshold   = 0.9f;      // normalised 0–1

        [Header("Cooldowns (seconds)")]
        [SerializeField] private float altitudeWarningCooldown  = 5f;
        [SerializeField] private float stallCooldown            = 3f;
        [SerializeField] private float boostCooldown            = 0.1f;

        // ── Runtime references ───────────────────────────────────────────────────
        private AltitudeController  _altitude;
        private FlightController    _flight;
        private TeleportController  _teleport;
        private ScreenshotController _screenshot;
        private AchievementManager  _achievements;

        // ── Cooldown tracking ────────────────────────────────────────────────────
        private float _altWarningCooldownTimer;
        private float _stallCooldownTimer;
        private float _boostCooldownTimer;

        // ── Boost state ──────────────────────────────────────────────────────────
        private bool  _boostActive;
        private Coroutine _boostRoutine;

        // ── Space-entry state ────────────────────────────────────────────────────
        private bool _spaceEntryTriggered;
        private bool _altitudeWarningTriggered;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Start()
        {
            _altitude    = FindFirstObjectByType<AltitudeController>();
            _flight      = FindFirstObjectByType<FlightController>();
            _teleport    = FindFirstObjectByType<TeleportController>();
            _screenshot  = FindFirstObjectByType<ScreenshotController>();
            _achievements = FindFirstObjectByType<AchievementManager>();

            if (_teleport != null)
                _teleport.OnTeleportCompleted += OnTeleportCompleted;

            if (_screenshot != null)
                _screenshot.OnScreenshotCaptured += OnScreenshotCaptured;

            if (_achievements != null)
                _achievements.OnAchievementUnlocked += OnAchievementUnlocked;

            if (_flight != null)
            {
                _flight.OnBoostStarted  += OnBoostStarted;
                _flight.OnBoostEnded    += OnBoostEnded;
                _flight.OnStallWarning  += OnStallWarning;
            }
        }

        private void OnDestroy()
        {
            if (_teleport != null)
                _teleport.OnTeleportCompleted -= OnTeleportCompleted;

            if (_screenshot != null)
                _screenshot.OnScreenshotCaptured -= OnScreenshotCaptured;

            if (_achievements != null)
                _achievements.OnAchievementUnlocked -= OnAchievementUnlocked;

            if (_flight != null)
            {
                _flight.OnBoostStarted  -= OnBoostStarted;
                _flight.OnBoostEnded    -= OnBoostEnded;
                _flight.OnStallWarning  -= OnStallWarning;
            }
        }

        private void Update()
        {
            TickCooldowns();
            CheckAltitudeHaptics();
        }

        // ── Cooldown tick ────────────────────────────────────────────────────────
        private void TickCooldowns()
        {
            float dt = Time.deltaTime;
            if (_altWarningCooldownTimer > 0f) _altWarningCooldownTimer -= dt;
            if (_stallCooldownTimer      > 0f) _stallCooldownTimer      -= dt;
            if (_boostCooldownTimer      > 0f) _boostCooldownTimer      -= dt;
        }

        // ── Altitude haptics ─────────────────────────────────────────────────────
        private void CheckAltitudeHaptics()
        {
            if (_altitude == null || HapticManager.Instance == null) return;

            float alt = _altitude.CurrentAltitudeMeters;

            // Kármán warning (100 km)
            if (alt >= altitudeWarningThreshold && alt < spaceEntryThreshold
                && !_altitudeWarningTriggered && _altWarningCooldownTimer <= 0f)
            {
                _altitudeWarningTriggered = true;
                _altWarningCooldownTimer  = altitudeWarningCooldown;
                HapticManager.Instance.Trigger(HapticPattern.AltitudeWarning);
            }
            else if (alt < altitudeWarningThreshold)
            {
                _altitudeWarningTriggered = false;
            }

            // Space entry (120 km)
            if (alt >= spaceEntryThreshold && !_spaceEntryTriggered)
            {
                _spaceEntryTriggered = true;
                HapticManager.Instance.Trigger(HapticPattern.Heavy);
            }
            else if (alt < spaceEntryThreshold)
            {
                _spaceEntryTriggered = false;
            }
        }

        // ── Event handlers ───────────────────────────────────────────────────────
        private void OnTeleportCompleted()
        {
            HapticManager.Instance?.Trigger(HapticPattern.TeleportComplete);
        }

        private void OnScreenshotCaptured(string _)
        {
            HapticManager.Instance?.Trigger(HapticPattern.ScreenshotSnap);
        }

        private void OnAchievementUnlocked(AchievementDef _)
        {
            HapticManager.Instance?.Trigger(HapticPattern.AchievementUnlock);
        }

        private void OnBoostStarted()
        {
            if (_boostActive) return;
            _boostActive  = true;
            _boostRoutine = StartCoroutine(BoostHapticLoop());
        }

        private void OnBoostEnded()
        {
            _boostActive = false;
            if (_boostRoutine != null)
            {
                StopCoroutine(_boostRoutine);
                _boostRoutine = null;
            }
        }

        private void OnStallWarning()
        {
            if (_stallCooldownTimer > 0f) return;
            _stallCooldownTimer = stallCooldown;
            HapticManager.Instance?.Trigger(HapticPattern.Stall);
        }

        // ── Boost loop ───────────────────────────────────────────────────────────
        private IEnumerator BoostHapticLoop()
        {
            while (_boostActive)
            {
                if (_boostCooldownTimer <= 0f)
                {
                    HapticManager.Instance?.Trigger(HapticPattern.Boost);
                    _boostCooldownTimer = boostCooldown;
                }
                yield return null;
            }
        }
    }
}
