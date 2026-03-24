using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.Wildlife
{
    /// <summary>
    /// Phase 75 — Connects wildlife encounters to the flight journal and achievement systems.
    /// Tracks species collection, handles cooldowns and deduplication,
    /// and persists the collection to disk.
    /// </summary>
    public class WildlifeJournalIntegration : MonoBehaviour
    {
        #region Events

        /// <summary>Fired when a species is added to the collection.</summary>
        public event System.Action<string, int> OnSpeciesCollected;

        /// <summary>Fired when all species have been discovered.</summary>
        public event System.Action OnCollectionComplete;

        #endregion

        #region Private State

        private readonly Dictionary<string, int>   _encounterCount     = new Dictionary<string, int>();
        private readonly HashSet<string>            _discoveredSpecies  = new HashSet<string>();
        private readonly Dictionary<string, float>  _lastReportTime     = new Dictionary<string, float>();
        private int _totalSpeciesCount;
        private const string SaveFileName = "wildlife_collection.json";

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            LoadCollection();
            SubscribeToManager();

            // Total species count: 15 default species registered by WildlifeManager
            _totalSpeciesCount = 15;
        }

        private void OnDestroy()
        {
            UnsubscribeFromManager();
            SaveCollection();
        }

        #endregion

        #region Manager Subscriptions

        private void SubscribeToManager()
        {
            var mgr = WildlifeManager.Instance;
            if (mgr == null) return;
            mgr.OnEncounterRecorded += HandleEncounter;
            mgr.OnSpeciesDiscovered += HandleDiscovery;
        }

        private void UnsubscribeFromManager()
        {
            var mgr = WildlifeManager.Instance;
            if (mgr == null) return;
            mgr.OnEncounterRecorded -= HandleEncounter;
            mgr.OnSpeciesDiscovered -= HandleDiscovery;
        }

        #endregion

        #region Encounter Handling

        private void HandleEncounter(WildlifeEncounterRecord record)
        {
            if (record == null) return;

            // Cooldown check
            float now = Time.time;
            float cooldown = WildlifeManager.Instance?.Config?.detectionReportCooldown ?? 10f;
            if (_lastReportTime.TryGetValue(record.speciesId, out float last) &&
                (now - last) < cooldown) return;
            _lastReportTime[record.speciesId] = now;

            // Count encounters
            if (!_encounterCount.ContainsKey(record.speciesId))
                _encounterCount[record.speciesId] = 0;
            _encounterCount[record.speciesId]++;

            // Push to journal (null-safe)
#if SWEF_JOURNAL_AVAILABLE
            var jm = SWEF.Journal.JournalManager.Instance;
            jm?.LogWildlifeEncounter(record);
#endif

            SaveCollection();
        }

        private void HandleDiscovery(WildlifeSpecies species)
        {
            if (species == null) return;
            bool isNew = _discoveredSpecies.Add(species.speciesId);
            if (!isNew) return;

            int total = _discoveredSpecies.Count;
            OnSpeciesCollected?.Invoke(species.speciesId, total);

            // Achievement bridge (null-safe)
            ReportAchievementProgress(total);

            if (total >= _totalSpeciesCount)
                OnCollectionComplete?.Invoke();

            SaveCollection();
        }

        #endregion

        #region Photo Mode Integration

        /// <summary>Marks an encounter as photographed (call from PhotoCaptureManager).</summary>
        public void MarkPhotographed(string speciesId)
        {
#if SWEF_JOURNAL_AVAILABLE
            var jm = SWEF.Journal.JournalManager.Instance;
            jm?.MarkWildlifePhotographed(speciesId);
#endif
            ReportPhotoAchievement();
        }

        #endregion

        #region Achievement Bridge

        private void ReportAchievementProgress(int discovered)
        {
#if SWEF_ACHIEVEMENT_AVAILABLE
            var am = SWEF.Achievement.AchievementManager.Instance;
            if (am == null) return;
            if (discovered >= 1)  am.ReportProgress("wildlife_first_encounter", 1);
            if (discovered >= 10) am.ReportProgress("wildlife_bird_watcher", discovered);
#endif
        }

        private void ReportPhotoAchievement()
        {
#if SWEF_ACHIEVEMENT_AVAILABLE
            var am = SWEF.Achievement.AchievementManager.Instance;
            am?.ReportProgress("wildlife_photographer", 1);
#endif
        }

        #endregion

        #region Persistence

        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        private void SaveCollection()
        {
            try
            {
                var data = new CollectionSaveData
                {
                    discovered = new System.Collections.Generic.List<string>(_discoveredSpecies)
                };
                File.WriteAllText(SavePath, JsonUtility.ToJson(data));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[WildlifeJournal] Save failed: {e.Message}");
            }
        }

        private void LoadCollection()
        {
            try
            {
                if (!File.Exists(SavePath)) return;
                var data = JsonUtility.FromJson<CollectionSaveData>(
                    File.ReadAllText(SavePath));
                if (data?.discovered == null) return;
                foreach (var id in data.discovered)
                    _discoveredSpecies.Add(id);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[WildlifeJournal] Load failed: {e.Message}");
            }
        }

        [System.Serializable]
        private class CollectionSaveData
        {
            public List<string> discovered = new List<string>();
        }

        #endregion

        #region Public API

        /// <summary>Returns how many times the given species has been encountered.</summary>
        public int GetEncounterCount(string speciesId) =>
            _encounterCount.TryGetValue(speciesId, out int c) ? c : 0;

        /// <summary>Returns the set of discovered species IDs.</summary>
        public IReadOnlyCollection<string> DiscoveredSpecies => _discoveredSpecies;

        /// <summary>Total number of discovered species.</summary>
        public int DiscoveredCount => _discoveredSpecies.Count;

        #endregion
    }
}
