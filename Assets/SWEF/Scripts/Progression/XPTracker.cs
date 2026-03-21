using System;
using UnityEngine;

namespace SWEF.Progression
{
    /// <summary>
    /// Automatically tracks player activities and forwards XP awards to
    /// <see cref="ProgressionManager.AddXP"/>.
    /// Attach to a persistent GameObject in the scene.
    /// Subscribes (null-safely) to achievement, event, and tour completion events.
    /// </summary>
    public class XPTracker : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────────
        private const string FirstFlightDateKey = "SWEF_LastFlightDate";
        private const string XPConfigResourcePath = "XPSourceConfig";

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Optional Overrides")]
        [Tooltip("Leave empty to auto-load from Resources/XPSourceConfig.")]
        [SerializeField] private XPSourceConfig configOverride;

        // ── Internal state ────────────────────────────────────────────────────────
        private XPSourceConfig _config;
        private ProgressionManager _progression;

        private float _flightMinuteAccum;       // seconds accumulated toward next flight-minute XP
        private float _kmAccum;                 // km accumulated toward next km XP tick
        private float _formationMinuteAccum;    // seconds accumulated toward next formation-minute XP
        private bool  _isFlying;
        private bool  _isInFormation;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            _config = configOverride
                ? configOverride
                : Resources.Load<XPSourceConfig>(XPConfigResourcePath) ?? XPSourceConfig.GetDefault();
        }

        private void Start()
        {
            _progression = ProgressionManager.Instance;
            if (_progression == null)
                _progression = FindFirstObjectByType<ProgressionManager>();

            SubscribeToAchievements();
            SubscribeToEvents();
            SubscribeToTours();

            // Grant first-flight-of-day bonus once per calendar day
            GrantFirstFlightBonusIfEligible();
        }

        private void OnDestroy()
        {
            UnsubscribeFromAchievements();
            UnsubscribeFromEvents();
            UnsubscribeFromTours();
        }

        // ── Per-frame tracking ────────────────────────────────────────────────────

        /// <summary>
        /// Call each frame while the player is actively flying.
        /// </summary>
        /// <param name="deltaTime">Frame delta time in seconds.</param>
        /// <param name="distanceDeltaKm">Distance flown this frame in kilometres.</param>
        /// <param name="inFormation">Whether the player is currently in formation flight.</param>
        public void TrackFlightFrame(float deltaTime, float distanceDeltaKm, bool inFormation = false)
        {
            if (_progression == null) return;

            float multiplier = GetCurrentMultiplier();

            // Per-minute XP
            _flightMinuteAccum += deltaTime;
            while (_flightMinuteAccum >= 60f)
            {
                _flightMinuteAccum -= 60f;
                long xp = (long)Mathf.Round(_config.xpPerFlightMinute * multiplier);
                _progression.AddXP(xp, "flight_time");
            }

            // Per-km XP
            _kmAccum += distanceDeltaKm;
            while (_kmAccum >= 1f)
            {
                _kmAccum -= 1f;
                long xp = (long)Mathf.Round(_config.xpPerKmFlown * multiplier);
                _progression.AddXP(xp, "distance");
            }

            // Formation XP
            if (inFormation)
            {
                _formationMinuteAccum += deltaTime;
                while (_formationMinuteAccum >= 60f)
                {
                    _formationMinuteAccum -= 60f;
                    long xp = (long)Mathf.Round(_config.xpPerFormationMinute * multiplier);
                    _progression.AddXP(xp, "formation");
                }
            }
            else
            {
                _formationMinuteAccum = 0f;
            }
        }

        /// <summary>
        /// Records XP when a photo (screenshot) is taken.
        /// </summary>
        public void TrackPhotoTaken()
        {
            if (_progression == null) return;
            long xp = (long)Mathf.Round(_config.xpPerPhotoTaken * GetCurrentMultiplier());
            _progression.AddXP(xp, "photo");
        }

        /// <summary>
        /// Records XP when a replay is shared.
        /// </summary>
        public void TrackReplayShared()
        {
            if (_progression == null) return;
            long xp = (long)Mathf.Round(_config.xpPerReplayShared * GetCurrentMultiplier());
            _progression.AddXP(xp, "replay_shared");
        }

        /// <summary>
        /// Records XP for completing a multiplayer session.
        /// </summary>
        public void TrackMultiplayerSessionEnded()
        {
            if (_progression == null) return;
            long xp = (long)Mathf.Round(_config.xpPerMultiplayerSession * GetCurrentMultiplier());
            _progression.AddXP(xp, "multiplayer_session");
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private float GetCurrentMultiplier()
        {
            float m = 1f;
            var dow = DateTime.Now.DayOfWeek;
            if (dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday)
                m *= _config.xpMultiplierWeekend;
            return m;
        }

        private void GrantFirstFlightBonusIfEligible()
        {
            if (_progression == null) return;
            string today  = DateTime.Now.ToString("yyyy-MM-dd");
            string stored = PlayerPrefs.GetString(FirstFlightDateKey, string.Empty);
            if (stored == today) return;

            PlayerPrefs.SetString(FirstFlightDateKey, today);
            PlayerPrefs.Save();
            _progression.AddXP(_config.xpBonusFirstFlightOfDay, "first_flight_of_day");
            Debug.Log("[SWEF] XPTracker: First flight of day bonus granted.");
        }

        // ── Achievement subscription ──────────────────────────────────────────────

        private Achievement.AchievementManager _achievementManager;

        private void SubscribeToAchievements()
        {
            _achievementManager = FindFirstObjectByType<Achievement.AchievementManager>();
            if (_achievementManager != null)
                _achievementManager.OnAchievementUnlocked += HandleAchievementUnlocked;
        }

        private void UnsubscribeFromAchievements()
        {
            if (_achievementManager != null)
                _achievementManager.OnAchievementUnlocked -= HandleAchievementUnlocked;
        }

        private void HandleAchievementUnlocked(Achievement.AchievementDefinition def)
        {
            if (_progression == null) return;
            long xp = (long)Mathf.Round(_config.xpPerAchievementUnlock * GetCurrentMultiplier());
            _progression.AddXP(xp, $"achievement:{def?.id ?? "unknown"}");
        }

        // ── Event subscription ────────────────────────────────────────────────────
        // Note: Event-completion XP is already granted by EventParticipationTracker.GrantRewardsForEvent.
        // XPTracker intentionally does not subscribe to EventScheduler.OnEventExpired to avoid double-granting.
        private void SubscribeToEvents() { }
        private void UnsubscribeFromEvents() { }

        // ── Tour subscription ─────────────────────────────────────────────────────

        private GuidedTour.TourManager _tourManager;

        private void SubscribeToTours()
        {
            _tourManager = FindFirstObjectByType<GuidedTour.TourManager>();
            if (_tourManager != null)
                _tourManager.OnTourCompleted += HandleTourCompleted;
        }

        private void UnsubscribeFromTours()
        {
            if (_tourManager != null)
                _tourManager.OnTourCompleted -= HandleTourCompleted;
        }

        private void HandleTourCompleted(GuidedTour.TourData tourData)
        {
            if (_progression == null) return;
            long xp = (long)Mathf.Round(_config.xpPerTourCompleted * GetCurrentMultiplier());
            _progression.AddXP(xp, $"tour:{tourData?.tourId ?? "unknown"}");
        }
    }
}
