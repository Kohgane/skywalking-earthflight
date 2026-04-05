// SquadronCreateUI.cs — Phase 109: Clan/Squadron System
// Squadron creation wizard — name, tag, description, type, emblem, recruitment settings.
// Namespace: SWEF.Squadron

using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Squadron
{
    /// <summary>
    /// Phase 109 — Multi-step wizard for creating a new squadron.
    /// Steps: (1) Name/Tag/Description → (2) Type selection → (3) Emblem designer
    /// → (4) Recruitment settings → (5) Confirm &amp; Create.
    /// </summary>
    public sealed class SquadronCreateUI : MonoBehaviour
    {
        // ── Inspector references ───────────────────────────────────────────────

        [Header("Step Panels")]
        [SerializeField] private GameObject stepBasicInfo;
        [SerializeField] private GameObject stepType;
        [SerializeField] private GameObject stepEmblem;
        [SerializeField] private GameObject stepRecruitment;
        [SerializeField] private GameObject stepConfirm;

        [Header("Step 1 — Basic Info")]
        [SerializeField] private InputField nameInput;
        [SerializeField] private InputField tagInput;
        [SerializeField] private InputField descriptionInput;
        [SerializeField] private Text nameValidationText;
        [SerializeField] private Text tagValidationText;

        [Header("Step 2 — Type")]
        [SerializeField] private Dropdown typeDropdown;

        [Header("Step 3 — Emblem")]
        [SerializeField] private Slider emblemHueSlider;
        [SerializeField] private Dropdown emblemIconDropdown;
        [SerializeField] private Dropdown emblemPatternDropdown;
        [SerializeField] private Image emblemPreview;

        [Header("Step 4 — Recruitment")]
        [SerializeField] private Toggle isRecruitingToggle;
        [SerializeField] private Slider minLevelSlider;
        [SerializeField] private Text minLevelLabel;
        [SerializeField] private Slider minFlightHoursSlider;
        [SerializeField] private Text minFlightHoursLabel;

        [Header("Step 5 — Confirm")]
        [SerializeField] private Text confirmSummaryText;
        [SerializeField] private Button confirmCreateButton;
        [SerializeField] private Text errorText;

        [Header("Navigation")]
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;

        // ── State ──────────────────────────────────────────────────────────────

        private int _currentStep;
        private const int TotalSteps = 5;

        // Wizard state
        private string _name;
        private string _tag;
        private string _description;
        private SquadronType _type;
        private float _emblemHue;
        private bool _isRecruiting;
        private int _minLevel;
        private float _minFlightHours;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Start()
        {
            if (prevButton    != null) prevButton.onClick.AddListener(GoBack);
            if (nextButton    != null) nextButton.onClick.AddListener(GoNext);
            if (confirmCreateButton != null) confirmCreateButton.onClick.AddListener(CreateSquadron);

            if (emblemHueSlider   != null) emblemHueSlider.onValueChanged.AddListener(_ => UpdateEmblemPreview());
            if (minLevelSlider    != null) minLevelSlider.onValueChanged.AddListener(v => {
                _minLevel = (int)v;
                if (minLevelLabel != null) minLevelLabel.text = $"Min Level: {_minLevel}";
            });
            if (minFlightHoursSlider != null) minFlightHoursSlider.onValueChanged.AddListener(v => {
                _minFlightHours = v;
                if (minFlightHoursLabel != null) minFlightHoursLabel.text = $"Min Flight Hours: {_minFlightHours:F0}";
            });

            ShowStep(0);
        }

        // ── Navigation ─────────────────────────────────────────────────────────

        private void GoNext()
        {
            if (!ValidateCurrentStep()) return;
            CollectCurrentStep();

            _currentStep = Mathf.Min(_currentStep + 1, TotalSteps - 1);
            ShowStep(_currentStep);
        }

        private void GoBack()
        {
            _currentStep = Mathf.Max(_currentStep - 1, 0);
            ShowStep(_currentStep);
        }

        private void ShowStep(int step)
        {
            var panels = new[] { stepBasicInfo, stepType, stepEmblem, stepRecruitment, stepConfirm };
            for (int i = 0; i < panels.Length; i++)
                if (panels[i] != null) panels[i].SetActive(i == step);

            if (prevButton != null) prevButton.interactable = step > 0;
            if (nextButton != null) nextButton.gameObject.SetActive(step < TotalSteps - 1);
            if (confirmCreateButton != null) confirmCreateButton.gameObject.SetActive(step == TotalSteps - 1);

            if (step == TotalSteps - 1) BuildConfirmSummary();
        }

        // ── Validation & collection ────────────────────────────────────────────

        private bool ValidateCurrentStep()
        {
            if (_currentStep == 0)
            {
                bool nameOk = nameInput != null &&
                              nameInput.text.Trim().Length >= SquadronConfig.NameMinLength &&
                              nameInput.text.Trim().Length <= SquadronConfig.NameMaxLength;

                bool tagOk  = tagInput != null &&
                              tagInput.text.Trim().Length >= SquadronConfig.TagMinLength &&
                              tagInput.text.Trim().Length <= SquadronConfig.TagMaxLength;

                if (nameValidationText != null)
                    nameValidationText.text = nameOk ? string.Empty : $"Name must be {SquadronConfig.NameMinLength}–{SquadronConfig.NameMaxLength} characters.";

                if (tagValidationText != null)
                    tagValidationText.text = tagOk ? string.Empty : $"Tag must be {SquadronConfig.TagMinLength}–{SquadronConfig.TagMaxLength} characters.";

                return nameOk && tagOk;
            }
            return true;
        }

        private void CollectCurrentStep()
        {
            switch (_currentStep)
            {
                case 0:
                    _name        = nameInput?.text.Trim() ?? string.Empty;
                    _tag         = tagInput?.text.Trim().ToUpperInvariant() ?? string.Empty;
                    _description = descriptionInput?.text ?? string.Empty;
                    break;

                case 1:
                    _type = typeDropdown != null
                        ? (SquadronType)typeDropdown.value
                        : SquadronType.Casual;
                    break;

                case 2:
                    _emblemHue = emblemHueSlider?.value ?? 0f;
                    break;

                case 3:
                    _isRecruiting  = isRecruitingToggle?.isOn ?? true;
                    _minLevel      = minLevelSlider != null ? (int)minLevelSlider.value : 0;
                    _minFlightHours = minFlightHoursSlider?.value ?? 0f;
                    break;
            }
        }

        private void BuildConfirmSummary()
        {
            if (confirmSummaryText == null) return;
            confirmSummaryText.text =
                $"Name: {_name}\n" +
                $"Tag: [{_tag}]\n" +
                $"Type: {_type}\n" +
                $"Recruiting: {(_isRecruiting ? "Yes" : "No")}\n" +
                $"Min Level: {_minLevel}  |  Min Hours: {_minFlightHours:F0}";
        }

        private void UpdateEmblemPreview()
        {
            if (emblemPreview == null || emblemHueSlider == null) return;
            emblemPreview.color = Color.HSVToRGB(emblemHueSlider.value, 0.8f, 0.9f);
        }

        // ── Create ─────────────────────────────────────────────────────────────

        private void CreateSquadron()
        {
            CollectCurrentStep();

            if (errorText != null) errorText.text = string.Empty;

            var manager = SquadronManager.Instance;
            if (manager == null)
            {
                ShowError("Squadron system unavailable.");
                return;
            }

            var squadron = manager.CreateSquadron(_name, _tag, _description, _type);
            if (squadron == null)
            {
                ShowError("Failed to create squadron. Check name and tag requirements.");
                return;
            }

            // Apply recruitment settings
            squadron.isRecruiting             = _isRecruiting;
            squadron.requirementMinLevel      = _minLevel;
            squadron.requirementMinFlightHours = _minFlightHours;
            squadron.emblemData               = $"hue:{_emblemHue:F2}";

            // Initialise the base
            SquadronBaseManager.Instance?.InitialiseBase(squadron.squadronId);

            // Post a system welcome message
            SquadronChatController.Instance?.PostSystemMessage($"Welcome to {_name}! The squadron has been founded.");

            gameObject.SetActive(false);
        }

        private void ShowError(string message)
        {
            if (errorText != null) errorText.text = message;
        }
    }
}
