using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Social
{
    /// <summary>
    /// 플레이어 프로필 편집 패널 (설정 화면).
    /// Profile editing panel for the Settings screen.
    /// </summary>
    public class PlayerProfileUI : MonoBehaviour
    {
        [Header("UI Fields")]
        [SerializeField] private InputField nameInput;
        [SerializeField] private Dropdown   regionDropdown;
        [SerializeField] private Button     saveButton;
        [SerializeField] private Text       validationText;

        // ── Region code list aligned with dropdown options ────────────────────────
        private readonly List<string> _regionCodes = new();

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            saveButton?.onClick.AddListener(OnSave);
            nameInput?.onValueChanged.AddListener(_ => ClearValidation());
            SetupRegionDropdown();
        }

        private void OnEnable()
        {
            // 패널이 열릴 때 현재 프로필 값으로 채우기
            var profile = PlayerProfileManager.Instance;
            if (profile == null) return;

            if (nameInput != null)
                nameInput.text = profile.DisplayName;

            int regionIdx = _regionCodes.IndexOf(profile.Region);
            if (regionDropdown != null && regionIdx >= 0)
                regionDropdown.value = regionIdx;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void OnSave()
        {
            var profile = PlayerProfileManager.Instance;
            if (profile == null)
            {
                ShowValidation("PlayerProfileManager not found.", false);
                return;
            }

            string newName = nameInput != null ? nameInput.text : string.Empty;

            if (!PlayerProfileManager.IsValidDisplayName(newName, out string reason))
            {
                ShowValidation(reason, false);
                return;
            }

            profile.SetDisplayName(newName);

            if (regionDropdown != null && regionDropdown.value < _regionCodes.Count)
                profile.SetRegion(_regionCodes[regionDropdown.value]);

            ShowValidation("저장되었습니다 / Saved!", true);
            Debug.Log("[SWEF] PlayerProfileUI: profile saved.");
        }

        private void SetupRegionDropdown()
        {
            if (regionDropdown == null) return;
            regionDropdown.ClearOptions();
            _regionCodes.Clear();

            var options = new List<string>();
            foreach (var kv in RegionHelper.Regions)
            {
                _regionCodes.Add(kv.Key);
                options.Add($"{RegionHelper.GetFlagEmoji(kv.Key)} {kv.Value}");
            }
            regionDropdown.AddOptions(options);
        }

        private void ShowValidation(string message, bool success)
        {
            if (validationText == null) return;
            validationText.text  = message;
            validationText.color = success ? new Color(0.2f, 0.8f, 0.2f) : new Color(1f, 0.3f, 0.3f);
            validationText.gameObject.SetActive(true);
        }

        private void ClearValidation()
        {
            if (validationText != null)
                validationText.gameObject.SetActive(false);
        }
    }
}
