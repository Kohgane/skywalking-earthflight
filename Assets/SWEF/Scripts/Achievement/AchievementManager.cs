using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.Achievement
{
    /// <summary>
    /// Central achievement manager for Phase 31.
    /// Replaces the earlier PlayerPrefs-based implementation with a JSON-persisted,
    /// ScriptableObject-driven system.
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────────
        /// <summary>Fallback target value used when unlocking a legacy achievement that has no definition.</summary>
        private const float DefaultTargetValue = 1f;

        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Singleton instance.</summary>
        public static AchievementManager Instance { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired whenever a new achievement is unlocked.</summary>
        public System.Action<AchievementDefinition> OnAchievementUnlocked;

        // ── Internal state ────────────────────────────────────────────────────────
        private Dictionary<string, AchievementDefinition> _definitions =
            new Dictionary<string, AchievementDefinition>();

        private Dictionary<string, AchievementState> _states =
            new Dictionary<string, AchievementState>();

        private static readonly string SaveFileName = "achievements.json";

        // ── Serialization wrapper ─────────────────────────────────────────────────
        [System.Serializable]
        private class SaveData
        {
            public List<AchievementState> states = new List<AchievementState>();
        }

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

            LoadDefinitions();
            LoadStates();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) SaveStates();
        }

        private void OnApplicationQuit()
        {
            SaveStates();
        }

        // ── Definition loading ────────────────────────────────────────────────────
        private void LoadDefinitions()
        {
            var defs = Resources.LoadAll<AchievementDefinition>("Achievements");
            foreach (var def in defs)
            {
                if (string.IsNullOrEmpty(def.id))
                {
                    Debug.LogWarning($"[SWEF] AchievementManager: Definition '{def.name}' has no id — skipped.");
                    continue;
                }
                _definitions[def.id] = def;
            }
            Debug.Log($"[SWEF] AchievementManager: {_definitions.Count} achievement definitions loaded.");
        }

        // ── Persistence ───────────────────────────────────────────────────────────
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        private void LoadStates()
        {
            if (File.Exists(SavePath))
            {
                try
                {
                    string json = File.ReadAllText(SavePath);
                    var saveData = JsonUtility.FromJson<SaveData>(json);
                    if (saveData?.states != null)
                    {
                        foreach (var state in saveData.states)
                            _states[state.achievementId] = state;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[SWEF] AchievementManager: Failed to load save data — {ex.Message}");
                }
            }

            // Ensure every definition has a matching state entry.
            foreach (var kvp in _definitions)
            {
                if (!_states.ContainsKey(kvp.Key))
                    _states[kvp.Key] = new AchievementState { achievementId = kvp.Key };

                _states[kvp.Key].SetTarget(kvp.Value.targetValue);
            }
        }

        private void SaveStates()
        {
            try
            {
                var saveData = new SaveData();
                foreach (var state in _states.Values)
                    saveData.states.Add(state);

                string json = JsonUtility.ToJson(saveData, prettyPrint: true);
                File.WriteAllText(SavePath, json);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SWEF] AchievementManager: Failed to save data — {ex.Message}");
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Reports incremental progress for an achievement.
        /// Adds <paramref name="value"/> to the current accumulated value.
        /// </summary>
        public void ReportProgress(string achievementId, float value)
        {
            if (!_states.TryGetValue(achievementId, out var state)) return;
            if (state.unlocked) return;

            state.currentValue += value;
            CheckUnlock(achievementId, state);
        }

        /// <summary>
        /// Sets the absolute progress value for an achievement.
        /// Only increases — will not decrease an already higher value.
        /// </summary>
        public void SetProgress(string achievementId, float absoluteValue)
        {
            if (!_states.TryGetValue(achievementId, out var state)) return;
            if (state.unlocked) return;

            state.currentValue = Mathf.Max(state.currentValue, absoluteValue);
            CheckUnlock(achievementId, state);
        }

        /// <summary>Returns whether the achievement with the given id has been unlocked.</summary>
        public bool IsUnlocked(string achievementId)
        {
            return _states.TryGetValue(achievementId, out var state) && state.unlocked;
        }

        /// <summary>Returns normalised progress [0, 1] for the given achievement.</summary>
        public float GetProgress01(string achievementId)
        {
            return _states.TryGetValue(achievementId, out var state) ? state.Progress01 : 0f;
        }

        /// <summary>Returns the <see cref="AchievementDefinition"/> for a given id, or null.</summary>
        public AchievementDefinition GetDefinition(string achievementId)
        {
            return _definitions.TryGetValue(achievementId, out var def) ? def : null;
        }

        /// <summary>Returns a snapshot list of all achievement states.</summary>
        public List<AchievementState> GetAllStates()
        {
            return new List<AchievementState>(_states.Values);
        }

        /// <summary>Returns the total XP earned from unlocked achievements.</summary>
        public int GetTotalXP()
        {
            int xp = 0;
            foreach (var kvp in _states)
            {
                if (kvp.Value.unlocked && _definitions.TryGetValue(kvp.Key, out var def))
                    xp += def.xpReward;
            }
            return xp;
        }

        /// <summary>
        /// Resets all achievement progress. Intended for debug/development use only.
        /// </summary>
        public void ResetAll()
        {
            _states.Clear();
            foreach (var kvp in _definitions)
            {
                var state = new AchievementState { achievementId = kvp.Key };
                state.SetTarget(kvp.Value.targetValue);
                _states[kvp.Key] = state;
            }
            SaveStates();
            Debug.Log("[SWEF] AchievementManager: All achievement progress reset.");
        }

        /// <summary>
        /// Legacy unlock helper retained for backward compatibility with other systems.
        /// Prefer <see cref="SetProgress"/> or <see cref="ReportProgress"/> for new code.
        /// </summary>
        public bool TryUnlock(string id) => TryUnlockInternal(id);

        // ── Internal helpers ──────────────────────────────────────────────────────
        private void CheckUnlock(string achievementId, AchievementState state)
        {
            if (!_definitions.TryGetValue(achievementId, out var def)) return;
            if (state.currentValue >= def.targetValue)
                TryUnlockInternal(achievementId);
        }

        private bool TryUnlockInternal(string achievementId)
        {
            if (IsUnlocked(achievementId)) return false;

            if (!_states.TryGetValue(achievementId, out var state))
            {
                // Create on-the-fly for IDs not in definitions (legacy).
                state = new AchievementState { achievementId = achievementId };
                _states[achievementId] = state;
            }

            state.unlocked      = true;
            state.currentValue  = _definitions.TryGetValue(achievementId, out var defRef)
                ? defRef.targetValue
                : DefaultTargetValue;
            state.unlockDateISO = System.DateTime.UtcNow.ToString("o");

            Debug.Log($"[SWEF] Achievement unlocked: {achievementId}");

            if (defRef != null)
            {
                OnAchievementUnlocked?.Invoke(defRef);
                SWEF.Audio.AudioManager.Instance?.PlaySFX("AchievementUnlock");
            }

            // Phase 21 — telemetry
            var dispatcher = SWEF.Analytics.TelemetryDispatcher.Instance;
            if (dispatcher != null)
            {
                var evt = SWEF.Analytics.TelemetryEventBuilder
                    .Create(SWEF.Analytics.AnalyticsEvents.AchievementUnlocked)
                    .WithCategory("achievement")
                    .WithProperty("achievementId", achievementId)
                    .Build();
                dispatcher.EnqueueEvent(evt);
            }

            SaveStates();
            return true;
        }
    }
}
