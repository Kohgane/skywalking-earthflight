using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.Events
{
    /// <summary>
    /// MonoBehaviour that tracks the player's participation in world events.
    /// Detects when the player enters an event's region, measures participation time,
    /// determines completion, grants XP/achievements, and persists history to JSON.
    /// </summary>
    public class EventParticipationTracker : MonoBehaviour
    {
        // ── Inner types ───────────────────────────────────────────────────────────
        /// <summary>
        /// Snapshot of a player's involvement in a single event instance.
        /// </summary>
        [Serializable]
        public struct EventParticipation
        {
            /// <summary>Guid of the <see cref="WorldEventInstance"/> this record refers to.</summary>
            public string instanceId;           // string for JSON serialisation

            /// <summary>eventId from the linked <see cref="WorldEventData"/>.</summary>
            public string eventId;

            /// <summary><see cref="Time.time"/> when the player first entered the event region.</summary>
            public float joinedTime;

            /// <summary>Cumulative seconds the player has spent inside the event region.</summary>
            public float totalParticipationSeconds;

            /// <summary>Whether the player met the participation threshold for completion.</summary>
            public bool completed;

            /// <summary>XP earned for this participation (0 if not completed).</summary>
            public int xpEarned;
        }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Settings")]
        [Tooltip("Fraction of event duration the player must spend inside the radius to count as completed.")]
        [SerializeField, Range(0f, 1f)] private float completionThreshold = 0.25f;

        [Header("References")]
        [Tooltip("Transform used as the player's position for distance checks. Auto-resolved if null.")]
        [SerializeField] private Transform playerTransform;

        // ── Internal state ────────────────────────────────────────────────────────
        private EventScheduler _scheduler;

        // instanceId (string) → active participation record
        private readonly Dictionary<string, EventParticipation> _active =
            new Dictionary<string, EventParticipation>();

        private readonly List<EventParticipation> _history = new List<EventParticipation>();

        private static readonly string SaveFileName = "event_participation.json";

        [Serializable]
        private class SaveData { public List<EventParticipation> records = new List<EventParticipation>(); }

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (playerTransform == null)
            {
                var fc = FindFirstObjectByType<SWEF.Flight.FlightController>();
                if (fc != null) playerTransform = fc.transform;
            }
            LoadHistory();
        }

        private void OnEnable()
        {
            _scheduler = FindFirstObjectByType<EventScheduler>();
            if (_scheduler != null)
            {
                _scheduler.OnEventSpawned  += HandleEventSpawned;
                _scheduler.OnEventExpired  += HandleEventExpired;
            }
        }

        private void OnDisable()
        {
            if (_scheduler != null)
            {
                _scheduler.OnEventSpawned  -= HandleEventSpawned;
                _scheduler.OnEventExpired  -= HandleEventExpired;
            }
        }

        private void Update()
        {
            if (playerTransform == null) return;

            var activeEvents = _scheduler != null
                ? _scheduler.GetActiveEvents()
                : new List<WorldEventInstance>();

            foreach (var inst in activeEvents)
            {
                if (inst.state == WorldEventState.Ended) continue;

                string key = inst.instanceId.ToString();
                float dist = Vector3.Distance(playerTransform.position, inst.spawnPosition);
                bool inRegion = dist <= inst.eventData.spawnRegion.radius;

                if (inRegion)
                {
                    if (!_active.ContainsKey(key))
                    {
                        _active[key] = new EventParticipation
                        {
                            instanceId = key,
                            eventId    = inst.eventData.eventId,
                            joinedTime = Time.time
                        };
                    }

                    var record = _active[key];
                    record.totalParticipationSeconds += Time.deltaTime;

                    float eventDuration = inst.eventData.maxDurationMinutes * 60f;
                    if (!record.completed &&
                        record.totalParticipationSeconds >= eventDuration * completionThreshold)
                    {
                        record.completed = true;
                        record.xpEarned  = inst.eventData.xpReward;
                        GrantRewardsForEvent(inst, record);
                    }

                    _active[key] = record;
                }
            }
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) SaveHistory();
        }

        private void OnDestroy()
        {
            SaveHistory();
        }

        // ── Public API ────────────────────────────────────────────────────────────
        /// <summary>
        /// Returns a copy of the full participation history (completed and in-progress records).
        /// </summary>
        public List<EventParticipation> GetParticipationHistory()
        {
            return new List<EventParticipation>(_history);
        }

        /// <summary>
        /// Returns a list of participation records for events the player is currently in.
        /// </summary>
        public List<EventParticipation> GetActiveParticipation()
        {
            return new List<EventParticipation>(_active.Values);
        }

        /// <summary>
        /// Returns <c>true</c> if the player is currently inside the region of the
        /// specified event instance.
        /// </summary>
        /// <param name="instanceId">Guid of the event instance to query.</param>
        public bool IsParticipatingIn(Guid instanceId)
        {
            return _active.ContainsKey(instanceId.ToString());
        }

        // ── Internal helpers ──────────────────────────────────────────────────────
        private void HandleEventSpawned(WorldEventInstance inst)
        {
            // Nothing to do on spawn — tracking starts when player enters region.
        }

        private void HandleEventExpired(WorldEventInstance inst)
        {
            string key = inst.instanceId.ToString();
            if (_active.TryGetValue(key, out var record))
            {
                _history.Add(record);
                _active.Remove(key);
                SaveHistory();
            }
        }

        private void GrantRewardsForEvent(WorldEventInstance inst, EventParticipation record)
        {
            Debug.Log($"[SWEF] EventParticipationTracker: player completed event '{inst.eventData.eventId}', earned {record.xpEarned} XP.");

            var achievementManager = FindFirstObjectByType<SWEF.Achievement.AchievementManager>();
            if (achievementManager != null && !string.IsNullOrEmpty(inst.eventData.achievementId))
                achievementManager.TryUnlock(inst.eventData.achievementId);

            var rewardController = FindFirstObjectByType<EventRewardController>();
            if (rewardController != null)
            {
                var rewards = new List<EventRewardController.RewardItem>
                {
                    new EventRewardController.RewardItem
                    {
                        type        = EventRewardController.RewardType.XP,
                        id          = inst.eventData.eventId,
                        amount      = record.xpEarned,
                        displayName = $"+{record.xpEarned} XP"
                    }
                };

                if (!string.IsNullOrEmpty(inst.eventData.achievementId))
                {
                    rewards.Add(new EventRewardController.RewardItem
                    {
                        type        = EventRewardController.RewardType.Achievement,
                        id          = inst.eventData.achievementId,
                        amount      = 1,
                        displayName = inst.eventData.achievementId
                    });
                }

                rewardController.ShowRewardPopup(rewards);
            }
        }

        private void SaveHistory()
        {
            try
            {
                var combined = new List<EventParticipation>(_history);
                combined.AddRange(_active.Values);

                string path = Path.Combine(Application.persistentDataPath, SaveFileName);
                string json = JsonUtility.ToJson(new SaveData { records = combined }, prettyPrint: true);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] EventParticipationTracker: failed to save history — {ex.Message}");
            }
        }

        private void LoadHistory()
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, SaveFileName);
                if (!File.Exists(path)) return;

                string json = File.ReadAllText(path);
                var save = JsonUtility.FromJson<SaveData>(json);
                if (save?.records != null)
                    _history.AddRange(save.records);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] EventParticipationTracker: failed to load history — {ex.Message}");
            }
        }
    }
}
