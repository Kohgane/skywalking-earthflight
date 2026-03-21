using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SWEF.Events
{
    /// <summary>
    /// Enumeration of tabs shown in the event calendar UI.
    /// </summary>
    public enum CalendarTab { ActiveNow, Upcoming, History }

    /// <summary>
    /// Full-screen calendar / schedule view for world events.
    /// Shows active, upcoming, and historical events with completion status.
    /// </summary>
    public class EventCalendarUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Panels")]
        [Tooltip("Root panel of the calendar. Toggle to show/hide the calendar.")]
        [SerializeField] private GameObject calendarRoot;

        [Header("Tab Buttons")]
        [SerializeField] private Button tabActiveNowButton;
        [SerializeField] private Button tabUpcomingButton;
        [SerializeField] private Button tabHistoryButton;

        [Header("List")]
        [Tooltip("Scroll-view content root where event entry prefabs are instantiated.")]
        [SerializeField] private RectTransform contentRoot;

        [Tooltip("Prefab used for each event entry row.")]
        [SerializeField] private GameObject eventEntryPrefab;

        [Header("Entry Sub-Elements (on prefab)")]
        [Tooltip("Name of the child Text component for the event name.")]
        [SerializeField] private string entryNameTextName   = "EventNameText";

        [Tooltip("Name of the child Text component for the time window.")]
        [SerializeField] private string entryTimeTextName   = "TimeWindowText";

        [Tooltip("Name of the child Text component for the reward.")]
        [SerializeField] private string entryRewardTextName = "RewardText";

        [Tooltip("Name of the child Text for status (Active / Upcoming / Completed).")]
        [SerializeField] private string entryStatusTextName = "StatusText";

        [Tooltip("Name of the child Button for navigating to event.")]
        [SerializeField] private string entryNavButtonName  = "NavigateButton";

        // ── Internal state ────────────────────────────────────────────────────────
        private EventScheduler            _scheduler;
        private EventParticipationTracker _tracker;
        private CalendarTab               _currentTab = CalendarTab.ActiveNow;
        private readonly List<GameObject> _entries    = new List<GameObject>();
        private SWEF.GuidedTour.WaypointNavigator _waypointNavigator;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (calendarRoot != null) calendarRoot.SetActive(false);
        }

        private void OnEnable()
        {
            _scheduler         = FindFirstObjectByType<EventScheduler>();
            _tracker           = FindFirstObjectByType<EventParticipationTracker>();
            _waypointNavigator = FindFirstObjectByType<SWEF.GuidedTour.WaypointNavigator>();

            if (tabActiveNowButton != null) tabActiveNowButton.onClick.AddListener(() => SwitchTab(CalendarTab.ActiveNow));
            if (tabUpcomingButton  != null) tabUpcomingButton.onClick.AddListener(() => SwitchTab(CalendarTab.Upcoming));
            if (tabHistoryButton   != null) tabHistoryButton.onClick.AddListener(() => SwitchTab(CalendarTab.History));
        }

        private void OnDisable()
        {
            if (tabActiveNowButton != null) tabActiveNowButton.onClick.RemoveAllListeners();
            if (tabUpcomingButton  != null) tabUpcomingButton.onClick.RemoveAllListeners();
            if (tabHistoryButton   != null) tabHistoryButton.onClick.RemoveAllListeners();
        }

        // ── Public API ────────────────────────────────────────────────────────────
        /// <summary>
        /// Opens the calendar and displays the <see cref="CalendarTab.ActiveNow"/> tab.
        /// </summary>
        public void Open()
        {
            if (calendarRoot != null) calendarRoot.SetActive(true);
            SwitchTab(_currentTab);
        }

        /// <summary>
        /// Closes the calendar panel.
        /// </summary>
        public void Close()
        {
            if (calendarRoot != null) calendarRoot.SetActive(false);
        }

        /// <summary>
        /// Toggles the calendar open / closed.
        /// </summary>
        public void Toggle()
        {
            if (calendarRoot == null) return;
            if (calendarRoot.activeSelf) Close(); else Open();
        }

        /// <summary>
        /// Switches to the specified tab and rebuilds the event list.
        /// </summary>
        /// <param name="tab">Tab to display.</param>
        public void SwitchTab(CalendarTab tab)
        {
            _currentTab = tab;
            RebuildList();
        }

        // ── Internal helpers ──────────────────────────────────────────────────────
        private void RebuildList()
        {
            ClearEntries();

            switch (_currentTab)
            {
                case CalendarTab.ActiveNow:
                    PopulateActiveNow();
                    break;
                case CalendarTab.Upcoming:
                    PopulateUpcoming();
                    break;
                case CalendarTab.History:
                    PopulateHistory();
                    break;
            }
        }

        private void ClearEntries()
        {
            foreach (var go in _entries) Destroy(go);
            _entries.Clear();
        }

        private void PopulateActiveNow()
        {
            if (_scheduler == null) return;

            foreach (var inst in _scheduler.GetActiveEvents())
            {
                var entry = CreateEntry(
                    name:      inst.eventData?.eventName ?? "Event",
                    timeWindow: $"Ends in {FormatSeconds(inst.RemainingTime)}",
                    reward:    $"{inst.eventData?.xpReward} XP",
                    status:    inst.state.ToString(),
                    instance:  inst);
                _entries.Add(entry);
            }
        }

        private void PopulateUpcoming()
        {
            if (_scheduler == null) return;

            foreach (var data in _scheduler.GetUpcomingEvents())
            {
                var entry = CreateEntryFromData(
                    name:      data.eventName,
                    timeWindow: $"Up to {data.maxDurationMinutes:F0} min",
                    reward:    $"{data.xpReward} XP",
                    status:    "Upcoming",
                    data:      data);
                _entries.Add(entry);
            }
        }

        private void PopulateHistory()
        {
            if (_tracker == null) return;

            foreach (var record in _tracker.GetParticipationHistory())
            {
                string status = record.completed ? "Completed" : "Participated";
                var entry = CreateEntryFromRecord(
                    name:      record.eventId,
                    timeWindow: $"{record.totalParticipationSeconds:F0} s participated",
                    reward:    record.completed ? $"+{record.xpEarned} XP earned" : "Not completed",
                    status:    status);
                _entries.Add(entry);
            }
        }

        private GameObject CreateEntry(string name, string timeWindow, string reward,
                                        string status, WorldEventInstance instance)
        {
            var go = InstantiateEntry(name, timeWindow, reward, status);
            if (go == null) return go;

            // Wire up Navigate button
            var btn = go.transform.Find(entryNavButtonName)?.GetComponent<Button>();
            if (btn != null && instance != null)
            {
                btn.onClick.AddListener(() =>
                {
                    if (_waypointNavigator != null)
                    {
                        _waypointNavigator.SetManualTarget(instance.spawnPosition);
                        _waypointNavigator.EnableAutoPilot();
                    }
                    Close();
                });
            }

            return go;
        }

        private GameObject CreateEntryFromData(string name, string timeWindow, string reward,
                                                string status, WorldEventData data)
        {
            return InstantiateEntry(name, timeWindow, reward, status);
        }

        private GameObject CreateEntryFromRecord(string name, string timeWindow, string reward, string status)
        {
            return InstantiateEntry(name, timeWindow, reward, status);
        }

        private GameObject InstantiateEntry(string name, string timeWindow, string reward, string status)
        {
            if (eventEntryPrefab == null || contentRoot == null) return null;

            var go = Instantiate(eventEntryPrefab, contentRoot);
            SetChildText(go, entryNameTextName,   name);
            SetChildText(go, entryTimeTextName,   timeWindow);
            SetChildText(go, entryRewardTextName, reward);
            SetChildText(go, entryStatusTextName, status);
            return go;
        }

        private static void SetChildText(GameObject root, string childName, string text)
        {
            if (root == null || string.IsNullOrEmpty(childName)) return;
            var child = root.transform.Find(childName);
            if (child == null) return;

            var tmp = child.GetComponent<TMP_Text>();
            if (tmp != null) { tmp.text = text; return; }

            var legacy = child.GetComponent<Text>();
            if (legacy != null) legacy.text = text;
        }

        private static string FormatSeconds(float seconds)
        {
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            return $"{m:D2}:{s:D2}";
        }
    }
}
