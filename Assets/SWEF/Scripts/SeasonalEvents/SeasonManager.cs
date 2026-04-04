// Phase 99 — Seasonal Live Events & Battle Pass
// Assets/SWEF/Scripts/SeasonalEvents/SeasonManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.SeasonalEvents
{
    /// <summary>
    /// Core singleton manager for seasonal content rotation.
    /// Loads season definitions from <c>Resources/Seasons/</c>, determines the currently
    /// active season, monitors season boundaries, and fires lifecycle events.
    /// </summary>
    /// <remarks>
    /// Attach to a persistent GameObject in the scene. The manager survives scene loads
    /// via <c>DontDestroyOnLoad</c>.
    /// </remarks>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public class SeasonManager : MonoBehaviour
    {
        #region Singleton
        /// <summary>Global singleton instance.</summary>
        public static SeasonManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSeasons();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        #endregion

        #region Events
        /// <summary>Fired when a new season becomes active. Provides the season's data.</summary>
        public event Action<SeasonData> OnSeasonStarted;

        /// <summary>Fired when the current season ends. Provides the ended season's data.</summary>
        public event Action<SeasonData> OnSeasonEnded;

        /// <summary>Fired after end-of-season rewards have been distributed to the player.</summary>
        public event Action OnSeasonRewardsDistributed;
        #endregion

        #region Inspector
        [Header("Season Resources")]
        [Tooltip("Sub-folder under Resources/ containing season JSON files.")]
        [SerializeField] private string seasonsResourceFolder = "Seasons";

        [Header("Poll Interval")]
        [Tooltip("How often (seconds) the manager checks for season transitions.")]
        [SerializeField, Range(10f, 3600f)] private float pollIntervalSeconds = 60f;
        #endregion

        #region State
        private readonly List<SeasonData> _allSeasons = new List<SeasonData>();
        private SeasonData _currentSeason;
        private Coroutine _pollCoroutine;

        /// <summary>The currently active season, or <c>null</c> if no season is running.</summary>
        public SeasonData CurrentSeason => _currentSeason;

        /// <summary>Read-only list of all loaded season definitions.</summary>
        public IReadOnlyList<SeasonData> AllSeasons => _allSeasons.AsReadOnly();
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            DetermineActiveSeason();
            _pollCoroutine = StartCoroutine(PollSeasonTransitions());
        }
        #endregion

        #region Season Loading
        private void LoadSeasons()
        {
            _allSeasons.Clear();

            // Load from Resources (TextAsset JSON files)
            var assets = Resources.LoadAll<TextAsset>(seasonsResourceFolder);
            foreach (var asset in assets)
            {
                if (asset == null) continue;
                try
                {
                    var data = SeasonData.FromJson(asset.text);
                    if (data != null) _allSeasons.Add(data);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SWEF] SeasonManager: Failed to parse season JSON '{asset.name}' — {ex.Message}");
                }
            }

            // Also try persistentDataPath for live-delivered seasons
            LoadFromPersistentPath();

            // Sort by start date
            _allSeasons.Sort((a, b) => a.GetStartDateUtc().CompareTo(b.GetStartDateUtc()));

            Debug.Log($"[SWEF] SeasonManager: Loaded {_allSeasons.Count} season(s).");
        }

        private void LoadFromPersistentPath()
        {
            var dir = Path.Combine(Application.persistentDataPath, "Seasons");
            if (!Directory.Exists(dir)) return;
            foreach (var file in Directory.GetFiles(dir, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var data = SeasonData.FromJson(json);
                    if (data != null) _allSeasons.Add(data);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SWEF] SeasonManager: Failed to load season from '{file}' — {ex.Message}");
                }
            }
        }

        /// <summary>Adds a season definition at runtime (e.g. from a server response).</summary>
        public void RegisterSeason(SeasonData season)
        {
            if (season == null) return;
            _allSeasons.Add(season);
            _allSeasons.Sort((a, b) => a.GetStartDateUtc().CompareTo(b.GetStartDateUtc()));
            DetermineActiveSeason();
        }
        #endregion

        #region Season Selection
        private void DetermineActiveSeason()
        {
            var now = DateTime.UtcNow;
            SeasonData newSeason = null;

            foreach (var s in _allSeasons)
            {
                if (now >= s.GetStartDateUtc() && now < s.GetEndDateUtc())
                {
                    newSeason = s;
                    break;
                }
            }

            if (newSeason == null && _allSeasons.Count > 0)
            {
                // Fallback: use the most recently started season to avoid a completely blank
                // experience between seasons. This behaviour can be overridden by clearing
                // _allSeasons or returning null before calling DetermineActiveSeason().
                newSeason = _allSeasons[_allSeasons.Count - 1];
            }

            if (newSeason != _currentSeason)
            {
                if (_currentSeason != null)
                    EndSeason(_currentSeason);

                _currentSeason = newSeason;

                if (_currentSeason != null)
                {
                    Debug.Log($"[SWEF] SeasonManager: Season started — '{_currentSeason.SeasonName}'");
                    OnSeasonStarted?.Invoke(_currentSeason);
                }
            }
        }

        private void EndSeason(SeasonData season)
        {
            Debug.Log($"[SWEF] SeasonManager: Season ended — '{season.SeasonName}'");
            OnSeasonEnded?.Invoke(season);
            DistributeEndOfSeasonRewards(season);
        }

        private void DistributeEndOfSeasonRewards(SeasonData season)
        {
            // Actual reward distribution delegates to BattlePassController.
            var bpc = BattlePassController.Instance;
            if (bpc != null)
                bpc.DistributeEndOfSeasonRewards(season);

            OnSeasonRewardsDistributed?.Invoke();
        }
        #endregion

        #region Poll Coroutine
        private IEnumerator PollSeasonTransitions()
        {
            while (true)
            {
                yield return new WaitForSeconds(pollIntervalSeconds);
                DetermineActiveSeason();
            }
        }
        #endregion
    }
}
