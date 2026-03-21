using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SWEF.SocialHub
{
    /// <summary>
    /// UI panel that displays the player's friend list and pending friend requests.
    /// Instantiates <see cref="ProfileCardUI"/> prefabs inside scroll-view content
    /// containers for friends and incoming/outgoing requests.
    /// Subscribes to <see cref="FriendManager"/> and <see cref="PlayerProfileManager"/>
    /// events to keep the display up to date.
    /// Call <see cref="Open"/> / <see cref="Close"/> to show or hide the panel.
    /// </summary>
    public class FriendListUI : MonoBehaviour
    {
        // ── Inspector — Root ──────────────────────────────────────────────────────
        [Header("Root")]
        [SerializeField] private GameObject rootPanel;

        // ── Inspector — Friends tab ───────────────────────────────────────────────
        [Header("Friends List")]
        [Tooltip("Scroll-view content transform where friend cards are spawned.")]
        [SerializeField] private RectTransform friendsContainer;

        [Tooltip("Prefab containing a ProfileCardUI component.")]
        [SerializeField] private GameObject profileCardPrefab;

        [SerializeField] private TextMeshProUGUI friendCountLabel;
        [SerializeField] private TextMeshProUGUI emptyFriendsLabel;

        // ── Inspector — Pending requests tab ──────────────────────────────────────
        [Header("Pending Requests")]
        [SerializeField] private RectTransform incomingContainer;
        [SerializeField] private RectTransform outgoingContainer;
        [SerializeField] private GameObject pendingSection;
        [SerializeField] private TextMeshProUGUI incomingCountLabel;

        // ── Inspector — Tab buttons ───────────────────────────────────────────────
        [Header("Tabs")]
        [SerializeField] private Button tabFriendsButton;
        [SerializeField] private Button tabPendingButton;
        [SerializeField] private GameObject friendsTab;
        [SerializeField] private GameObject pendingTab;

        // ── Inspector — Add friend ────────────────────────────────────────────────
        [Header("Add Friend")]
        [SerializeField] private TMP_InputField addFriendIdInput;
        [SerializeField] private TMP_InputField addFriendNameInput;
        [SerializeField] private Button addFriendButton;

        // ── State ─────────────────────────────────────────────────────────────────
        private readonly List<GameObject> _spawnedCards = new List<GameObject>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            // Tab wiring
            if (tabFriendsButton != null)
                tabFriendsButton.onClick.AddListener(ShowFriendsTab);
            if (tabPendingButton != null)
                tabPendingButton.onClick.AddListener(ShowPendingTab);

            // Add-friend button
            if (addFriendButton != null)
                addFriendButton.onClick.AddListener(OnAddFriendClicked);
        }

        private void OnEnable()
        {
            // Subscribe to updates.
            if (FriendManager.Instance != null)
                FriendManager.Instance.OnFriendListChanged += Refresh;
            if (PlayerProfileManager.Instance != null)
                PlayerProfileManager.Instance.OnRemoteProfileUpdated += _ => Refresh();

            Refresh();
        }

        private void OnDisable()
        {
            if (FriendManager.Instance != null)
                FriendManager.Instance.OnFriendListChanged -= Refresh;
            if (PlayerProfileManager.Instance != null)
                PlayerProfileManager.Instance.OnRemoteProfileUpdated -= _ => Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Shows the friend list panel.</summary>
        public void Open()
        {
            if (rootPanel != null) rootPanel.SetActive(true);
            ShowFriendsTab();
            Refresh();
        }

        /// <summary>Hides the friend list panel.</summary>
        public void Close()
        {
            if (rootPanel != null) rootPanel.SetActive(false);
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private void ShowFriendsTab()
        {
            SetActive(friendsTab, true);
            SetActive(pendingTab, false);
        }

        private void ShowPendingTab()
        {
            SetActive(friendsTab, false);
            SetActive(pendingTab, true);
        }

        /// <summary>Rebuilds all displayed cards from the current FriendManager state.</summary>
        public void Refresh()
        {
            ClearCards();
            if (FriendManager.Instance == null) return;

            // ── Confirmed friends ────────────────────────────────────────────
            var friends = FriendManager.Instance.GetFriends();
            if (friendCountLabel != null)
                friendCountLabel.text = friends.Count.ToString();
            if (emptyFriendsLabel != null)
                emptyFriendsLabel.gameObject.SetActive(friends.Count == 0);

            foreach (var entry in friends)
            {
                var card = SpawnCard(friendsContainer, entry);
                if (card != null)
                    card.SetActionButton("Remove", () => FriendManager.Instance.RemoveFriend(entry.playerId));
            }

            // ── Incoming requests ────────────────────────────────────────────
            var incoming = FriendManager.Instance.GetPendingIncoming();
            if (incomingCountLabel != null)
                incomingCountLabel.text = incoming.Count.ToString();

            foreach (var entry in incoming)
            {
                var card = SpawnCard(incomingContainer, entry);
                if (card != null)
                    card.SetActionButton("Accept", () => FriendManager.Instance.AcceptFriendRequest(entry.playerId));
            }

            // ── Outgoing requests ────────────────────────────────────────────
            var outgoing = FriendManager.Instance.GetPendingOutgoing();
            foreach (var entry in outgoing)
            {
                var card = SpawnCard(outgoingContainer, entry);
                if (card != null)
                    card.SetActionButton("Cancel", () => FriendManager.Instance.RemoveFriend(entry.playerId));
            }

            // Show/hide pending section based on activity
            if (pendingSection != null)
                pendingSection.SetActive(incoming.Count > 0 || outgoing.Count > 0);
        }

        private ProfileCardUI SpawnCard(RectTransform container, FriendEntry entry)
        {
            if (profileCardPrefab == null || container == null) return null;

            var go = Instantiate(profileCardPrefab, container);
            _spawnedCards.Add(go);

            var cardUI = go.GetComponent<ProfileCardUI>();
            if (cardUI == null) return null;

            // Try to resolve a full PlayerProfile from cache, fall back to stub.
            PlayerProfile profile = PlayerProfileManager.Instance != null
                ? PlayerProfileManager.Instance.GetRemoteProfile(entry.playerId)
                : null;

            if (profile == null)
            {
                profile = new PlayerProfile
                {
                    playerId    = entry.playerId,
                    displayName = entry.displayName
                };
            }

            cardUI.Bind(profile);
            return cardUI;
        }

        private void ClearCards()
        {
            foreach (var go in _spawnedCards)
                if (go != null) Destroy(go);
            _spawnedCards.Clear();
        }

        private void OnAddFriendClicked()
        {
            if (FriendManager.Instance == null) return;
            string id   = addFriendIdInput  != null ? addFriendIdInput.text.Trim()   : string.Empty;
            string name = addFriendNameInput != null ? addFriendNameInput.text.Trim() : string.Empty;

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("[SWEF] FriendListUI: Player ID is required to send a friend request.");
                return;
            }

            FriendManager.Instance.SendFriendRequest(id, name);

            if (addFriendIdInput   != null) addFriendIdInput.text   = string.Empty;
            if (addFriendNameInput != null) addFriendNameInput.text = string.Empty;
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }
    }
}
