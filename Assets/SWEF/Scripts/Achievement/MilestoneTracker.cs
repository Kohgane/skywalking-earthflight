using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.Achievement
{
    /// <summary>
    /// Monitors milestone completion whenever an achievement is unlocked.
    /// Persists completed milestone IDs to <c>Application.persistentDataPath/milestones.json</c>.
    /// </summary>
    public class MilestoneTracker : MonoBehaviour
    {
        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when a milestone is completed.</summary>
        public System.Action<MilestoneDefinition> OnMilestoneCompleted;

        // ── State ─────────────────────────────────────────────────────────────────
        private List<MilestoneDefinition>   _milestones = new List<MilestoneDefinition>();
        private HashSet<string>             _completed  = new HashSet<string>();

        private static readonly string SaveFileName = "milestones.json";

        [System.Serializable]
        private class SaveData { public List<string> completedIds = new List<string>(); }

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            LoadMilestones();
            LoadState();
        }

        private void OnEnable()
        {
            if (AchievementManager.Instance != null)
                AchievementManager.Instance.OnAchievementUnlocked += OnAchievementUnlocked;
        }

        private void OnDisable()
        {
            if (AchievementManager.Instance != null)
                AchievementManager.Instance.OnAchievementUnlocked -= OnAchievementUnlocked;
        }

        private void OnApplicationQuit()    => SaveState();
        private void OnApplicationPause(bool p) { if (p) SaveState(); }

        // ── Internal ─────────────────────────────────────────────────────────────
        private void LoadMilestones()
        {
            var loaded = Resources.LoadAll<MilestoneDefinition>("Milestones");
            _milestones.AddRange(loaded);
            Debug.Log($"[SWEF] MilestoneTracker: {_milestones.Count} milestone definitions loaded.");
        }

        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        private void LoadState()
        {
            if (!File.Exists(SavePath)) return;
            try
            {
                string json = File.ReadAllText(SavePath);
                var data    = JsonUtility.FromJson<SaveData>(json);
                if (data?.completedIds != null)
                    foreach (var id in data.completedIds)
                        _completed.Add(id);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SWEF] MilestoneTracker: Load failed — {ex.Message}");
            }
        }

        private void SaveState()
        {
            try
            {
                var data = new SaveData();
                data.completedIds.AddRange(_completed);
                File.WriteAllText(SavePath, JsonUtility.ToJson(data, prettyPrint: true));
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SWEF] MilestoneTracker: Save failed — {ex.Message}");
            }
        }

        private void OnAchievementUnlocked(AchievementDefinition _)
        {
            CheckMilestones();
        }

        private void CheckMilestones()
        {
            var mgr = AchievementManager.Instance;
            if (mgr == null) return;

            foreach (var milestone in _milestones)
            {
                if (_completed.Contains(milestone.id)) continue;
                if (!milestone.IsComplete(mgr)) continue;

                _completed.Add(milestone.id);
                Debug.Log($"[SWEF] Milestone completed: {milestone.id}");
                OnMilestoneCompleted?.Invoke(milestone);
                SaveState();
            }
        }
    }
}
