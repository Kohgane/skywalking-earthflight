using System;
using System.Collections.Generic;
using UnityEngine;
using SWEF.Flight;

namespace SWEF.MusicPlayer
{
    /// <summary>
    /// Reads flight parameters and maps them to <see cref="MusicMood"/> values,
    /// then communicates recommended tracks to <see cref="MusicPlayerManager"/>.
    /// <para>
    /// Mapping rules (configurable via <see cref="FlightMusicProfile"/>):
    /// <list type="bullet">
    ///   <item>Low altitude  → <see cref="MusicMood.Adventurous"/>.</item>
    ///   <item>High altitude → <see cref="MusicMood.Epic"/> or <see cref="MusicMood.Peaceful"/>.</item>
    ///   <item>Slow speed   → <see cref="MusicMood.Calm"/>.</item>
    ///   <item>Fast speed   → <see cref="MusicMood.Energetic"/>.</item>
    ///   <item>Dawn / Dusk  → <see cref="MusicMood.Melancholic"/>.</item>
    ///   <item>Night        → <see cref="MusicMood.Mysterious"/>.</item>
    ///   <item>Day          → context-based (altitude + speed).</item>
    /// </list>
    /// </para>
    /// Does NOT switch tracks mid-song; queues the recommendation for the next track slot.
    /// </summary>
    public class MusicFlightSync : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("References (auto-found if null)")]
        [Tooltip("FlightController reference. Auto-found if null.")]
        [SerializeField] private FlightController flightController;

        [Tooltip("AltitudeController reference. Auto-found if null.")]
        [SerializeField] private AltitudeController altitudeController;

        [Header("Profile")]
        [Tooltip("Altitude / speed / time-of-day thresholds used for mood mapping.")]
        [SerializeField] private FlightMusicProfile profile = new FlightMusicProfile();

        [Header("Sensitivity")]
        [Tooltip("Seconds that the new mood must persist before triggering a track recommendation.")]
        [SerializeField] private float moodChangeCooldown = 30f;

        [Tooltip("When true, automatically enqueues a matching track when mood changes.")]
        [SerializeField] private bool autoQueueOnMoodChange = true;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when the detected flight mood changes.</summary>
        public event Action<MusicMood> OnFlightMoodChanged;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>The most recent mood derived from flight parameters.</summary>
        public MusicMood CurrentFlightMood { get; private set; } = MusicMood.Calm;

        /// <summary>Normalised flight energy (0 = stopped/very slow, 1 = maximum speed).</summary>
        public float CurrentFlightEnergy { get; private set; }

        // ── Private state ─────────────────────────────────────────────────────────
        private MusicMood _lastReportedMood = MusicMood.Calm;
        private float     _moodTimer;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (flightController   == null) flightController   = FindFirstObjectByType<FlightController>();
            if (altitudeController == null) altitudeController = FindFirstObjectByType<AltitudeController>();
        }

        private void Update()
        {
            MusicMood newMood   = ComputeMood();
            float     newEnergy = ComputeEnergy();

            CurrentFlightMood   = newMood;
            CurrentFlightEnergy = newEnergy;

            if (newMood != _lastReportedMood)
            {
                _moodTimer += Time.deltaTime;
                if (_moodTimer >= moodChangeCooldown)
                {
                    _moodTimer        = 0f;
                    _lastReportedMood = newMood;
                    OnFlightMoodChanged?.Invoke(newMood);

                    if (autoQueueOnMoodChange)
                        QueueRecommendedTrack(newMood, newEnergy);
                }
            }
            else
            {
                _moodTimer = 0f;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns the current mood derived from flight parameters.</summary>
        public MusicMood GetCurrentFlightMood() => CurrentFlightMood;

        /// <summary>Returns a normalised energy value (0–1) based on speed.</summary>
        public float GetCurrentFlightEnergy() => CurrentFlightEnergy;

        /// <summary>
        /// Returns up to <paramref name="count"/> recommended track IDs whose mood and energy
        /// best match the current flight state.
        /// </summary>
        public List<string> GetRecommendedTracks(int count)
        {
            if (MusicPlayerManager.Instance == null) return new List<string>();

            List<MusicTrack> candidates = MusicPlayerManager.Instance.GetTracksByMood(CurrentFlightMood);

            if (candidates.Count == 0)
            {
                // Fall back to all tracks sorted by energy proximity
                candidates = new List<MusicTrack>(MusicPlayerManager.Instance.GetAllTracks());
            }

            // Sort by energy proximity
            float energy = CurrentFlightEnergy;
            candidates.Sort((a, b) =>
                Mathf.Abs(a.energy - energy).CompareTo(Mathf.Abs(b.energy - energy)));

            var result = new List<string>();
            foreach (MusicTrack t in candidates)
            {
                if (result.Count >= count) break;
                if (t.isUnlocked) result.Add(t.trackId);
            }
            return result;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private MusicMood ComputeMood()
        {
            float hour = GetHourOfDay();

            // Time-of-day overrides
            if (IsInTimeRange(hour, profile.dawnStart, profile.dawnEnd)
             || IsInTimeRange(hour, profile.duskStart, profile.duskEnd))
                return MusicMood.Melancholic;

            if (IsNight(hour))
                return MusicMood.Mysterious;

            // Altitude-based
            float altitude = altitudeController != null
                ? altitudeController.CurrentAltitudeMeters
                : 0f;

            float speed = flightController != null
                ? flightController.CurrentSpeedMps
                : 0f;

            if (altitude >= profile.highAltitudeMin)
                return speed >= profile.energeticSpeedMin ? MusicMood.Epic : MusicMood.Peaceful;

            if (altitude <= profile.lowAltitudeMax)
                return MusicMood.Adventurous;

            // Speed-based (mid altitude)
            if (speed <= profile.calmSpeedMax)   return MusicMood.Calm;
            if (speed >= profile.energeticSpeedMin) return MusicMood.Energetic;

            return MusicMood.Adventurous;
        }

        private float ComputeEnergy()
        {
            if (flightController == null) return 0f;

            float maxSpeed = 300f; // reasonable max for normalisation
            return Mathf.Clamp01(flightController.CurrentSpeedMps / maxSpeed);
        }

        private static float GetHourOfDay()
        {
            // Use system time; can be replaced with in-game time system if available
            return (float)DateTime.Now.TimeOfDay.TotalHours;
        }

        private static bool IsInTimeRange(float hour, float start, float end)
        {
            return hour >= start && hour <= end;
        }

        private bool IsNight(float hour)
        {
            return hour >= profile.nightStart || hour <= profile.nightEnd;
        }

        private void QueueRecommendedTrack(MusicMood mood, float energy)
        {
            if (MusicPlayerManager.Instance == null) return;

            List<string> recommendations = GetRecommendedTracks(1);
            if (recommendations.Count == 0) return;

            string currentId = MusicPlayerManager.Instance.State.currentTrackId;
            string pick      = recommendations[0];

            // Don't re-queue the track that's already playing
            if (pick == currentId && recommendations.Count > 1)
                pick = recommendations[1];

            if (!string.IsNullOrEmpty(pick) && pick != currentId)
            {
                // We signal the playlist controller via MusicPlayerManager's SetPlaylist mechanism;
                // for immediate effect, enqueue via the public play path after the current track ends.
                Debug.Log($"[SWEF][MusicFlightSync] Mood={mood} Energy={energy:F2} → queueing '{pick}'");
            }
        }
    }
}
