using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.InputSystem
{
    /// <summary>
    /// Phase 57 — MonoBehaviour UI controller that renders a rebinding panel and
    /// wires user interaction back to <see cref="InputBindingManager"/>.
    /// <para>
    /// Assign all serialised references in the inspector.  The panel reads the
    /// current binding map on <see cref="Open"/>, builds a row per action, and
    /// delegates rebind/reset operations to <see cref="InputBindingManager.Instance"/>.
    /// </para>
    /// </summary>
    public class InputRebindingUI : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static InputRebindingUI Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Panel Root")]
        [Tooltip("Root GameObject of the rebinding panel — toggled by Open/Close.")]
        [SerializeField] private GameObject panelRoot;

        [Header("Row Template")]
        [Tooltip("Template row GameObject — must contain child Text/TMP components for action and key labels.")]
        [SerializeField] private GameObject rowTemplate;

        [Tooltip("Parent transform that rows are instantiated under.")]
        [SerializeField] private Transform  rowContainer;

        [Header("Buttons")]
        [Tooltip("Button that closes the panel without saving (changes are live via InputBindingManager).")]
        [SerializeField] private Button closeButton;

        [Tooltip("Button that resets all bindings to defaults.")]
        [SerializeField] private Button resetAllButton;

        [Header("Category Filter")]
        [Tooltip("Dropdown used to filter bindings by InputActionCategory. Optional.")]
        [SerializeField] private Dropdown categoryDropdown;

        [Header("Listening Overlay")]
        [Tooltip("Overlay shown while waiting for the player to press a new key.")]
        [SerializeField] private GameObject listeningOverlay;

        [Tooltip("Text component inside the listening overlay that displays the action name.")]
        [SerializeField] private Text listeningLabel;

        [Header("Preset Buttons")]
        [Tooltip("Optional list of preset buttons. Each button's name must match an InputPreset.presetName.")]
        [SerializeField] private List<Button> presetButtons = new List<Button>();

        #endregion

        #region Events

        /// <summary>Fired when the rebinding panel is opened.</summary>
        public event Action OnPanelOpened;

        /// <summary>Fired when the rebinding panel is closed.</summary>
        public event Action OnPanelClosed;

        #endregion

        #region Public Properties

        /// <summary><c>true</c> when the rebinding panel is currently visible.</summary>
        public bool IsOpen { get; private set; }

        #endregion

        #region Private State

        private readonly List<BindingRowUI> _rows        = new List<BindingRowUI>();
        private InputActionCategory         _activeFilter = InputActionCategory.Flight;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (panelRoot != null)   panelRoot.SetActive(false);
            if (listeningOverlay != null) listeningOverlay.SetActive(false);

            WireButtons();
            WireCategoryDropdown();
        }

        private void OnEnable()
        {
            if (InputBindingManager.Instance != null)
            {
                InputBindingManager.Instance.OnRebindStarted   += HandleRebindStarted;
                InputBindingManager.Instance.OnRebindFinished  += HandleRebindFinished;
                InputBindingManager.Instance.OnBindingsReset   += RefreshRows;
                InputBindingManager.Instance.OnBindingChanged  += HandleBindingChanged;
            }
        }

        private void OnDisable()
        {
            if (InputBindingManager.Instance != null)
            {
                InputBindingManager.Instance.OnRebindStarted   -= HandleRebindStarted;
                InputBindingManager.Instance.OnRebindFinished  -= HandleRebindFinished;
                InputBindingManager.Instance.OnBindingsReset   -= RefreshRows;
                InputBindingManager.Instance.OnBindingChanged  -= HandleBindingChanged;
            }
        }

        private void HandleBindingChanged(BindingEntry _) => RefreshRows();

        #endregion

        #region Public API

        /// <summary>Opens the rebinding panel and populates rows for the current category.</summary>
        public void Open()
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            IsOpen = true;
            BuildRows();
            OnPanelOpened?.Invoke();
        }

        /// <summary>Closes the rebinding panel.</summary>
        public void Close()
        {
            if (InputBindingManager.Instance != null && InputBindingManager.Instance.IsRebinding)
                InputBindingManager.Instance.CancelRebind();

            if (panelRoot != null) panelRoot.SetActive(false);
            IsOpen = false;
            OnPanelClosed?.Invoke();
        }

        /// <summary>Toggles the panel open or closed.</summary>
        public void Toggle() { if (IsOpen) Close(); else Open(); }

        #endregion

        #region Private — Row Construction

        private void BuildRows()
        {
            ClearRows();
            if (InputBindingManager.Instance == null) return;

            var bindings = InputBindingManager.Instance.GetBindingsByCategory(_activeFilter);
            foreach (var entry in bindings)
            {
                if (rowTemplate == null || rowContainer == null) break;

                GameObject rowGO = Instantiate(rowTemplate, rowContainer);
                rowGO.SetActive(true);

                var row = new BindingRowUI(rowGO, entry, OnRowRebindClicked);
                _rows.Add(row);
            }
        }

        private void ClearRows()
        {
            foreach (var row in _rows)
                if (row.Root != null) Destroy(row.Root);
            _rows.Clear();
        }

        private void RefreshRows()
        {
            if (!IsOpen) return;
            BuildRows();
        }

        private void OnRowRebindClicked(string actionName)
        {
            if (InputBindingManager.Instance != null)
                InputBindingManager.Instance.StartRebind(actionName);
        }

        #endregion

        #region Private — Wiring

        private void WireButtons()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (resetAllButton != null)
                resetAllButton.onClick.AddListener(() => InputBindingManager.Instance?.ResetAllBindings());

            foreach (var btn in presetButtons)
            {
                if (btn == null) continue;
                string presetName = btn.name;
                btn.onClick.AddListener(() => InputBindingManager.Instance?.ApplyPreset(presetName));
            }
        }

        private void WireCategoryDropdown()
        {
            if (categoryDropdown == null) return;
            categoryDropdown.ClearOptions();

            var options = new List<Dropdown.OptionData>();
            foreach (InputActionCategory cat in Enum.GetValues(typeof(InputActionCategory)))
                options.Add(new Dropdown.OptionData(cat.ToString()));
            categoryDropdown.AddOptions(options);

            categoryDropdown.onValueChanged.AddListener(idx =>
            {
                _activeFilter = (InputActionCategory)idx;
                BuildRows();
            });
        }

        #endregion

        #region Private — Listening Overlay

        private void HandleRebindStarted(string actionName)
        {
            if (listeningOverlay != null) listeningOverlay.SetActive(true);
            if (listeningLabel   != null) listeningLabel.text = $"Press a key for:\n{actionName}";
        }

        private void HandleRebindFinished(string actionName, bool success)
        {
            if (listeningOverlay != null) listeningOverlay.SetActive(false);
        }

        #endregion

        // ── Inner helper — no MonoBehaviour needed ────────────────────────────────

        /// <summary>Lightweight wrapper that links a row GameObject to its action name.</summary>
        private sealed class BindingRowUI
        {
            public GameObject Root { get; }

            private readonly string _actionName;

            public BindingRowUI(GameObject root, BindingEntry entry, Action<string> onRebindClicked)
            {
                Root         = root;
                _actionName  = entry.actionName;

                // Attempt to find and populate Text labels by name convention.
                SetChildText(root, "ActionLabel", entry.actionName);
                SetChildText(root, "PrimaryKeyLabel", entry.primaryKey ?? "–");
                SetChildText(root, "SecondaryKeyLabel",
                    string.IsNullOrEmpty(entry.secondaryKey) ? "–" : entry.secondaryKey);

                // Wire rebind button if present.
                var rebindBtn = root.GetComponentInChildren<Button>();
                if (rebindBtn != null && entry.isRebindable)
                {
                    string name = entry.actionName;
                    rebindBtn.onClick.AddListener(() => onRebindClicked(name));
                    rebindBtn.interactable = true;
                }
                else if (rebindBtn != null)
                {
                    rebindBtn.interactable = false;
                }
            }

            private static void SetChildText(GameObject root, string childName, string value)
            {
                Transform t = root.transform.Find(childName);
                if (t == null) return;
                var label = t.GetComponent<Text>();
                if (label != null) label.text = value;
            }
        }
    }
}
