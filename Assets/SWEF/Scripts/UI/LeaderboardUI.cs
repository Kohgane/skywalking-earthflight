using UnityEngine;
using UnityEngine.UI;
using SWEF.Social;

namespace SWEF.UI
{
    /// <summary>
    /// Displays the local leaderboard in a scrollable UI panel. Shows rank, date,
    /// max altitude, flight duration, max speed, and score for each entry.
    /// The panel can be toggled open and closed.
    /// </summary>
    public class LeaderboardUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LeaderboardManager leaderboard;
        [SerializeField] private GameObject panel;
        [SerializeField] private Transform entryContainer;
        [SerializeField] private GameObject entryPrefab;
        [SerializeField] private Button toggleButton;
        [SerializeField] private Text titleText;

        private bool _isVisible = false;

        private void Awake()
        {
            if (leaderboard == null)
                leaderboard = FindFirstObjectByType<LeaderboardManager>();

            if (leaderboard == null)
                Debug.LogWarning("[SWEF] LeaderboardUI: no LeaderboardManager found in scene.");

            if (toggleButton != null)
                toggleButton.onClick.AddListener(TogglePanel);

            // Ensure the panel starts hidden.
            if (panel != null)
                panel.SetActive(false);
        }

        /// <summary>Toggles the leaderboard panel open or closed.</summary>
        public void TogglePanel()
        {
            _isVisible = !_isVisible;
            if (panel != null)
                panel.SetActive(_isVisible);

            if (_isVisible)
                RefreshList();
        }

        /// <summary>Opens the leaderboard panel and refreshes the displayed list.</summary>
        public void Show()
        {
            _isVisible = true;
            if (panel != null)
                panel.SetActive(true);
            RefreshList();
        }

        /// <summary>Closes the leaderboard panel.</summary>
        public void Hide()
        {
            _isVisible = false;
            if (panel != null)
                panel.SetActive(false);
        }

        /// <summary>
        /// Destroys all current entry rows and rebuilds them from the latest
        /// leaderboard data, populating Rank, Date, Altitude, Duration, Speed and Score.
        /// </summary>
        private void RefreshList()
        {
            if (entryContainer == null || entryPrefab == null || leaderboard == null)
                return;

            // Clear existing rows.
            for (int i = entryContainer.childCount - 1; i >= 0; i--)
                Destroy(entryContainer.GetChild(i).gameObject);

            LeaderboardData data = leaderboard.GetLeaderboard();
            if (data == null)
                return;

            for (int i = 0; i < data.entries.Count; i++)
            {
                LeaderboardEntry entry = data.entries[i];
                GameObject row = Instantiate(entryPrefab, entryContainer);

                // Populate child Text components by name.
                SetChildText(row, "Rank", $"#{i + 1}");
                SetChildText(row, "Date", entry.date);
                SetChildText(row, "Altitude", $"{entry.maxAltitude:N0}m");
                SetChildText(row, "Duration", FormatDuration(entry.duration));
                SetChildText(row, "Speed", $"{entry.maxSpeed:N0} m/s");
                SetChildText(row, "Score", $"{entry.score:N0}");
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Finds a child <see cref="Text"/> component by the child GameObject name
        /// and sets its text. Fails silently if the child does not exist.
        /// </summary>
        private static void SetChildText(GameObject parent, string childName, string value)
        {
            Transform child = parent.transform.Find(childName);
            if (child == null)
                return;

            Text label = child.GetComponent<Text>();
            if (label != null)
                label.text = value;
        }

        /// <summary>Converts a duration in seconds to a mm:ss string.</summary>
        private static string FormatDuration(float seconds)
        {
            int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return $"{total / 60:D2}:{total % 60:D2}";
        }
    }
}
