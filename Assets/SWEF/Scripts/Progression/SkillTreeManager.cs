using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.Progression
{
    /// <summary>
    /// Singleton manager for the pilot skill tree.
    /// Tracks available skill points, unlocked skills, and computes cumulative
    /// effect bonuses from all unlocked nodes.
    /// Persists state to <c>Application.persistentDataPath/skills.json</c>.
    /// </summary>
    [DefaultExecutionOrder(-49)]
    public class SkillTreeManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static SkillTreeManager Instance { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when a skill is successfully unlocked.</summary>
        public event Action<SkillTreeData> OnSkillUnlocked;

        /// <summary>Fired after all skills have been reset and points refunded.</summary>
        public event Action OnSkillsReset;

        // ── Persistence ───────────────────────────────────────────────────────────
        private static readonly string SaveFileName = "skills.json";
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        [Serializable]
        private class SkillsSaveData
        {
            public int          availableSkillPoints;
            public List<string> unlockedSkillIds = new List<string>();
        }

        private SkillsSaveData _save = new SkillsSaveData();

        // ── Runtime state ─────────────────────────────────────────────────────────
        private readonly HashSet<string> _unlockedIds = new HashSet<string>();
        private readonly Dictionary<string, SkillTreeData> _catalog = new Dictionary<string, SkillTreeData>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadCatalog();
            Load();
        }

        private void OnEnable()
        {
            if (ProgressionManager.Instance != null)
                ProgressionManager.Instance.OnRankUp += HandleRankUp;
        }

        private void OnDisable()
        {
            if (ProgressionManager.Instance != null)
                ProgressionManager.Instance.OnRankUp -= HandleRankUp;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to unlock a skill by its ID.
        /// Checks that the player has enough skill points and all prerequisites are met.
        /// </summary>
        /// <returns><c>true</c> if the skill was successfully unlocked.</returns>
        public bool UnlockSkill(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return false;
            if (_unlockedIds.Contains(skillId))
            {
                Debug.Log($"[SWEF] SkillTreeManager: Skill '{skillId}' is already unlocked.");
                return false;
            }

            if (!_catalog.TryGetValue(skillId, out SkillTreeData skill))
            {
                Debug.LogWarning($"[SWEF] SkillTreeManager: Unknown skill '{skillId}'.");
                return false;
            }

            if (_save.availableSkillPoints < skill.skillPointCost)
            {
                Debug.Log($"[SWEF] SkillTreeManager: Not enough skill points to unlock '{skillId}' (need {skill.skillPointCost}, have {_save.availableSkillPoints}).");
                return false;
            }

            // Check prerequisites
            foreach (var prereq in skill.prerequisiteSkillIds)
            {
                if (!_unlockedIds.Contains(prereq))
                {
                    Debug.Log($"[SWEF] SkillTreeManager: Prerequisite '{prereq}' not met for skill '{skillId}'.");
                    return false;
                }
            }

            _save.availableSkillPoints -= skill.skillPointCost;
            _unlockedIds.Add(skillId);
            _save.unlockedSkillIds.Add(skillId);

            Save();
            OnSkillUnlocked?.Invoke(skill);
            Debug.Log($"[SWEF] SkillTreeManager: Unlocked skill '{skillId}'.");
            return true;
        }

        /// <summary>Returns whether a skill with the given ID has been unlocked.</summary>
        public bool IsSkillUnlocked(string skillId) =>
            !string.IsNullOrEmpty(skillId) && _unlockedIds.Contains(skillId);

        /// <summary>
        /// Returns the cumulative bonus from all unlocked skills that share the given effect type.
        /// </summary>
        public float GetSkillEffect(SkillEffect effectType)
        {
            float total = 0f;
            foreach (var id in _unlockedIds)
            {
                if (_catalog.TryGetValue(id, out var skill) && skill.effect == effectType)
                    total += skill.effectValue;
            }
            return total;
        }

        /// <summary>Returns the number of unspent skill points.</summary>
        public int GetAvailableSkillPoints() => _save.availableSkillPoints;

        /// <summary>
        /// Refunds all spent skill points and clears unlocked skills.
        /// Call with user confirmation before invoking.
        /// </summary>
        public void ResetSkills()
        {
            int refunded = 0;
            foreach (var id in _unlockedIds)
            {
                if (_catalog.TryGetValue(id, out var skill))
                    refunded += skill.skillPointCost;
            }
            _save.availableSkillPoints += refunded;
            _unlockedIds.Clear();
            _save.unlockedSkillIds.Clear();

            Save();
            OnSkillsReset?.Invoke();
            Debug.Log($"[SWEF] SkillTreeManager: Reset all skills, refunded {refunded} points.");
        }

        /// <summary>Returns all skills in the catalog.</summary>
        public IReadOnlyCollection<SkillTreeData> GetAllSkills() => _catalog.Values;

        // ── Private helpers ───────────────────────────────────────────────────────

        private void LoadCatalog()
        {
            var assets = Resources.LoadAll<SkillTreeData>("Skills");
            _catalog.Clear();
            if (assets.Length > 0)
            {
                foreach (var s in assets)
                    _catalog[s.skillId] = s;
                Debug.Log($"[SWEF] SkillTreeManager: Loaded {assets.Length} skill definitions.");
            }
            else
            {
                foreach (var s in ProgressionDefaultData.GetDefaultSkills())
                    _catalog[s.skillId] = s;
                Debug.Log("[SWEF] SkillTreeManager: No skill assets found in Resources/Skills/. Using built-in defaults.");
            }
        }

        private void HandleRankUp(PilotRankData oldRank, PilotRankData newRank)
        {
            // 1 point per rank-up, plus bonus points at tier thresholds
            int bonus = 1;
            if (newRank.rankLevel % 10 == 0) bonus += 2; // extra bonus every 10 levels
            _save.availableSkillPoints += bonus;
            Save();
            Debug.Log($"[SWEF] SkillTreeManager: Granted {bonus} skill point(s) for rank-up to level {newRank.rankLevel}.");
        }

        // ── Persistence ───────────────────────────────────────────────────────────

        private void Save()
        {
            try { File.WriteAllText(SavePath, JsonUtility.ToJson(_save, true)); }
            catch (Exception ex) { Debug.LogWarning($"[SWEF] SkillTreeManager: Save failed — {ex.Message}"); }
        }

        private void Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    _save = JsonUtility.FromJson<SkillsSaveData>(File.ReadAllText(SavePath)) ?? new SkillsSaveData();
                    _unlockedIds.Clear();
                    foreach (var id in _save.unlockedSkillIds)
                        _unlockedIds.Add(id);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] SkillTreeManager: Load failed — {ex.Message}");
                _save = new SkillsSaveData();
            }
        }

        private void OnApplicationQuit() => Save();
        private void OnApplicationPause(bool p) { if (p) Save(); }
    }
}
