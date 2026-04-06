// ATCUI.cs — Phase 119: Advanced AI Traffic Control
// ATC interface: communication log, frequency selector, clearance display,
// traffic scope.
// Namespace: SWEF.ATC

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — Root ATC UI controller: wires together the communication log,
    /// frequency selector, clearance panel and radar traffic scope.
    /// </summary>
    public class ATCUI : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private GameObject communicationLogPanel;
        [SerializeField] private GameObject frequencySelectorPanel;
        [SerializeField] private GameObject clearanceDisplayPanel;
        [SerializeField] private GameObject trafficScopePanel;

        private bool _isVisible;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Shows the ATC UI.</summary>
        public void Show()
        {
            _isVisible = true;
            SetPanelsActive(true);
        }

        /// <summary>Hides the ATC UI.</summary>
        public void Hide()
        {
            _isVisible = false;
            SetPanelsActive(false);
        }

        /// <summary>Toggles ATC UI visibility.</summary>
        public void Toggle() { if (_isVisible) Hide(); else Show(); }

        /// <summary>Whether the ATC UI is currently visible.</summary>
        public bool IsVisible => _isVisible;

        private void SetPanelsActive(bool active)
        {
            communicationLogPanel?.SetActive(active);
            frequencySelectorPanel?.SetActive(active);
            clearanceDisplayPanel?.SetActive(active);
            trafficScopePanel?.SetActive(active);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10)) Toggle();
        }
    }
}
