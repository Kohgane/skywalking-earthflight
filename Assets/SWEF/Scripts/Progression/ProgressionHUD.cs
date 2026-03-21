using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SWEF.Progression
{
    /// <summary>
    /// Always-visible HUD overlay for pilot progression.
    /// Displays the current rank badge, rank name, animated XP fill bar, and level number.
    /// Shows floating "+XP" popups on XP gain and triggers a full rank-up celebration.
    /// Attach to a Canvas GameObject that persists across scenes.
    /// </summary>
    public class ProgressionHUD : MonoBehaviour
    {
        // ── Inspector — Rank display ───────────────────────────────────────────────
        [Header("Rank Display")]
        [Tooltip("Image component used to display the current rank badge sprite.")]
        [SerializeField] private Image rankBadgeImage;

        [Tooltip("Text label showing the rank name.")]
        [SerializeField] private TextMeshProUGUI rankNameText;

        [Tooltip("Text label showing the numeric level (e.g. 'Lv.12').")]
        [SerializeField] private TextMeshProUGUI levelText;

        // ── Inspector — XP bar ────────────────────────────────────────────────────
        [Header("XP Bar")]
        [Tooltip("Slider or Image (Filled) representing XP progress toward next rank.")]
        [SerializeField] private Slider xpBarSlider;

        [Tooltip("Text label showing current XP / XP needed (optional).")]
        [SerializeField] private TextMeshProUGUI xpBarLabel;

        [Tooltip("Speed at which the XP bar animates toward its target value.")]
        [SerializeField] private float xpBarFillSpeed = 2f;

        // ── Inspector — XP popup ──────────────────────────────────────────────────
        [Header("XP Gain Popup")]
        [Tooltip("Prefab for the floating '+XP' text popup. Needs a TextMeshProUGUI and CanvasGroup.")]
        [SerializeField] private GameObject xpPopupPrefab;

        [Tooltip("Parent RectTransform where popups are spawned.")]
        [SerializeField] private RectTransform popupRoot;

        [Tooltip("How far (in UI units) the popup floats upward before fading.")]
        [SerializeField] private float popupFloatDistance = 80f;

        [Tooltip("Duration in seconds for the popup float + fade animation.")]
        [SerializeField] private float popupDuration = 1.6f;

        // ── Inspector — Rank-up celebration ───────────────────────────────────────
        [Header("Rank-Up Celebration")]
        [Tooltip("Full-screen flash panel (Image with alpha).")]
        [SerializeField] private Image flashPanel;

        [Tooltip("Root transform for the large badge celebration animation.")]
        [SerializeField] private RectTransform rankUpBadgeRoot;

        [Tooltip("Text label in the celebration showing the new rank name.")]
        [SerializeField] private TextMeshProUGUI rankUpNameText;

        [Tooltip("Optional AudioSource to trigger the rank-up sound.")]
        [SerializeField] private AudioSource rankUpAudioSource;

        // ── Internal state ────────────────────────────────────────────────────────
        private ProgressionManager _progression;
        private float _targetXPFill;
        private Coroutine _xpFillCoroutine;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Start()
        {
            _progression = ProgressionManager.Instance
                           ?? FindFirstObjectByType<ProgressionManager>();

            if (_progression != null)
            {
                _progression.OnXPGained  += HandleXPGained;
                _progression.OnRankUp    += HandleRankUp;
            }

            // Hide celebration elements initially
            if (flashPanel    != null) flashPanel.gameObject.SetActive(false);
            if (rankUpBadgeRoot != null) rankUpBadgeRoot.gameObject.SetActive(false);

            RefreshDisplay();
        }

        private void OnDestroy()
        {
            if (_progression != null)
            {
                _progression.OnXPGained -= HandleXPGained;
                _progression.OnRankUp   -= HandleRankUp;
            }
        }

        private void Update()
        {
            // Smoothly animate XP bar toward target fill
            if (xpBarSlider != null && !Mathf.Approximately(xpBarSlider.value, _targetXPFill))
                xpBarSlider.value = Mathf.MoveTowards(xpBarSlider.value, _targetXPFill, xpBarFillSpeed * Time.deltaTime);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Forces an immediate refresh of all HUD elements from the current progression state.</summary>
        public void RefreshDisplay()
        {
            if (_progression == null) return;

            var rank = _progression.GetCurrentRank();

            if (rankBadgeImage != null && rank != null)
            {
                rankBadgeImage.sprite = rank.rankIcon;
                rankBadgeImage.color  = rank.rankColor;
            }

            if (rankNameText != null && rank != null)
                rankNameText.text = rank.rankName;

            if (levelText != null)
                levelText.text = $"Lv.{_progression.CurrentRankLevel}";

            _targetXPFill = _progression.GetProgressToNextRank01();
            if (xpBarSlider != null)
                xpBarSlider.value = _targetXPFill;

            UpdateXPLabel();
        }

        // ── Private handlers ──────────────────────────────────────────────────────

        private void HandleXPGained(long amount, string source)
        {
            _targetXPFill = _progression.GetProgressToNextRank01();
            UpdateXPLabel();
            SpawnXPPopup(amount);
        }

        private void HandleRankUp(PilotRankData oldRank, PilotRankData newRank)
        {
            RefreshDisplay();
            StartCoroutine(PlayRankUpCelebration(newRank));
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void UpdateXPLabel()
        {
            if (_progression == null || xpBarLabel == null) return;
            long toNext = _progression.GetXPToNextRank();
            var  next   = _progression.GetNextRank();
            xpBarLabel.text = next != null
                ? $"{_progression.CurrentXP:N0} / {next.requiredXP:N0} XP"
                : "MAX RANK";
        }

        private void SpawnXPPopup(long amount)
        {
            if (xpPopupPrefab == null || popupRoot == null) return;

            var go = Instantiate(xpPopupPrefab, popupRoot);
            var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = $"+{amount} XP";

            StartCoroutine(AnimateXPPopup(go));
        }

        private IEnumerator AnimateXPPopup(GameObject popup)
        {
            var rt     = popup.GetComponent<RectTransform>() ?? popup.GetComponentInChildren<RectTransform>();
            var cg     = popup.GetComponent<CanvasGroup>()   ?? popup.GetComponentInChildren<CanvasGroup>();
            var startY = rt != null ? rt.anchoredPosition.y : 0f;
            float t = 0f;

            while (t < popupDuration)
            {
                t += Time.deltaTime;
                float p = t / popupDuration;
                if (rt  != null) rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, startY + popupFloatDistance * p);
                if (cg  != null) cg.alpha = Mathf.Clamp01(1f - (p - 0.5f) * 2f);
                yield return null;
            }

            Destroy(popup);
        }

        private IEnumerator PlayRankUpCelebration(PilotRankData newRank)
        {
            // Flash
            if (flashPanel != null)
            {
                flashPanel.gameObject.SetActive(true);
                float t = 0f;
                while (t < 0.5f)
                {
                    t += Time.deltaTime;
                    var c = flashPanel.color;
                    c.a = Mathf.PingPong(t * 4f, 1f);
                    flashPanel.color = c;
                    yield return null;
                }
                flashPanel.gameObject.SetActive(false);
            }

            // Badge animation
            if (rankUpBadgeRoot != null)
            {
                rankUpBadgeRoot.gameObject.SetActive(true);
                if (rankUpNameText != null) rankUpNameText.text = newRank.rankName;

                rankUpBadgeRoot.localScale = Vector3.zero;
                float t = 0f;
                while (t < 0.6f)
                {
                    t += Time.deltaTime;
                    rankUpBadgeRoot.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t / 0.4f);
                    yield return null;
                }

                yield return new WaitForSeconds(2f);

                t = 0f;
                while (t < 0.4f)
                {
                    t += Time.deltaTime;
                    rankUpBadgeRoot.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t / 0.4f);
                    yield return null;
                }
                rankUpBadgeRoot.gameObject.SetActive(false);
            }

            // Audio
            if (rankUpAudioSource != null)
                rankUpAudioSource.Play();
        }
    }
}
