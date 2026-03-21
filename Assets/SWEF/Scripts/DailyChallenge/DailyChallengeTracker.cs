using UnityEngine;

namespace SWEF.DailyChallenge
{
    /// <summary>
    /// MonoBehaviour that automatically tracks player activities and forwards progress
    /// deltas to <see cref="DailyChallengeManager"/> and <see cref="WeeklyChallengeManager"/>.
    /// Per-frame tracking covers distance, altitude, duration, and speed.
    /// Event-based tracking covers photos, tours, formations, and multiplayer sessions.
    /// All integrations are null-safe — missing managers are silently skipped.
    /// </summary>
    public class DailyChallengeTracker : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Flight References")]
        [Tooltip("FlightController used for position-delta distance and speed.")]
        [SerializeField] private SWEF.Flight.FlightController _flightController;

        [Tooltip("AltitudeController used for altitude tracking.")]
        [SerializeField] private SWEF.Flight.AltitudeController _altitudeController;

        // ── State ─────────────────────────────────────────────────────────────────
        private Vector3 _lastPosition;
        private bool _positionInitialised;
        private float _maxAltitude;
        private float _maxSpeed;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Start()
        {
            TryAutoFindReferences();
            SubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void Update()
        {
            TrackFlightFrame(Time.deltaTime);
        }

        // ── Per-frame tracking ────────────────────────────────────────────────────

        private void TrackFlightFrame(float dt)
        {
            if (_flightController == null) return;

            var pos = _flightController.transform.position;

            // Distance flown.
            if (_positionInitialised)
            {
                float deltaMetres = Vector3.Distance(pos, _lastPosition);
                float deltaKm = deltaMetres / 1000f;
                if (deltaKm > 0f)
                    Report(ChallengeType.FlyDistance, deltaKm);
            }
            _lastPosition = pos;
            _positionInitialised = true;

            // Speed tracking (m/s → km/h).
            float speedKmh = _flightController.CurrentSpeedMps * 3.6f;
            if (speedKmh > _maxSpeed)
            {
                _maxSpeed = speedKmh;
                Report(ChallengeType.AchieveSpeed, speedKmh);
            }

            // Flight duration.
            Report(ChallengeType.FlyDuration, dt);

            // Altitude.
            if (_altitudeController != null)
            {
                float alt = _altitudeController.CurrentAltitudeMeters;
                if (alt > _maxAltitude)
                {
                    _maxAltitude = alt;
                    Report(ChallengeType.ReachAltitude, alt);
                }
            }
        }

        // ── Event subscriptions ───────────────────────────────────────────────────

        private void SubscribeEvents()
        {
            // Screenshots / photo mode — null-safe.
            var photoCtrl = FindFirstObjectByType<SWEF.Screenshot.ScreenshotController>();
            if (photoCtrl != null)
                photoCtrl.OnScreenshotCaptured += OnPhotoTaken;

            // Guided tours — null-safe.
            var tourMgr = SWEF.GuidedTour.TourManager.Instance;
            if (tourMgr != null)
                tourMgr.OnTourCompleted += OnTourCompleted;

            // Formation flying — null-safe.
            var formMgr = SWEF.Multiplayer.FormationFlyingManager.Instance;
            if (formMgr != null)
                formMgr.OnFormationBroken += OnFormationBroken;

            // Multiplayer sessions — null-safe.
            var netMgr = SWEF.Multiplayer.NetworkManager2.Instance;
            if (netMgr != null)
                netMgr.OnLobbyJoined += OnLobbyJoined;
        }

        private void UnsubscribeEvents()
        {
            var photoCtrl = FindFirstObjectByType<SWEF.Screenshot.ScreenshotController>();
            if (photoCtrl != null)
                photoCtrl.OnScreenshotCaptured -= OnPhotoTaken;

            var tourMgr = SWEF.GuidedTour.TourManager.Instance;
            if (tourMgr != null)
                tourMgr.OnTourCompleted -= OnTourCompleted;

            var formMgr = SWEF.Multiplayer.FormationFlyingManager.Instance;
            if (formMgr != null)
                formMgr.OnFormationBroken -= OnFormationBroken;

            var netMgr = SWEF.Multiplayer.NetworkManager2.Instance;
            if (netMgr != null)
                netMgr.OnLobbyJoined -= OnLobbyJoined;
        }

        // ── Handlers ──────────────────────────────────────────────────────────────

        private void OnPhotoTaken(string _)    => Report(ChallengeType.TakePhotos, 1f);
        private void OnTourCompleted(SWEF.GuidedTour.TourData _) => Report(ChallengeType.CompleteTour, 1f);
        private void OnFormationBroken(SWEF.Multiplayer.Formation _) => Report(ChallengeType.CompleteFormation, 1f);
        private void OnLobbyJoined(SWEF.Multiplayer.LobbyInfo _) => Report(ChallengeType.PlayMultiplayer, 1f);

        // ── Helper ────────────────────────────────────────────────────────────────

        private static void Report(ChallengeType type, float amount)
        {
            DailyChallengeManager.Instance?.ReportProgress(type, amount);
            WeeklyChallengeManager.Instance?.ReportProgress(type, amount);
        }

        private void TryAutoFindReferences()
        {
            if (_flightController == null)
                _flightController = FindFirstObjectByType<SWEF.Flight.FlightController>();
            if (_altitudeController == null)
                _altitudeController = FindFirstObjectByType<SWEF.Flight.AltitudeController>();
        }
    }
}
