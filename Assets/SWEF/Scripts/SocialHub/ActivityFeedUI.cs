using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SWEF.SocialHub
{
    /// <summary>
    /// UI panel that displays the social activity feed maintained by <see cref="SocialActivityFeed"/>.
    /// Instantiates row prefabs in a scroll-view content container and subscribes to
    /// <see cref="SocialActivityFeed.OnActivityPosted"/> to update in real time.
    /// Call <see cref="Open"/> / <see cref="Close"/> to show or hide the panel.
    /// </summary>
    public class ActivityFeedUI : MonoBehaviour
    {
        // ── Inspector — Root ──────────────────────────────────────────────────────
        [Header("Root")]
        [SerializeField] private GameObject rootPanel;

        // ── Inspector — Feed content ──────────────────────────────────────────────
        [Header("Feed")]
        [Tooltip("Scroll-view content transform where activity rows are instantiated.")]
        [SerializeField] private RectTransform feedContainer;
        [Tooltip("Prefab with TextMeshProUGUI children for actor name, activity label, and timestamp.")]
        [SerializeField] private GameObject activityRowPrefab;
        [SerializeField] private TextMeshProUGUI emptyFeedLabel;

        // ── Inspector — Filter ────────────────────────────────────────────────────
        [Header("Filter")]
        [Tooltip("Dropdown to filter by ActivityType. First option = All.")]
        [SerializeField] private TMP_Dropdown activityTypeFilter;
        [SerializeField] private Button refreshButton;

        // ── State ─────────────────────────────────────────────────────────────────
        private readonly List<GameObject> _spawnedRows = new List<GameObject>();
        private ActivityType? _activeFilter;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (refreshButton != null)
                refreshButton.onClick.AddListener(Refresh);

            // Build filter dropdown from enum values.
            if (activityTypeFilter != null)
            {
                activityTypeFilter.ClearOptions();
                var options = new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("All") };
                foreach (ActivityType t in Enum.GetValues(typeof(ActivityType)))
                    options.Add(new TMP_Dropdown.OptionData(t.ToString()));
                activityTypeFilter.AddOptions(options);
                activityTypeFilter.onValueChanged.AddListener(OnFilterChanged);
            }
        }

        private void OnEnable()
        {
            if (SocialActivityFeed.Instance != null)
                SocialActivityFeed.Instance.OnActivityPosted += OnActivityPosted;
            Refresh();
        }

        private void OnDisable()
        {
            if (SocialActivityFeed.Instance != null)
                SocialActivityFeed.Instance.OnActivityPosted -= OnActivityPosted;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Shows the activity feed panel.</summary>
        public void Open()
        {
            if (rootPanel != null) rootPanel.SetActive(true);
            Refresh();
        }

        /// <summary>Hides the activity feed panel.</summary>
        public void Close()
        {
            if (rootPanel != null) rootPanel.SetActive(false);
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private void OnFilterChanged(int index)
        {
            // index 0 = All
            if (index == 0)
                _activeFilter = null;
            else
                _activeFilter = (ActivityType)(index - 1);

            Refresh();
        }

        private void OnActivityPosted(ActivityEntry entry)
        {
            // If a filter is active and this entry doesn't match, do a full refresh;
            // otherwise prepend a single row for efficiency.
            if (_activeFilter.HasValue && entry.activityType != _activeFilter.Value) return;
            PrependRow(entry);
        }

        /// <summary>Rebuilds the entire feed display from the current activity data.</summary>
        public void Refresh()
        {
            ClearRows();
            if (SocialActivityFeed.Instance == null) return;

            var entries = SocialActivityFeed.Instance.GetEntries();
            int count = 0;
            foreach (var e in entries)
            {
                if (_activeFilter.HasValue && e.activityType != _activeFilter.Value) continue;
                SpawnRow(e);
                count++;
            }

            if (emptyFeedLabel != null)
                emptyFeedLabel.gameObject.SetActive(count == 0);
        }

        private void PrependRow(ActivityEntry entry)
        {
            if (feedContainer == null || activityRowPrefab == null) return;
            var go = Instantiate(activityRowPrefab, feedContainer);
            go.transform.SetAsFirstSibling();
            _spawnedRows.Insert(0, go);
            BindRow(go, entry);

            if (emptyFeedLabel != null)
                emptyFeedLabel.gameObject.SetActive(false);
        }

        private void SpawnRow(ActivityEntry entry)
        {
            if (feedContainer == null || activityRowPrefab == null) return;
            var go = Instantiate(activityRowPrefab, feedContainer);
            _spawnedRows.Add(go);
            BindRow(go, entry);
        }

        private static void BindRow(GameObject go, ActivityEntry entry)
        {
            var texts = go.GetComponentsInChildren<TextMeshProUGUI>();
            // Convention: texts[0]=actor, texts[1]=activity label, texts[2]=timestamp
            if (texts.Length > 0) texts[0].text = entry.actorDisplayName;
            if (texts.Length > 1) texts[1].text = FormatActivityLabel(entry);
            if (texts.Length > 2) texts[2].text = FormatTimestamp(entry.timestampUtc);
        }

        private static string FormatActivityLabel(ActivityEntry entry)
        {
            return entry.activityType switch
            {
                ActivityType.FlightCompleted      => "completed a flight",
                ActivityType.AchievementUnlocked  => $"unlocked: {entry.contextLabel}",
                ActivityType.RankUp               => $"reached rank: {entry.contextLabel}",
                ActivityType.SeasonTierReached     => $"reached season tier {entry.contextLabel}",
                ActivityType.ChallengeCompleted    => "completed a daily challenge",
                ActivityType.BecameFriends         => $"became friends with {entry.contextLabel}",
                ActivityType.JoinedMultiplayer     => "joined a multiplayer session",
                _                                  => entry.contextLabel
            };
        }

        private static string FormatTimestamp(string iso)
        {
            if (DateTime.TryParse(iso, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime dt))
            {
                var delta = DateTime.UtcNow - dt;
                if (delta.TotalMinutes < 1) return "just now";
                if (delta.TotalHours   < 1) return $"{(int)delta.TotalMinutes}m ago";
                if (delta.TotalDays    < 1) return $"{(int)delta.TotalHours}h ago";
                return $"{(int)delta.TotalDays}d ago";
            }
            return iso;
        }

        private void ClearRows()
        {
            foreach (var go in _spawnedRows)
                if (go != null) Destroy(go);
            _spawnedRows.Clear();
        }
    }
}
