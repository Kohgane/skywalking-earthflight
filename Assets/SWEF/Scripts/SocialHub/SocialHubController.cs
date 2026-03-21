using System;
using UnityEngine;

namespace SWEF.SocialHub
{
    // ── Enums ────────────────────────────────────────────────────────────────────

    /// <summary>Identifies the top-level panel shown inside the Social Hub overlay.</summary>
    public enum SocialHubPanel
    {
        /// <summary>The local player's own profile.</summary>
        MyProfile,
        /// <summary>The friend list and pending requests.</summary>
        Friends,
        /// <summary>The social activity feed.</summary>
        ActivityFeed,
        /// <summary>Player search / discovery panel.</summary>
        PlayerSearch,
        /// <summary>Profile customization panel.</summary>
        Customization
    }

    /// <summary>
    /// Central MonoBehaviour controller for the Social Hub overlay.
    /// Manages which panel is currently visible, routes open/close calls to sub-UIs,
    /// and exposes a single entry-point (<see cref="Open"/> / <see cref="Close"/>) for
    /// other systems.
    /// Survives scene loads via <c>DontDestroyOnLoad</c>.
    /// </summary>
    public class SocialHubController : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static SocialHubController Instance { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when the Social Hub is opened. Parameter: the initial panel shown.</summary>
        public event Action<SocialHubPanel> OnHubOpened;

        /// <summary>Fired when the Social Hub is closed.</summary>
        public event Action OnHubClosed;

        /// <summary>Fired when the active panel changes.</summary>
        public event Action<SocialHubPanel> OnPanelChanged;

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Sub-UI References")]
        [Tooltip("Root panel — shown/hidden with the entire hub.")]
        [SerializeField] private GameObject hubRootPanel;

        [Tooltip("Panel shown for the local player's own profile.")]
        [SerializeField] private GameObject myProfilePanel;

        [Tooltip("Panel for the friend list and pending requests.")]
        [SerializeField] private GameObject friendsPanel;

        [Tooltip("Panel for the social activity feed.")]
        [SerializeField] private GameObject activityFeedPanel;

        [Tooltip("Panel for searching / discovering other players.")]
        [SerializeField] private GameObject playerSearchPanel;

        [Tooltip("Panel for profile customization (avatar, title, cosmetics).")]
        [SerializeField] private GameObject customizationPanel;

        // ── State ─────────────────────────────────────────────────────────────────
        /// <summary>Whether the Social Hub overlay is currently visible.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>The panel currently shown inside the hub.</summary>
        public SocialHubPanel ActivePanel { get; private set; }

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

            // Ensure the hub starts closed.
            SetHubRootVisible(false);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Opens the Social Hub overlay, optionally jumping directly to a specific panel.
        /// </summary>
        /// <param name="panel">Panel to show on open (defaults to <see cref="SocialHubPanel.MyProfile"/>).</param>
        public void Open(SocialHubPanel panel = SocialHubPanel.MyProfile)
        {
            if (IsOpen && ActivePanel == panel) return;

            IsOpen = true;
            SetHubRootVisible(true);
            ShowPanel(panel);
            OnHubOpened?.Invoke(panel);
            Debug.Log($"[SWEF] SocialHubController: Opened (panel={panel}).");
        }

        /// <summary>Closes the Social Hub overlay.</summary>
        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            SetHubRootVisible(false);
            OnHubClosed?.Invoke();
            Debug.Log("[SWEF] SocialHubController: Closed.");
        }

        /// <summary>Toggles the Social Hub overlay open/closed.</summary>
        public void Toggle(SocialHubPanel panel = SocialHubPanel.MyProfile)
        {
            if (IsOpen)
                Close();
            else
                Open(panel);
        }

        /// <summary>
        /// Switches the visible panel inside an already-open hub.
        /// Calls <see cref="Open"/> if the hub is closed.
        /// </summary>
        public void ShowPanel(SocialHubPanel panel)
        {
            if (!IsOpen)
            {
                Open(panel);
                return;
            }

            ActivePanel = panel;
            SetPanelActive(myProfilePanel,      panel == SocialHubPanel.MyProfile);
            SetPanelActive(friendsPanel,         panel == SocialHubPanel.Friends);
            SetPanelActive(activityFeedPanel,    panel == SocialHubPanel.ActivityFeed);
            SetPanelActive(playerSearchPanel,    panel == SocialHubPanel.PlayerSearch);
            SetPanelActive(customizationPanel,   panel == SocialHubPanel.Customization);

            OnPanelChanged?.Invoke(panel);
        }

        // ── Convenience shortcuts (for UI button wiring) ──────────────────────────

        /// <summary>Shows the My Profile panel.</summary>
        public void ShowMyProfile()     => ShowPanel(SocialHubPanel.MyProfile);
        /// <summary>Shows the Friends panel.</summary>
        public void ShowFriends()       => ShowPanel(SocialHubPanel.Friends);
        /// <summary>Shows the Activity Feed panel.</summary>
        public void ShowActivityFeed()  => ShowPanel(SocialHubPanel.ActivityFeed);
        /// <summary>Shows the Player Search panel.</summary>
        public void ShowPlayerSearch()  => ShowPanel(SocialHubPanel.PlayerSearch);
        /// <summary>Shows the Customization panel.</summary>
        public void ShowCustomization() => ShowPanel(SocialHubPanel.Customization);

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void SetHubRootVisible(bool visible)
        {
            if (hubRootPanel != null)
                hubRootPanel.SetActive(visible);
        }

        private static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
                panel.SetActive(active);
        }
    }
}
