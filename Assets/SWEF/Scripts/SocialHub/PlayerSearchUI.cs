using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SWEF.SocialHub
{
    /// <summary>
    /// UI panel for searching and discovering other players by display name.
    /// Searches through <see cref="PlayerProfileManager"/>'s cached remote profiles
    /// and the local player's friend list from <see cref="FriendManager"/>.
    /// Displays results as <see cref="ProfileCardUI"/> cards with add-friend actions.
    /// Call <see cref="Open"/> / <see cref="Close"/> to show or hide the panel.
    /// </summary>
    public class PlayerSearchUI : MonoBehaviour
    {
        // ── Inspector — Root ──────────────────────────────────────────────────────
        [Header("Root")]
        [SerializeField] private GameObject rootPanel;

        // ── Inspector — Search input ──────────────────────────────────────────────
        [Header("Search")]
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private Button searchButton;
        [SerializeField] private Button clearButton;

        // ── Inspector — Results ───────────────────────────────────────────────────
        [Header("Results")]
        [Tooltip("Scroll-view content transform where result cards are spawned.")]
        [SerializeField] private RectTransform resultsContainer;
        [Tooltip("ProfileCardUI prefab for each search result.")]
        [SerializeField] private GameObject profileCardPrefab;
        [SerializeField] private TextMeshProUGUI resultsCountLabel;
        [SerializeField] private TextMeshProUGUI noResultsLabel;
        [SerializeField] private TextMeshProUGUI searchStatusLabel;

        // ── State ─────────────────────────────────────────────────────────────────
        private readonly List<GameObject> _spawnedCards = new List<GameObject>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (searchButton != null)
                searchButton.onClick.AddListener(OnSearch);
            if (clearButton != null)
                clearButton.onClick.AddListener(OnClear);
            if (searchInput != null)
                searchInput.onSubmit.AddListener(_ => OnSearch());
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Shows the player search panel.</summary>
        public void Open()
        {
            if (rootPanel != null) rootPanel.SetActive(true);
            OnClear();
        }

        /// <summary>Hides the player search panel.</summary>
        public void Close()
        {
            if (rootPanel != null) rootPanel.SetActive(false);
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private void OnSearch()
        {
            string query = searchInput != null ? searchInput.text.Trim().ToLowerInvariant() : string.Empty;

            if (string.IsNullOrEmpty(query))
            {
                SetStatus("Enter a player name to search.");
                return;
            }

            var results = Search(query);
            ShowResults(results);
        }

        private void OnClear()
        {
            if (searchInput != null) searchInput.text = string.Empty;
            ClearCards();
            if (resultsCountLabel != null) resultsCountLabel.text = string.Empty;
            SetStatus(string.Empty);
            if (noResultsLabel != null) noResultsLabel.gameObject.SetActive(false);
        }

        /// <summary>
        /// Searches cached remote profiles for display names matching <paramref name="query"/>.
        /// Excludes the local player's own profile.
        /// </summary>
        private static List<PlayerProfile> Search(string query)
        {
            var results = new List<PlayerProfile>();
            string localId = PlayerPrefs.GetString("SWEF_PlayerId", string.Empty);

            if (PlayerProfileManager.Instance != null)
            {
                foreach (var profile in PlayerProfileManager.Instance.GetAllRemoteProfiles())
                {
                    if (profile.playerId == localId) continue;
                    if (profile.displayName.ToLowerInvariant().Contains(query))
                        results.Add(profile);
                }
            }

            return results;
        }

        private void ShowResults(List<PlayerProfile> results)
        {
            ClearCards();

            bool hasResults = results.Count > 0;
            if (resultsCountLabel != null)
                resultsCountLabel.text = hasResults ? $"{results.Count} player(s) found" : string.Empty;
            if (noResultsLabel != null)
                noResultsLabel.gameObject.SetActive(!hasResults);

            if (!hasResults)
            {
                SetStatus("No players found.");
                return;
            }

            SetStatus(string.Empty);
            foreach (var profile in results)
                SpawnResultCard(profile);
        }

        private void SpawnResultCard(PlayerProfile profile)
        {
            if (profileCardPrefab == null || resultsContainer == null) return;

            var go = Instantiate(profileCardPrefab, resultsContainer);
            _spawnedCards.Add(go);

            var card = go.GetComponent<ProfileCardUI>();
            if (card == null) return;

            card.Bind(profile);

            // Determine action button based on current friend status.
            if (FriendManager.Instance != null)
            {
                FriendStatus status = FriendManager.Instance.GetStatus(profile.playerId);
                string capturedId   = profile.playerId;
                string capturedName = profile.displayName;

                switch (status)
                {
                    case FriendStatus.Friend:
                        card.SetActionButton("Friends ✓", null); // no action
                        break;
                    case FriendStatus.RequestSent:
                        card.SetActionButton("Request Sent", null);
                        break;
                    case FriendStatus.RequestReceived:
                        card.SetActionButton("Accept Request",
                            () => FriendManager.Instance.AcceptFriendRequest(capturedId));
                        break;
                    default:
                        card.SetActionButton("Add Friend",
                            () =>
                            {
                                FriendManager.Instance.SendFriendRequest(capturedId, capturedName);
                                // Update button label immediately.
                                card.SetActionButton("Request Sent", null);
                            });
                        break;
                }
            }
        }

        private void ClearCards()
        {
            foreach (var go in _spawnedCards)
                if (go != null) Destroy(go);
            _spawnedCards.Clear();
        }

        private void SetStatus(string message)
        {
            if (searchStatusLabel != null)
                searchStatusLabel.text = message;
        }
    }
}
