using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SWEF.Progression;

namespace SWEF.SocialHub
{
    /// <summary>
    /// UI panel that lets the player customise their public profile:
    /// display name, avatar, equipped title, and preferred cosmetics.
    /// Reads available options from <see cref="CosmeticUnlockManager"/> and writes
    /// changes back via <see cref="PlayerProfileManager"/> and
    /// <see cref="CosmeticUnlockManager"/>.
    /// Call <see cref="Open"/> / <see cref="Close"/> to show or hide the panel.
    /// </summary>
    public class ProfileCustomizationUI : MonoBehaviour
    {
        // ── Inspector — Root ──────────────────────────────────────────────────────
        [Header("Root")]
        [SerializeField] private GameObject rootPanel;

        // ── Inspector — Display name ──────────────────────────────────────────────
        [Header("Display Name")]
        [SerializeField] private TMP_InputField displayNameInput;
        [SerializeField] private Button saveDisplayNameButton;
        [SerializeField] private TextMeshProUGUI displayNameFeedback;

        // ── Inspector — Avatar selection ──────────────────────────────────────────
        [Header("Avatar")]
        [Tooltip("Parent transform where avatar option buttons are spawned.")]
        [SerializeField] private RectTransform avatarContainer;
        [Tooltip("Prefab for a single avatar selection button (needs Button + Image + child TextMeshProUGUI).")]
        [SerializeField] private GameObject avatarOptionPrefab;
        [SerializeField] private Image selectedAvatarPreview;

        // ── Inspector — Title (NameTag) ───────────────────────────────────────────
        [Header("Title / NameTag")]
        [Tooltip("Parent transform where title option buttons are spawned.")]
        [SerializeField] private RectTransform titleContainer;
        [Tooltip("Prefab for a single title selection button (needs Button + TextMeshProUGUI).")]
        [SerializeField] private GameObject titleOptionPrefab;
        [SerializeField] private TextMeshProUGUI equippedTitleLabel;

        // ── Inspector — Profile preview ───────────────────────────────────────────
        [Header("Profile Preview")]
        [Tooltip("ProfileCardUI component for the live preview card.")]
        [SerializeField] private ProfileCardUI previewCard;

        // ── Inspector — Save / cancel ─────────────────────────────────────────────
        [Header("Actions")]
        [SerializeField] private Button saveButton;
        [SerializeField] private Button cancelButton;

        // ── State ─────────────────────────────────────────────────────────────────
        private readonly List<GameObject> _spawnedAvatarOptions = new List<GameObject>();
        private readonly List<GameObject> _spawnedTitleOptions  = new List<GameObject>();
        private string _pendingAvatarId;
        private string _pendingTitleId;

        // Hardcoded built-in avatar ids available to all players.
        private static readonly string[] BuiltinAvatarIds = {
            "avatar_default", "avatar_ace", "avatar_rookie",
            "avatar_veteran", "avatar_eagle", "avatar_hawk"
        };

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (saveDisplayNameButton != null)
                saveDisplayNameButton.onClick.AddListener(OnSaveDisplayName);
            if (saveButton   != null) saveButton.onClick.AddListener(OnSave);
            if (cancelButton != null) cancelButton.onClick.AddListener(Close);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Opens the customization panel and pre-fills current values.</summary>
        public void Open()
        {
            if (rootPanel != null) rootPanel.SetActive(true);
            Populate();
        }

        /// <summary>Closes the customization panel without saving pending changes.</summary>
        public void Close()
        {
            if (rootPanel != null) rootPanel.SetActive(false);
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private void Populate()
        {
            var profileMgr = PlayerProfileManager.Instance;
            if (profileMgr == null) return;

            var profile = profileMgr.GetLocalProfile();

            // Display name
            if (displayNameInput != null)
                displayNameInput.text = profile.displayName;
            if (displayNameFeedback != null)
                displayNameFeedback.text = string.Empty;

            _pendingAvatarId = profile.avatarId;
            _pendingTitleId  = profile.titleId;

            BuildAvatarOptions(profile.avatarId);
            BuildTitleOptions(profile.titleId);
            RefreshPreview();
        }

        private void BuildAvatarOptions(string currentAvatarId)
        {
            foreach (var go in _spawnedAvatarOptions) if (go != null) Destroy(go);
            _spawnedAvatarOptions.Clear();

            if (avatarContainer == null || avatarOptionPrefab == null) return;

            foreach (string id in BuiltinAvatarIds)
            {
                string capturedId = id;
                var go = Instantiate(avatarOptionPrefab, avatarContainer);
                _spawnedAvatarOptions.Add(go);

                // Load sprite
                var img = go.GetComponentInChildren<Image>();
                if (img != null)
                {
                    Sprite sp = Resources.Load<Sprite>($"Avatars/{capturedId}");
                    if (sp != null) img.sprite = sp;
                }

                // Label
                var label = go.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = capturedId.Replace("avatar_", string.Empty).ToUpper();

                // Click
                var btn = go.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(() => SelectAvatar(capturedId));
            }
        }

        private void BuildTitleOptions(string currentTitleId)
        {
            foreach (var go in _spawnedTitleOptions) if (go != null) Destroy(go);
            _spawnedTitleOptions.Clear();

            if (titleContainer == null || titleOptionPrefab == null) return;

            var cosmeticMgr = CosmeticUnlockManager.Instance;
            if (cosmeticMgr == null) return;

            foreach (var item in cosmeticMgr.GetUnlockedCosmetics())
            {
                if (item.category != CosmeticCategory.NameTag) continue;

                string capturedId = item.id;
                var go = Instantiate(titleOptionPrefab, titleContainer);
                _spawnedTitleOptions.Add(go);

                var label = go.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = item.nameKey;

                var btn = go.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(() => SelectTitle(capturedId));
            }
        }

        private void SelectAvatar(string avatarId)
        {
            _pendingAvatarId = avatarId;

            if (selectedAvatarPreview != null)
            {
                Sprite sp = Resources.Load<Sprite>($"Avatars/{avatarId}");
                if (sp != null) selectedAvatarPreview.sprite = sp;
            }

            RefreshPreview();
        }

        private void SelectTitle(string titleId)
        {
            _pendingTitleId = titleId;

            if (equippedTitleLabel != null)
                equippedTitleLabel.text = titleId;

            RefreshPreview();
        }

        private void RefreshPreview()
        {
            if (previewCard == null || PlayerProfileManager.Instance == null) return;

            var profile = PlayerProfileManager.Instance.GetLocalProfile();
            // Apply pending changes to preview copy (non-destructive)
            var preview = new PlayerProfile
            {
                playerId              = profile.playerId,
                displayName           = profile.displayName,
                avatarId              = _pendingAvatarId ?? profile.avatarId,
                titleId               = _pendingTitleId  ?? profile.titleId,
                pilotRankLevel        = profile.pilotRankLevel,
                pilotRankName         = profile.pilotRankName,
                totalXP               = profile.totalXP,
                totalFlights          = profile.totalFlights,
                achievementsUnlocked  = profile.achievementsUnlocked,
                achievementsTotal     = profile.achievementsTotal,
                dailyStreak           = profile.dailyStreak,
                seasonTier            = profile.seasonTier,
                isPremium             = profile.isPremium
            };
            previewCard.Bind(preview);
        }

        private void OnSaveDisplayName()
        {
            if (PlayerProfileManager.Instance == null) return;
            string name = displayNameInput != null ? displayNameInput.text : string.Empty;
            bool ok = PlayerProfileManager.Instance.SetDisplayName(name);
            if (displayNameFeedback != null)
                displayNameFeedback.text = ok ? "Saved!" : "Name must be 2–20 characters.";
            if (ok) RefreshPreview();
        }

        private void OnSave()
        {
            var profileMgr = PlayerProfileManager.Instance;
            if (profileMgr == null) { Close(); return; }

            // Apply avatar
            profileMgr.SetAvatarId(_pendingAvatarId);

            // Apply title via CosmeticUnlockManager equip
            if (CosmeticUnlockManager.Instance != null && !string.IsNullOrEmpty(_pendingTitleId))
                CosmeticUnlockManager.Instance.EquipCosmetic(_pendingTitleId, CosmeticCategory.NameTag);

            Close();
        }
    }
}
