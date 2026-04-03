// PhotoSpotDiscovery.cs — SWEF Advanced Photography & Drone Camera System (Phase 89)
using System;
using System.Collections.Generic;
using UnityEngine;

#if SWEF_BIOME_AVAILABLE
using SWEF.Biome;
#endif

#if SWEF_WEATHER_AVAILABLE
using SWEF.Weather;
#endif

#if SWEF_NARRATION_AVAILABLE
using SWEF.Narration;
#endif

#if SWEF_MINIMAP_AVAILABLE
using SWEF.Minimap;
#endif

namespace SWEF.AdvancedPhotography
{
    /// <summary>
    /// Phase 89 — Singleton MonoBehaviour that maintains a registry of scenic
    /// <see cref="PhotoSpot"/> locations, handles proximity-based discovery, and
    /// provides a scored recommendation engine.
    ///
    /// <para>Integrates with Biome, Weather, Narration (LandmarkDatabase), and
    /// Minimap systems when the corresponding compile guards are defined.</para>
    /// </summary>
    public sealed class PhotoSpotDiscovery : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static PhotoSpotDiscovery Instance { get; private set; }

        #endregion

        #region Events

        /// <summary>Fired when the player discovers a new photo spot.</summary>
        public event Action<PhotoSpot> OnPhotoSpotDiscovered;

        /// <summary>Fired when the recommendation engine produces a new ranked list.</summary>
        public event Action<PhotoSpot[]> OnPhotoSpotRecommended;

        #endregion

        #region Inspector

        [Header("References")]
        [Tooltip("Player transform used for proximity and distance calculations.")]
        [SerializeField] private Transform _playerTransform;

        [Header("Spots")]
        [Tooltip("Pre-authored spots. Additional spots can be registered at runtime.")]
        [SerializeField] private List<PhotoSpot> _spots = new List<PhotoSpot>();

        [Header("Discovery")]
        [Tooltip("How often (seconds) the discovery check runs.")]
        [SerializeField] [Min(0.1f)] private float _discoveryCheckInterval = 2f;

        [Header("Recommendations")]
        [Tooltip("Number of spots returned by GetRecommendedSpots when count is not specified.")]
        [SerializeField] [Min(1)] private int _defaultRecommendationCount = 5;

        #endregion

        #region Private State

        private float _lastCheckTime = 0f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            RegisterMinimapMarkers();
        }

        private void Update()
        {
            if (Time.time - _lastCheckTime >= _discoveryCheckInterval)
            {
                _lastCheckTime = Time.time;
                CheckDiscovery();
            }
        }

        #endregion

        #region Public API

        /// <summary>Returns all spots within <paramref name="radius"/> metres of <paramref name="position"/>.</summary>
        public List<PhotoSpot> GetNearbySpots(Vector3 position, float radius)
        {
            var result = new List<PhotoSpot>();
            float r2 = radius * radius;

            foreach (var spot in _spots)
                if ((spot.position - position).sqrMagnitude <= r2)
                    result.Add(spot);

            return result;
        }

        /// <summary>Returns up to <paramref name="count"/> recommended spots for the current context.</summary>
        public PhotoSpot[] GetRecommendedSpots(int count = 0)
        {
            if (count <= 0) count = _defaultRecommendationCount;

            Vector3 playerPos = _playerTransform != null ? _playerTransform.position : Vector3.zero;

            var scored = new List<(PhotoSpot spot, float score)>();

            foreach (var spot in _spots)
            {
                float score = ScoreSpot(spot, playerPos);
                scored.Add((spot, score));
            }

            scored.Sort((a, b) => b.score.CompareTo(a.score));

            var result = new PhotoSpot[Mathf.Min(count, scored.Count)];
            for (int i = 0; i < result.Length; i++)
                result[i] = scored[i].spot;

            OnPhotoSpotRecommended?.Invoke(result);
            return result;
        }

        /// <summary>Manually marks a spot as discovered by its <paramref name="spotId"/>.</summary>
        public void DiscoverSpot(string spotId)
        {
            PhotoSpot spot = _spots.Find(s => s.spotId == spotId);
            if (spot == null || spot.discovered) return;

            spot.discovered = true;
            OnPhotoSpotDiscovered?.Invoke(spot);
            AdvancedPhotographyAnalytics.RecordPhotoSpotDiscovered(spotId);
            Debug.Log($"[SWEF] PhotoSpotDiscovery: discovered spot '{spotId}'");
        }

        /// <summary>Returns all spots marked as discovered.</summary>
        public List<PhotoSpot> GetDiscoveredSpots()
        {
            return _spots.FindAll(s => s.discovered);
        }

        /// <summary>Returns the number of undiscovered spots in the registry.</summary>
        public int GetUndiscoveredCount()
        {
            int count = 0;
            foreach (var s in _spots)
                if (!s.discovered) count++;
            return count;
        }

        /// <summary>Registers a new <see cref="PhotoSpot"/> into the discovery registry at runtime.</summary>
        public void RegisterSpot(PhotoSpot spot)
        {
            if (spot == null) return;
            if (_spots.Exists(s => s.spotId == spot.spotId)) return;

            _spots.Add(spot);

#if SWEF_MINIMAP_AVAILABLE
            MinimapManager.Instance?.RegisterMarker(spot.spotId, spot.position, "photo_spot");
#endif
        }

        #endregion

        #region Private — Discovery

        private void CheckDiscovery()
        {
            if (_playerTransform == null) return;

            Vector3 pos = _playerTransform.position;
            float r2 = AdvancedPhotographyConfig.PhotoSpotTriggerRadius *
                       AdvancedPhotographyConfig.PhotoSpotTriggerRadius;

            foreach (var spot in _spots)
            {
                if (spot.discovered) continue;
                if ((spot.position - pos).sqrMagnitude <= r2)
                    DiscoverSpot(spot.spotId);
            }
        }

        #endregion

        #region Private — Scoring

        private float ScoreSpot(PhotoSpot spot, Vector3 playerPos)
        {
            float score = 0f;

            // Proximity bonus (closer = higher, up to half the discovery radius)
            float dist = Vector3.Distance(spot.position, playerPos);
            score += Mathf.Clamp01(1f - dist / AdvancedPhotographyConfig.PhotoSpotDiscoveryRadius) * 0.25f;

            // Undiscovered bonus
            if (!spot.discovered) score += 0.20f;

            // Time-of-day match
            float hour = System.DateTime.UtcNow.Hour + System.DateTime.UtcNow.Minute / 60f;
            if (hour >= spot.bestTimeOfDayRange.x && hour <= spot.bestTimeOfDayRange.y)
                score += 0.20f;

            // Weather match
#if SWEF_WEATHER_AVAILABLE
            string currentWeather = WeatherManager.Instance?.CurrentWeather ?? "";
            if (!string.IsNullOrEmpty(spot.bestWeather) && spot.bestWeather == currentWeather)
                score += 0.20f;
#endif

            // Biome variety bonus (avoid recommending the same biome repeatedly)
#if SWEF_BIOME_AVAILABLE
            // Each unique spot biome gets a small bonus; simplified here.
            score += 0.05f;
#endif

            // Difficulty inverse bonus (easier spots score slightly higher)
            score += (1f - (spot.difficulty - 1) / 4f) * 0.10f;

            return Mathf.Clamp01(score);
        }

        #endregion

        #region Private — Minimap

        private void RegisterMinimapMarkers()
        {
#if SWEF_MINIMAP_AVAILABLE
            foreach (var spot in _spots)
                MinimapManager.Instance?.RegisterMarker(spot.spotId, spot.position, "photo_spot");
#endif
        }

        #endregion
    }
}
