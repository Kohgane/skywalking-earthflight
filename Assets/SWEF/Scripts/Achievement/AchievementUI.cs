using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Achievement
{
    /// <summary>
    /// Displays achievement unlock popup (toast) and an achievement list panel.
    /// </summary>
    public class AchievementUI : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private AchievementManager manager;

        [Header("Toast")]
        [SerializeField] private GameObject toastPanel;
        [SerializeField] private Text toastTitle;
        [SerializeField] private Text toastDescription;
        [SerializeField] private CanvasGroup toastCanvasGroup;
        [SerializeField] private float toastDuration = 3f;

        [Header("Achievement List")]
        [SerializeField] private GameObject listPanel;
        [SerializeField] private Button toggleButton;
        /// <summary>Array of 8 Text elements, one per achievement definition.</summary>
        [SerializeField] private Text[] achievementTexts = new Text[8];

        private Coroutine _toastCoroutine;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (manager == null)
                manager = FindFirstObjectByType<AchievementManager>();

            if (toggleButton != null)
                toggleButton.onClick.AddListener(ToggleList);

            // Hide panels on start
            if (toastPanel != null)
                toastPanel.SetActive(false);
            if (listPanel != null)
                listPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (manager != null)
                manager.OnAchievementUnlocked += HandleAchievementUnlocked;
        }

        private void OnDisable()
        {
            if (manager != null)
                manager.OnAchievementUnlocked -= HandleAchievementUnlocked;
        }

        // ── Event handlers ────────────────────────────────────────────────────────
        private void HandleAchievementUnlocked(AchievementManager.AchievementDef def)
        {
            if (_toastCoroutine != null)
                StopCoroutine(_toastCoroutine);
            _toastCoroutine = StartCoroutine(ShowToast(def));
            RefreshList();
        }

        // ── Public API ────────────────────────────────────────────────────────────
        /// <summary>Refreshes the achievement list panel to reflect current unlock state.</summary>
        public void RefreshList()
        {
            var defs = AchievementManager.Definitions;
            for (int i = 0; i < achievementTexts.Length && i < defs.Length; i++)
            {
                if (achievementTexts[i] == null) continue;
                bool unlocked = manager != null && manager.IsUnlocked(defs[i].id);
                achievementTexts[i].text = unlocked
                    ? $"✅ {defs[i].title}"
                    : $"🔒 {defs[i].title}";
            }
        }

        // ── Internal ─────────────────────────────────────────────────────────────
        private void ToggleList()
        {
            if (listPanel == null) return;
            bool next = !listPanel.activeSelf;
            listPanel.SetActive(next);
            if (next) RefreshList();
        }

        private IEnumerator ShowToast(AchievementManager.AchievementDef def)
        {
            if (toastPanel == null) yield break;

            // Populate text
            if (toastTitle != null)
                toastTitle.text = $"{def.emoji} {def.title}";
            if (toastDescription != null)
                toastDescription.text = def.description;

            toastPanel.SetActive(true);

            // Fade in
            yield return StartCoroutine(FadeToast(0f, 1f, 0.3f));

            // Hold
            yield return new WaitForSeconds(toastDuration);

            // Fade out
            yield return StartCoroutine(FadeToast(1f, 0f, 0.3f));

            toastPanel.SetActive(false);
            _toastCoroutine = null;
        }

        private IEnumerator FadeToast(float from, float to, float duration)
        {
            if (toastCanvasGroup == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                toastCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            toastCanvasGroup.alpha = to;
        }
    }
}
