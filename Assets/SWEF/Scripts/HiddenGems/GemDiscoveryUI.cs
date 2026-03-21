using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SWEF.Localization;
using SWEF.Social;
using SWEF.GuidedTour;

namespace SWEF.HiddenGems
{
    /// <summary>
    /// Discovery popup notification that appears when the player discovers a hidden gem.
    /// Multiple simultaneous discoveries are queued and shown sequentially.
    /// Auto-dismisses after <see cref="autoHideSeconds"/> seconds.
    /// </summary>
    public class GemDiscoveryUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Panel References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Content")]
        [SerializeField] private TextMeshProUGUI gemNameText;
        [SerializeField] private TextMeshProUGUI rarityText;
        [SerializeField] private TextMeshProUGUI categoryText;
        [SerializeField] private TextMeshProUGUI continentText;
        [SerializeField] private TextMeshProUGUI xpText;
        [SerializeField] private TextMeshProUGUI funFactText;
        [SerializeField] private Image           rarityBorderImage;
        [SerializeField] private Image           categoryIconImage;

        [Header("Buttons")]
        [SerializeField] private Button viewCollectionButton;
        [SerializeField] private Button navigateButton;
        [SerializeField] private Button shareButton;
        [SerializeField] private Button dismissButton;

        [Header("Timing")]
        [SerializeField] private float autoHideSeconds  = 8f;
        [SerializeField] private float fadeInDuration   = 0.4f;
        [SerializeField] private float fadeOutDuration  = 0.4f;
        [SerializeField] private float xpAnimDuration   = 1.2f;

        // ── State ─────────────────────────────────────────────────────────────────
        private readonly Queue<GemDiscoveryEvent> _queue     = new Queue<GemDiscoveryEvent>();
        private GemDiscoveryEvent                 _current;
        private bool                              _isShowing;
        private Coroutine                         _autoHideCoroutine;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        private void OnEnable()
        {
            if (HiddenGemManager.Instance != null)
                HiddenGemManager.Instance.OnGemDiscovered += EnqueueDiscovery;
        }

        private void OnDisable()
        {
            if (HiddenGemManager.Instance != null)
                HiddenGemManager.Instance.OnGemDiscovered -= EnqueueDiscovery;
        }

        private void Start()
        {
            if (viewCollectionButton != null)
                viewCollectionButton.onClick.AddListener(OnViewCollection);
            if (navigateButton != null)
                navigateButton.onClick.AddListener(OnNavigate);
            if (shareButton != null)
                shareButton.onClick.AddListener(OnShare);
            if (dismissButton != null)
                dismissButton.onClick.AddListener(Dismiss);
        }

        // ── Queue management ──────────────────────────────────────────────────────

        private void EnqueueDiscovery(GemDiscoveryEvent evt)
        {
            _queue.Enqueue(evt);
            if (!_isShowing) StartCoroutine(ShowNextInQueue());
        }

        private IEnumerator ShowNextInQueue()
        {
            while (_queue.Count > 0)
            {
                _current  = _queue.Dequeue();
                _isShowing = true;
                yield return ShowPanel(_current);
                _isShowing = false;
            }
        }

        // ── Display ───────────────────────────────────────────────────────────────

        private IEnumerator ShowPanel(GemDiscoveryEvent evt)
        {
            var gem  = evt.gem;
            var lm   = LocalizationManager.Instance;

            // Populate text fields
            if (gemNameText   != null) gemNameText.text   = lm != null ? lm.GetText(gem.nameKey)        : gem.nameKey;
            if (rarityText    != null) rarityText.text    = lm != null ? lm.GetText($"gem_rarity_{gem.rarity.ToString().ToLowerInvariant()}") : gem.rarity.ToString();
            if (categoryText  != null) categoryText.text  = lm != null ? lm.GetText($"gem_category_{gem.category.ToString().ToLowerInvariant()}") : gem.category.ToString();
            if (continentText != null) continentText.text = lm != null ? lm.GetText($"gem_continent_{gem.continent.ToString().ToLowerInvariant()}") : gem.continent.ToString();
            if (funFactText   != null) funFactText.text   = lm != null ? lm.GetText(gem.factKey)        : gem.factKey;
            if (xpText        != null) xpText.text        = $"+{gem.xpReward} XP";

            // Rarity border color
            if (rarityBorderImage != null && ColorUtility.TryParseHtmlString(
                HiddenGemDefinition.RarityColor(gem.rarity), out Color c))
                rarityBorderImage.color = c;

            // Accessibility: apply colorblind-safe palette if needed
            ApplyAccessibility(gem.rarity);

            // Show panel
            if (panelRoot != null) panelRoot.SetActive(true);
            yield return FadeTo(1f, fadeInDuration);

            // XP count-up animation
            if (xpText != null)
                yield return AnimateXP(0, gem.xpReward, xpAnimDuration);

            // Auto-hide timer
            _autoHideCoroutine = StartCoroutine(AutoHide());
            yield return new WaitUntil(() => !_isShowing);
        }

        private IEnumerator AutoHide()
        {
            yield return new WaitForSeconds(autoHideSeconds);
            yield return Dismiss_Internal();
        }

        /// <summary>Dismiss triggered by button.</summary>
        public void Dismiss()
        {
            if (_autoHideCoroutine != null) StopCoroutine(_autoHideCoroutine);
            StartCoroutine(Dismiss_Internal());
        }

        private IEnumerator Dismiss_Internal()
        {
            yield return FadeTo(0f, fadeOutDuration);
            if (panelRoot != null) panelRoot.SetActive(false);
            _isShowing = false;
        }

        private IEnumerator FadeTo(float target, float duration)
        {
            if (canvasGroup == null) yield break;
            float start   = canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed           += Time.deltaTime;
                canvasGroup.alpha  = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = target;
        }

        private IEnumerator AnimateXP(int from, int to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                int value = Mathf.RoundToInt(Mathf.Lerp(from, to, elapsed / duration));
                if (xpText != null) xpText.text = $"+{value} XP";
                yield return null;
            }
            if (xpText != null) xpText.text = $"+{to} XP";
        }

        // ── Accessibility ─────────────────────────────────────────────────────────

        private void ApplyAccessibility(GemRarity rarity)
        {
            // AccessibilityManager integration: rarity colour is already text-labelled.
            // If a colorblind mode is active, the text label provides the fallback cue.
            // Extended implementation can query AccessibilityManager.Instance.CurrentProfile.
        }

        // ── Button handlers ───────────────────────────────────────────────────────

        private void OnViewCollection()
        {
            var col = FindFirstObjectByType<GemCollectionUI>();
            col?.Show();
        }

        private void OnNavigate()
        {
            if (_current == null) return;
            var nav = FindFirstObjectByType<WaypointNavigator>();
            if (nav == null) return;
            Vector3 dest = HiddenGemManager.GetWorldPosition(_current.gem);
            nav.SetManualTarget(dest);
        }

        private void OnShare()
        {
            if (_current == null) return;
            var lm      = LocalizationManager.Instance;
            string name = lm != null ? lm.GetText(_current.gem.nameKey) : _current.gem.nameKey;
            string msg  = $"I just discovered {name} in Skywalking: Earth Flight! #SWEF #HiddenGems";
            var ssm = FindFirstObjectByType<ShareManager>();
            ssm?.ShareText(msg);
        }
    }
}
