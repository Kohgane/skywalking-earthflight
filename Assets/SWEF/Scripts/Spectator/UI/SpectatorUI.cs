// SpectatorUI.cs — SWEF Phase 107: Live Streaming & Spectator Mode
using System;
using UnityEngine;
using UnityEngine.UI;

#if SWEF_TMPRO_AVAILABLE
using TMPro;
#endif

namespace SWEF.Spectator
{
    /// <summary>
    /// Phase 107 — Full spectator settings panel.
    ///
    /// <para>Provides the player-facing UI for:</para>
    /// <list type="bullet">
    ///   <item>Camera mode selection (buttons or dropdown).</item>
    ///   <item>Target aircraft selection dropdown.</item>
    ///   <item>Auto-director toggle and min/max cut interval sliders.</item>
    ///   <item>Chat overlay toggle.</item>
    ///   <item>Replay / recording controls placeholder.</item>
    /// </list>
    ///
    /// <para>Wire all UI element references via the Inspector.</para>
    /// </summary>
    public sealed class SpectatorUI : MonoBehaviour
    {
        // ── Inspector — panel ─────────────────────────────────────────────────
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;

        // ── Inspector — camera mode ───────────────────────────────────────────
        [Header("Camera Mode")]
        [SerializeField] private Dropdown cameraModeDropdown;

        // ── Inspector — target selection ──────────────────────────────────────
        [Header("Target Selection")]
        [SerializeField] private Dropdown targetDropdown;
        [SerializeField] private Button nextTargetButton;
        [SerializeField] private Button prevTargetButton;

        // ── Inspector — auto-director ─────────────────────────────────────────
        [Header("Auto-Director")]
        [SerializeField] private Toggle autoDirectorToggle;
        [SerializeField] private Slider minCutSlider;
        [SerializeField] private Slider maxCutSlider;

        // ── Inspector — chat ──────────────────────────────────────────────────
        [Header("Chat Overlay")]
        [SerializeField] private Toggle chatOverlayToggle;

        // ── Inspector — recording ─────────────────────────────────────────────
        [Header("Recording / Replay")]
        [SerializeField] private Button markHighlightButton;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Start()
        {
            BindButtons();
            BindDropdowns();
        }

        private void OnEnable()
        {
            RefreshTargetDropdown();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Shows or hides the spectator settings panel.</summary>
        public void SetVisible(bool visible)
        {
            if (panelRoot != null)
                panelRoot.SetActive(visible);
        }

        /// <summary>Toggles the spectator settings panel visibility.</summary>
        public void ToggleVisible()
        {
            if (panelRoot != null)
                panelRoot.SetActive(!panelRoot.activeSelf);
        }

        // ── Private binding helpers ────────────────────────────────────────────

        private void BindButtons()
        {
            if (nextTargetButton != null)
                nextTargetButton.onClick.AddListener(OnNextTarget);

            if (prevTargetButton != null)
                prevTargetButton.onClick.AddListener(OnPrevTarget);

            if (autoDirectorToggle != null)
                autoDirectorToggle.onValueChanged.AddListener(OnAutoDirectorToggled);

            if (chatOverlayToggle != null)
                chatOverlayToggle.onValueChanged.AddListener(OnChatOverlayToggled);

            if (markHighlightButton != null)
                markHighlightButton.onClick.AddListener(OnMarkHighlight);
        }

        private void BindDropdowns()
        {
            if (cameraModeDropdown != null)
            {
                cameraModeDropdown.ClearOptions();
                foreach (SpectatorCameraMode mode in Enum.GetValues(typeof(SpectatorCameraMode)))
                    cameraModeDropdown.options.Add(new Dropdown.OptionData(mode.ToString()));

                cameraModeDropdown.onValueChanged.AddListener(OnCameraModeSelected);
                cameraModeDropdown.RefreshShownValue();
            }
        }

        private void RefreshTargetDropdown()
        {
            if (targetDropdown == null) return;
            targetDropdown.ClearOptions();

            var spectator = SpectatorModeController.Instance;
            if (spectator == null) return;

            targetDropdown.options.Add(new Dropdown.OptionData("— FreeCam —"));
            foreach (var t in spectator.GetTargets())
                targetDropdown.options.Add(new Dropdown.OptionData(t != null ? t.name : "Unknown"));

            targetDropdown.onValueChanged.AddListener(OnTargetSelected);
            targetDropdown.RefreshShownValue();
        }

        // ── UI event handlers ──────────────────────────────────────────────────

        private void OnCameraModeSelected(int index)
        {
            var modes = (SpectatorCameraMode[])Enum.GetValues(typeof(SpectatorCameraMode));
            if (index < 0 || index >= modes.Length) return;

            SpectatorModeController.Instance?.SetCameraMode(modes[index]);
        }

        private void OnTargetSelected(int index)
        {
            var spectator = SpectatorModeController.Instance;
            if (spectator == null) return;

            // Index 0 is "FreeCam" (no target)
            if (index == 0)
            {
                spectator.SetCameraMode(SpectatorCameraMode.FreeCam);
                return;
            }

            var targets = spectator.GetTargets();
            int targetIndex = index - 1;
            if (targetIndex >= 0 && targetIndex < targets.Count)
                spectator.SelectTarget(targets[targetIndex]);
        }

        private void OnNextTarget() => SpectatorModeController.Instance?.SelectNextTarget();

        private void OnPrevTarget() => SpectatorModeController.Instance?.SelectPreviousTarget();

        private void OnAutoDirectorToggled(bool isOn)
        {
            var director = CameraSwitchDirector.Instance;
            if (director == null) return;

            if (isOn) director.StartAutoDirector();
            else      director.StopAutoDirector();
        }

        private void OnChatOverlayToggled(bool isOn)
        {
            // The chat panel visibility is driven by LiveChatController;
            // this toggle is a convenience passthrough.
            Debug.Log($"[SpectatorUI] Chat overlay {(isOn ? "enabled" : "disabled")}.");
        }

        private void OnMarkHighlight()
        {
            CommentatorController.Instance?.MarkHighlight();
        }
    }
}
