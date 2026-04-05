// UGCTriggerEditor.cs — SWEF Phase 108: User-Generated Content (UGC) Editor
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.UGC
{
    /// <summary>
    /// Phase 108 — MonoBehaviour for placing and configuring event triggers in the
    /// UGC editor.
    ///
    /// <para>Renders each trigger's activation radius as a wire-sphere gizmo,
    /// draws visual connection lines between chained triggers, and exposes
    /// API for adding, editing, and removing triggers.</para>
    /// </summary>
    public sealed class UGCTriggerEditor : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Visualisation")]
        [Tooltip("Colour used to draw trigger radius spheres.")]
        [SerializeField] private Color _triggerColor = new Color(1f, 0.6f, 0f, 0.5f);

        [Tooltip("Colour used for disabled/inactive triggers.")]
        [SerializeField] private Color _inactiveTriggerColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

        [Tooltip("Colour of the chain connection lines between linked triggers.")]
        [SerializeField] private Color _chainLineColor = new Color(1f, 1f, 0f, 0.8f);

        [Tooltip("Prefab used to render trigger chain connection lines.")]
        [SerializeField] private LineRenderer _chainLinePrefab;

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Raised when a trigger is added to the project.</summary>
        public event Action<UGCTrigger> OnTriggerAdded;

        /// <summary>Raised when a trigger is removed.</summary>
        public event Action<string> OnTriggerRemoved;

        /// <summary>Raised when the selected trigger changes (null = deselected).</summary>
        public event Action<UGCTrigger> OnTriggerSelected;

        // ── Public state ───────────────────────────────────────────────────────

        /// <summary>Currently selected trigger in the editor, or <c>null</c>.</summary>
        public UGCTrigger SelectedTrigger { get; private set; }

        // ── Internal state ─────────────────────────────────────────────────────

        private UGCContent _content;
        private readonly List<LineRenderer> _chainLines = new List<LineRenderer>();

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void OnDestroy()
        {
            ClearChainLines();
            _content = null;
        }

        private void OnDrawGizmosSelected()
        {
            if (_content == null) return;
            DrawTriggerGizmos();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Binds this editor to a content project.
        /// </summary>
        public void SetContent(UGCContent content)
        {
            _content = content;
            RefreshVisuals();
        }

        /// <summary>
        /// Adds a new trigger at the given world position with default settings.
        /// </summary>
        public UGCTrigger AddTrigger(Vector3 worldPosition, UGCTriggerType type = UGCTriggerType.EnterZone)
        {
            if (_content == null) return null;
            if (_content.triggers.Count >= UGCConfig.MaxTriggers)
            {
                Debug.LogWarning("[UGCTriggerEditor] Max triggers reached.");
                return null;
            }

            var trigger = new UGCTrigger
            {
                triggerId   = Guid.NewGuid().ToString(),
                triggerType = type,
                position    = worldPosition,
                radius      = 100f,
                action      = UGCActionType.ShowMessage,
                isEnabled   = true,
            };

            var cmd = new AddTriggerCommand(_content, trigger);
            UGCEditorManager.Instance?.ExecuteCommand(cmd);

            RefreshVisuals();
            OnTriggerAdded?.Invoke(trigger);
            return trigger;
        }

        /// <summary>
        /// Removes the trigger with the given ID.
        /// </summary>
        public void RemoveTrigger(string triggerId)
        {
            if (_content == null) return;
            var trigger = _content.triggers.Find(t => t.triggerId == triggerId);
            if (trigger == null) return;

            _content.triggers.Remove(trigger);
            if (UGCEditorManager.Instance != null)
                UGCEditorManager.Instance.HasUnsavedChanges = true;

            if (SelectedTrigger == trigger) SelectTrigger(null);
            RefreshVisuals();
            OnTriggerRemoved?.Invoke(triggerId);
        }

        /// <summary>
        /// Sets the selected trigger.
        /// </summary>
        public void SelectTrigger(UGCTrigger trigger)
        {
            SelectedTrigger = trigger;
            OnTriggerSelected?.Invoke(trigger);
        }

        /// <summary>
        /// Creates a chain link: when <paramref name="sourceTrigger"/> fires it enables
        /// <paramref name="targetTrigger"/>.
        /// </summary>
        public void ChainTriggers(UGCTrigger sourceTrigger, UGCTrigger targetTrigger)
        {
            if (sourceTrigger == null || targetTrigger == null) return;
            sourceTrigger.chainToTriggerId = targetTrigger.triggerId;
            RefreshVisuals();
        }

        /// <summary>
        /// Removes the chain from <paramref name="sourceTrigger"/>.
        /// </summary>
        public void UnchainTrigger(UGCTrigger sourceTrigger)
        {
            if (sourceTrigger == null) return;
            sourceTrigger.chainToTriggerId = string.Empty;
            RefreshVisuals();
        }

        /// <summary>
        /// Rebuilds the chain-connection line-renderer visualisation.
        /// </summary>
        public void RefreshVisuals()
        {
            ClearChainLines();
            if (_content == null || _chainLinePrefab == null) return;

            foreach (var trigger in _content.triggers)
            {
                if (string.IsNullOrEmpty(trigger.chainToTriggerId)) continue;
                var target = _content.triggers.Find(t => t.triggerId == trigger.chainToTriggerId);
                if (target == null) continue;

                var lr = Instantiate(_chainLinePrefab, transform);
                lr.positionCount = 2;
                lr.SetPosition(0, trigger.position);
                lr.SetPosition(1, target.position);
                lr.startColor = _chainLineColor;
                lr.endColor   = _chainLineColor;
                _chainLines.Add(lr);
            }
        }

        // ── Gizmo drawing ──────────────────────────────────────────────────────

        private void DrawTriggerGizmos()
        {
#if UNITY_EDITOR
            foreach (var trigger in _content.triggers)
            {
                Gizmos.color = trigger.isEnabled ? _triggerColor : _inactiveTriggerColor;
                Gizmos.DrawWireSphere(trigger.position, trigger.radius);
            }

            if (SelectedTrigger != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(SelectedTrigger.position, 5f);
            }
#endif
        }

        private void ClearChainLines()
        {
            foreach (var lr in _chainLines)
                if (lr != null) Destroy(lr.gameObject);
            _chainLines.Clear();
        }
    }
}
