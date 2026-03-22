using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using SWEF.Flight;
using SWEF.Localization;
using SWEF.Analytics;

namespace SWEF.Narration
{
    /// <summary>
    /// Central singleton manager for the Environmental Storytelling &amp; Landmark Narration system.
    /// <para>
    /// Responsibilities:
    /// <list type="bullet">
    /// <item>Frame-rate proximity detection using the player's GPS-equivalent world position.</item>
    /// <item>Priority queue management — only one narration plays at a time by default.</item>
    /// <item>Delegates audio playback to <see cref="NarrationAudioController"/>.</item>
    /// <item>Enforces cooldown between auto-triggered narrations.</item>
    /// <item>Persists <see cref="NarrationConfig"/> to JSON.</item>
    /// </list>
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-15)]
    public class NarrationManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static NarrationManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Database")]
        [Tooltip("Assign the LandmarkDatabase ScriptableObject asset here.")]
        [SerializeField] private LandmarkDatabase database;

        [Header("Proximity")]
        [Tooltip("How often (seconds) the proximity scan runs. Lower = more responsive but more CPU.")]
        [SerializeField] private float proximityCheckInterval = 1f;

        [Tooltip("Radius (km) fed to spatial grid search each tick.")]
        [SerializeField] private float searchRadiusKm = 20f;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when a landmark enters the proximity trigger range.</summary>
        public event Action<LandmarkData> OnLandmarkEnterRange;

        /// <summary>Fired when a landmark exits the proximity trigger range.</summary>
        public event Action<LandmarkData> OnLandmarkExitRange;

        /// <summary>Fired when a narration starts playing.</summary>
        public event Action<NarrationQueueEntry> OnNarrationStarted;

        /// <summary>Fired when a narration completes or is skipped.</summary>
        public event Action<NarrationQueueEntry, NarrationState> OnNarrationFinished;

        /// <summary>Fired when a new fun fact is ready to show.</summary>
        public event Action<string> OnFunFactReady;

        /// <summary>Fired when a narration segment changes.</summary>
        public event Action<NarrationSegment> OnSegmentChanged;

        /// <summary>Fired when a new nearby landmark is detected for the first time this session.</summary>
        public event Action<LandmarkData> OnNearbyLandmarkDetected;

        // ── Config ────────────────────────────────────────────────────────────────
        /// <summary>Current player-adjustable configuration.</summary>
        public NarrationConfig Config { get; private set; } = new NarrationConfig();

        // ── State ─────────────────────────────────────────────────────────────────
        private readonly Queue<NarrationQueueEntry> _queue = new Queue<NarrationQueueEntry>();

        /// <summary>Currently active narration, or null if idle.</summary>
        public NarrationQueueEntry ActiveEntry { get; private set; }

        private Coroutine  _playbackCoroutine;
        private float      _lastNarrationFinishedTime = -999f;
        private float      _nextProximityCheck;

        private readonly HashSet<string>       _inRangeLandmarks   = new HashSet<string>();
        private readonly HashSet<string>       _narrationCooldowns = new HashSet<string>();

        // ── References ────────────────────────────────────────────────────────────
        private FlightController        _flight;
        private NarrationAudioController _audio;
        private LandmarkDiscoveryTracker _discovery;
        private UserBehaviorTracker      _analytics;

        // ── Persistence ───────────────────────────────────────────────────────────
        private static readonly string ConfigFile = "narration_config.json";
        private string ConfigPath => Path.Combine(Application.persistentDataPath, ConfigFile);

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

            LoadConfig();

            // Load built-in database if none assigned.
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<LandmarkDatabase>();
                Debug.LogWarning("[SWEF] NarrationManager: no LandmarkDatabase assigned — using runtime default.");
            }

            Debug.Log($"[SWEF] NarrationManager: ready ({database.TotalLandmarks} landmarks).");
        }

        private void Start()
        {
            _flight    = FindFirstObjectByType<FlightController>();
            _audio     = GetComponent<NarrationAudioController>() ?? gameObject.AddComponent<NarrationAudioController>();
            _discovery = FindFirstObjectByType<LandmarkDiscoveryTracker>();
            _analytics = UserBehaviorTracker.Instance;
        }

        private void Update()
        {
            if (!Config.enabled) return;
            if (Time.time < _nextProximityCheck) return;

            _nextProximityCheck = Time.time + proximityCheckInterval;
            RunProximityCheck();
        }

        private void OnApplicationPause(bool paused) { if (paused) SaveConfig(); }
        private void OnApplicationQuit()             { SaveConfig(); }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Manually trigger narration for a specific landmark, ignoring cooldown.
        /// </summary>
        public void TriggerNarration(string landmarkId)
        {
            var lm = database?.GetLandmarkById(landmarkId);
            if (lm == null)
            {
                Debug.LogWarning($"[SWEF] NarrationManager.TriggerNarration: landmark '{landmarkId}' not found.");
                return;
            }
            EnqueueNarration(lm, forced: true);
        }

        /// <summary>Skip the currently playing narration.</summary>
        public void SkipCurrent()
        {
            if (ActiveEntry == null) return;
            ActiveEntry.state = NarrationState.Skipped;
            _audio?.Stop();
            if (_playbackCoroutine != null) StopCoroutine(_playbackCoroutine);
            OnNarrationFinished?.Invoke(ActiveEntry, NarrationState.Skipped);
            ActiveEntry = null;
            ProcessQueue();
        }

        /// <summary>Pause or resume the current narration.</summary>
        public void SetPaused(bool paused)
        {
            if (ActiveEntry == null) return;
            if (paused && ActiveEntry.state == NarrationState.Playing)
            {
                _audio?.SetPaused(true);
                ActiveEntry.state = NarrationState.Paused;
            }
            else if (!paused && ActiveEntry.state == NarrationState.Paused)
            {
                _audio?.SetPaused(false);
                ActiveEntry.state = NarrationState.Playing;
            }
        }

        /// <summary>Update the narration configuration and persist it.</summary>
        public void ApplyConfig(NarrationConfig cfg)
        {
            Config = cfg ?? new NarrationConfig();
            SaveConfig();
        }

        /// <summary>Returns the landmark database.</summary>
        public LandmarkDatabase Database => database;

        // ── Proximity detection ───────────────────────────────────────────────────

        private void RunProximityCheck()
        {
            if (_flight == null) return;

            // Simulate GPS from Unity world position (1 unit ≈ 1 metre).
            Vector3 pos = _flight.transform.position;
            // Convert world position to mock lat/lon centred at 0,0.
            double lat = pos.z / 111320.0;
            double lon = pos.x / (111320.0 * Math.Cos(lat * Math.PI / 180.0));

            var nearby = database.GetLandmarksNear(lat, lon, searchRadiusKm);

            // Detect new entries.
            foreach (var lm in nearby)
            {
                if (!_inRangeLandmarks.Contains(lm.landmarkId))
                {
                    _inRangeLandmarks.Add(lm.landmarkId);
                    OnLandmarkEnterRange?.Invoke(lm);
                    OnNearbyLandmarkDetected?.Invoke(lm);

                    // Auto-trigger if conditions met.
                    if (Config.autoPlayOnProximity && ShouldAutoPlay(lm))
                        EnqueueNarration(lm, forced: false);
                }
            }

            // Detect exits.
            var nowInRange = new HashSet<string>(nearby.Select(l => l.landmarkId));
            var exited = _inRangeLandmarks.Where(id => !nowInRange.Contains(id)).ToList();
            foreach (var id in exited)
            {
                _inRangeLandmarks.Remove(id);
                var lm = database.GetLandmarkById(id);
                if (lm != null) OnLandmarkExitRange?.Invoke(lm);
            }
        }

        private bool ShouldAutoPlay(LandmarkData lm)
        {
            if (!Config.enabled) return false;
            // Category filter.
            if (Config.preferredCategories.Count > 0 && !Config.preferredCategories.Contains(lm.category))
                return false;
            // Cooldown between any narrations.
            if (Time.time - _lastNarrationFinishedTime < Config.cooldownBetweenNarrations)
                return false;
            // Per-landmark cooldown (already narrated this session).
            if (_narrationCooldowns.Contains(lm.landmarkId))
                return false;
            return true;
        }

        // ── Queue management ──────────────────────────────────────────────────────

        private void EnqueueNarration(LandmarkData lm, bool forced)
        {
            string langCode = GetLanguageCode();
            var script = database.GetBestScript(lm.landmarkId, langCode);
            if (script == null)
            {
                Debug.Log($"[SWEF] NarrationManager: no script found for '{lm.landmarkId}' ({langCode}).");
                return;
            }

            // Don't double-queue the same landmark.
            if (_queue.Any(e => e.landmark.landmarkId == lm.landmarkId)) return;
            if (ActiveEntry?.landmark.landmarkId == lm.landmarkId) return;

            var entry = new NarrationQueueEntry
            {
                landmark  = lm,
                script    = script,
                state     = NarrationState.Queued,
                queuedAt  = Time.time
            };

            _queue.Enqueue(entry);
            Debug.Log($"[SWEF] NarrationManager: queued '{lm.name}'.");

            if (ActiveEntry == null)
                ProcessQueue();
        }

        private void ProcessQueue()
        {
            if (_queue.Count == 0) return;
            if (ActiveEntry != null) return;

            ActiveEntry = _queue.Dequeue();
            ActiveEntry.state = NarrationState.Playing;
            _playbackCoroutine = StartCoroutine(PlayNarration(ActiveEntry));
        }

        // ── Playback coroutine ────────────────────────────────────────────────────

        private IEnumerator PlayNarration(NarrationQueueEntry entry)
        {
            Debug.Log($"[SWEF] NarrationManager: playing '{entry.landmark.name}'.");
            OnNarrationStarted?.Invoke(entry);
            _discovery?.RecordVisit(entry.landmark.landmarkId);
            _narrationCooldowns.Add(entry.landmark.landmarkId);
            _analytics?.TrackFeatureDiscovery("narration_" + entry.landmark.landmarkId);

            var script = entry.script;

            // Show fun facts.
            if (Config.enableFunFacts && script.funFacts.Count > 0)
            {
                int idx = UnityEngine.Random.Range(0, script.funFacts.Count);
                OnFunFactReady?.Invoke(script.funFacts[idx]);
            }

            // Play audio (if available and preferred).
            // Speed is applied as AudioSource.pitch inside NarrationAudioController.
            bool usingAudio = false;
            if (Config.preferAudioNarration && script.hasAudio && !string.IsNullOrEmpty(script.audioClipPath))
            {
                usingAudio = true;
                _audio?.PlayNarration(script.audioClipPath, Config.narrationVolume);
            }

            // Iterate over segments.
            // Speed controls how fast the text advances; audio pitch is handled in NarrationAudioController.
            float speed = Mathf.Max(0.1f, Config.narrationSpeed);
            for (int i = 0; i < script.segments.Count; i++)
            {
                if (entry.state == NarrationState.Skipped) yield break;
                while (entry.state == NarrationState.Paused)
                    yield return null;

                var seg = script.segments[i];
                OnSegmentChanged?.Invoke(seg);

                float segDuration = (seg.endTime - seg.startTime) / speed;
                yield return new WaitForSeconds(segDuration);

                if (i < script.segments.Count - 1 && Config.autoAdvanceSegments)
                    yield return new WaitForSeconds(Config.segmentPauseDuration);
            }

            // Clean up.
            if (!usingAudio) FinishPlayback(entry);
            else
            {
                // Wait for audio to finish. Audio pitch = speed, so actual wall-clock duration = totalDuration / speed.
                float remaining = script.totalDuration / speed;
                yield return new WaitForSeconds(remaining);
                FinishPlayback(entry);
            }
        }

        private void FinishPlayback(NarrationQueueEntry entry)
        {
            if (entry.state != NarrationState.Skipped)
                entry.state = NarrationState.Completed;

            _lastNarrationFinishedTime = Time.time;
            _audio?.Stop();
            OnNarrationFinished?.Invoke(entry, entry.state);
            ActiveEntry = null;

            ProcessQueue();
        }

        // ── Language ──────────────────────────────────────────────────────────────

        private static string GetLanguageCode()
        {
            if (LocalizationManager.Instance == null) return "en";
            return LocalizationManager.Instance.CurrentLanguage switch
            {
                UnityEngine.SystemLanguage.Korean          => "ko",
                UnityEngine.SystemLanguage.Japanese        => "ja",
                UnityEngine.SystemLanguage.ChineseSimplified => "zh",
                UnityEngine.SystemLanguage.Spanish         => "es",
                UnityEngine.SystemLanguage.French          => "fr",
                UnityEngine.SystemLanguage.German          => "de",
                UnityEngine.SystemLanguage.Portuguese      => "pt",
                _                                          => "en"
            };
        }

        // ── Config persistence ────────────────────────────────────────────────────

        private void LoadConfig()
        {
            if (!File.Exists(ConfigPath)) return;
            try
            {
                Config = JsonUtility.FromJson<NarrationConfig>(File.ReadAllText(ConfigPath))
                         ?? new NarrationConfig();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SWEF] NarrationManager: config load failed — {e.Message}");
                Config = new NarrationConfig();
            }
        }

        private void SaveConfig()
        {
            try { File.WriteAllText(ConfigPath, JsonUtility.ToJson(Config, true)); }
            catch (Exception e)
            { Debug.LogWarning($"[SWEF] NarrationManager: config save failed — {e.Message}"); }
        }
    }
}
