using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.TimeCapsule
{
    /// <summary>
    /// UI controller for the Time Capsule &amp; Memory system.
    /// Manages the capsule list panel, detail panel, and creation panel.
    /// Bridges player input to <see cref="TimeCapsuleManager"/>.
    /// </summary>
    public class TimeCapsuleUI : MonoBehaviour
    {
        // ── Inspector — Panels ────────────────────────────────────────────────────
        [Header("Panels")]
        [Tooltip("Root panel that displays the scrollable list of all capsules.")]
        [SerializeField] private GameObject capsuleListPanel;

        [Tooltip("Root panel that shows the full detail view of a single capsule.")]
        [SerializeField] private GameObject capsuleDetailPanel;

        [Tooltip("Root panel that presents the capsule creation form.")]
        [SerializeField] private GameObject createPanel;

        // ── Inspector — List Panel ────────────────────────────────────────────────
        [Header("List Panel")]
        [Tooltip("Prefab instantiated for each capsule entry in the list.")]
        [SerializeField] private GameObject capsuleListItemPrefab;

        [Tooltip("Scroll view content transform where list items are parented.")]
        [SerializeField] private Transform capsuleListContent;

        [Tooltip("Badge text element showing the count of capsules ready to open.")]
        [SerializeField] private Text notificationBadgeText;

        [Tooltip("Badge root that is hidden when there are no ready capsules.")]
        [SerializeField] private GameObject notificationBadgeRoot;

        // ── Inspector — Detail Panel ──────────────────────────────────────────────
        [Header("Detail Panel")]
        [Tooltip("Text element showing the capsule title.")]
        [SerializeField] private Text detailTitleText;

        [Tooltip("Text element showing the capsule description and personal note.")]
        [SerializeField] private Text detailDescriptionText;

        [Tooltip("Text element showing the capsule age and creation date.")]
        [SerializeField] private Text detailAgeText;

        [Tooltip("Text element showing the capsule location name.")]
        [SerializeField] private Text detailLocationText;

        [Tooltip("Image element used to display the capsule screenshot preview.")]
        [SerializeField] private RawImage detailScreenshotImage;

        [Tooltip("Open button shown when a sealed capsule is ready.")]
        [SerializeField] private Button openButton;

        [Tooltip("Share button.")]
        [SerializeField] private Button shareButton;

        // ── Inspector — Create Panel ──────────────────────────────────────────────
        [Header("Create Panel")]
        [Tooltip("Input field for the capsule title.")]
        [SerializeField] private InputField createTitleInput;

        [Tooltip("Input field for the capsule description.")]
        [SerializeField] private InputField createDescriptionInput;

        [Tooltip("Input field for the personal note.")]
        [SerializeField] private InputField createNoteInput;

        [Tooltip("Input field for the number of days to delay opening (0 = immediately).")]
        [SerializeField] private InputField createDelayDaysInput;

        [Tooltip("Dropdown used to select the capsule type.")]
        [SerializeField] private Dropdown createTypeDropdown;

        // ── Internal state ────────────────────────────────────────────────────────
        private string _currentDetailCapsuleId;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void OnEnable()
        {
            if (TimeCapsuleManager.Instance != null)
            {
                TimeCapsuleManager.Instance.OnCapsuleCreated += OnCapsuleCreated;
                TimeCapsuleManager.Instance.OnCapsuleOpened  += OnCapsuleOpened;
                TimeCapsuleManager.Instance.OnCapsuleShared  += OnCapsuleShared;
            }
            RefreshUI();
        }

        private void OnDisable()
        {
            if (TimeCapsuleManager.Instance != null)
            {
                TimeCapsuleManager.Instance.OnCapsuleCreated -= OnCapsuleCreated;
                TimeCapsuleManager.Instance.OnCapsuleOpened  -= OnCapsuleOpened;
                TimeCapsuleManager.Instance.OnCapsuleShared  -= OnCapsuleShared;
            }
        }

        // ── Panel navigation ──────────────────────────────────────────────────────

        /// <summary>
        /// Shows the scrollable list of all capsules, grouped by
        /// <see cref="TimeCapsuleStatus"/>, and hides other panels.
        /// </summary>
        public void ShowCapsuleList()
        {
            SetPanelActive(capsuleListPanel,  true);
            SetPanelActive(capsuleDetailPanel, false);
            SetPanelActive(createPanel,        false);
            RefreshUI();
        }

        /// <summary>
        /// Shows the detail view for the capsule identified by <paramref name="capsuleId"/>.
        /// Loads and displays the screenshot preview if a path is available.
        /// </summary>
        /// <param name="capsuleId">GUID of the capsule to display.</param>
        public void ShowCapsuleDetail(string capsuleId)
        {
            if (TimeCapsuleManager.Instance == null) return;

            var capsule = FindCapsule(capsuleId);
            if (capsule == null) return;

            _currentDetailCapsuleId = capsuleId;

            if (detailTitleText)       detailTitleText.text       = capsule.title;
            if (detailDescriptionText) detailDescriptionText.text = BuildDetailText(capsule);
            if (detailAgeText)         detailAgeText.text         = capsule.FormattedAge();
            if (detailLocationText)    detailLocationText.text    = capsule.location?.locationName ?? string.Empty;

            LoadScreenshot(capsule.screenshotPath);

            bool canOpen  = capsule.status == TimeCapsuleStatus.Sealed && capsule.IsReadyToOpen();
            bool canShare = capsule.status != TimeCapsuleStatus.Shared;

            if (openButton)
            {
                openButton.gameObject.SetActive(canOpen);
                openButton.onClick.RemoveAllListeners();
                string openId = capsuleId;
                openButton.onClick.AddListener(() => OnOpenButtonClicked(openId));
            }
            if (shareButton)
            {
                shareButton.gameObject.SetActive(canShare);
                shareButton.onClick.RemoveAllListeners();
                string shareId = capsuleId;
                shareButton.onClick.AddListener(() => OnShareButtonClicked(shareId));
            }

            SetPanelActive(capsuleListPanel,  false);
            SetPanelActive(capsuleDetailPanel, true);
            SetPanelActive(createPanel,        false);
        }

        /// <summary>Opens the capsule creation form panel.</summary>
        public void ShowCreatePanel()
        {
            SetPanelActive(capsuleListPanel,  false);
            SetPanelActive(capsuleDetailPanel, false);
            SetPanelActive(createPanel,        true);

            // Clear the form.
            if (createTitleInput)      createTitleInput.text      = string.Empty;
            if (createDescriptionInput) createDescriptionInput.text = string.Empty;
            if (createNoteInput)       createNoteInput.text       = string.Empty;
            if (createDelayDaysInput)  createDelayDaysInput.text  = "0";
        }

        // ── Button handlers ───────────────────────────────────────────────────────

        /// <summary>
        /// Called when the player confirms the creation form.
        /// Gathers all form fields and delegates to <see cref="TimeCapsuleManager.CreateCapsule"/>.
        /// </summary>
        public void OnCreateButtonClicked()
        {
            if (TimeCapsuleManager.Instance == null) return;

            string titleVal   = createTitleInput       ? createTitleInput.text       : "Capsule";
            string descVal    = createDescriptionInput ? createDescriptionInput.text : string.Empty;
            string noteVal    = createNoteInput        ? createNoteInput.text        : string.Empty;
            float  delayDays  = 0f;
            if (createDelayDaysInput && float.TryParse(createDelayDaysInput.text, out float parsed))
                delayDays = Mathf.Max(0f, parsed);

            TimeCapsuleType capsuleType = TimeCapsuleType.PersonalNote;
            if (createTypeDropdown)
                capsuleType = (TimeCapsuleType)createTypeDropdown.value;

            TimeCapsuleManager.Instance.CreateCapsule(
                title:          titleVal,
                description:    descVal,
                type:           capsuleType,
                location:       new CapsuleLocation(),
                weather:        new CapsuleWeatherSnapshot(),
                flight:         new CapsuleFlightSnapshot(),
                screenshotPath: string.Empty,
                tags:           new System.Collections.Generic.List<string>(),
                personalNote:   noteVal,
                delayDays:      delayDays);

            ShowCapsuleList();
        }

        /// <summary>
        /// Called when the player taps the Open button on a capsule detail view.
        /// </summary>
        /// <param name="capsuleId">GUID of the capsule to open.</param>
        public void OnOpenButtonClicked(string capsuleId)
        {
            if (TimeCapsuleManager.Instance == null) return;
            TimeCapsuleManager.Instance.OpenCapsule(capsuleId);
        }

        /// <summary>
        /// Called when the player taps the Share button on a capsule detail view.
        /// </summary>
        /// <param name="capsuleId">GUID of the capsule to share.</param>
        public void OnShareButtonClicked(string capsuleId)
        {
            if (TimeCapsuleManager.Instance == null) return;
            TimeCapsuleManager.Instance.ShareCapsule(capsuleId);
        }

        // ── Refresh ───────────────────────────────────────────────────────────────

        /// <summary>Rebuilds the capsule list display from the current manager state.</summary>
        public void RefreshUI()
        {
            RebuildList();
            UpdateNotificationBadge();
        }

        /// <summary>
        /// Updates the notification badge with the count of capsules that are ready to open.
        /// Hides the badge when the count is zero.
        /// </summary>
        public void UpdateNotificationBadge()
        {
            if (TimeCapsuleManager.Instance == null) return;

            int count = TimeCapsuleManager.Instance.GetReadyCapsules().Count;
            bool show = count > 0;

            if (notificationBadgeRoot) notificationBadgeRoot.SetActive(show);
            if (notificationBadgeText) notificationBadgeText.text = count.ToString();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void RebuildList()
        {
            if (capsuleListContent == null || capsuleListItemPrefab == null) return;
            if (TimeCapsuleManager.Instance == null) return;

            // Clear existing items.
            foreach (Transform child in capsuleListContent)
                Destroy(child.gameObject);

            // Instantiate one item per capsule.
            foreach (var capsule in TimeCapsuleManager.Instance.Capsules)
            {
                var item = Instantiate(capsuleListItemPrefab, capsuleListContent);
                // Attempt to bind the capsule ID to any Button on the item.
                var btn = item.GetComponentInChildren<Button>();
                if (btn != null)
                {
                    string id = capsule.capsuleId;
                    btn.onClick.AddListener(() => ShowCapsuleDetail(id));
                }
                // Set title text on any Text component in the item.
                var label = item.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = $"{capsule.title}  [{capsule.status}]";
            }
        }

        private void LoadScreenshot(string path)
        {
            if (detailScreenshotImage == null) return;
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                detailScreenshotImage.texture = null;
                detailScreenshotImage.gameObject.SetActive(false);
                return;
            }
            byte[] data = System.IO.File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2);
            if (tex.LoadImage(data))
            {
                detailScreenshotImage.texture = tex;
                detailScreenshotImage.gameObject.SetActive(true);
            }
        }

        private string BuildDetailText(TimeCapsule capsule)
        {
            var sb = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(capsule.description))
                sb.AppendLine(capsule.description);
            if (!string.IsNullOrEmpty(capsule.personalNote))
            {
                sb.AppendLine();
                sb.AppendLine("Note:");
                sb.AppendLine(capsule.personalNote);
            }
            return sb.ToString().TrimEnd();
        }

        private TimeCapsule FindCapsule(string capsuleId)
        {
            if (TimeCapsuleManager.Instance == null) return null;
            foreach (var c in TimeCapsuleManager.Instance.Capsules)
                if (c.capsuleId == capsuleId) return c;
            return null;
        }

        private static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel) panel.SetActive(active);
        }

        // ── Manager event callbacks ───────────────────────────────────────────────

        private void OnCapsuleCreated(TimeCapsule capsule) => RefreshUI();
        private void OnCapsuleOpened(TimeCapsule capsule)  => ShowCapsuleDetail(capsule.capsuleId);
        private void OnCapsuleShared(TimeCapsule capsule)  => RefreshUI();
    }
}
