// LiveryBrowserUI.cs — Phase 115: Advanced Aircraft Livery Editor
// Livery browser: my liveries, team liveries, community gallery, search/filter.
// Namespace: SWEF.LiveryEditor

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Livery browser UI controller.  Presents three tabs:
    /// My Liveries, Team Liveries, and Community Gallery.  Supports search
    /// and category filtering.
    /// </summary>
    public class LiveryBrowserUI : MonoBehaviour
    {
        // ── Inspector references ──────────────────────────────────────────────────
        [Header("Sub-System References")]
        [SerializeField] private LiveryEditorManager manager;
        [SerializeField] private LiveryGallery        gallery;
        [SerializeField] private TeamLiveryManager    teamManager;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when the user double-clicks a livery to open it for editing.</summary>
        public event Action<LiverySaveData> OnLiverySelected;

        /// <summary>Raised when the user confirms deletion of a livery.</summary>
        public event Action<string> OnLiveryDeleteRequested;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Currently active browser tab.</summary>
        public BrowserTab ActiveTab { get; private set; } = BrowserTab.MyLiveries;

        /// <summary>Current search query string.</summary>
        public string SearchQuery { get; private set; } = string.Empty;

        // ── Tab enum ──────────────────────────────────────────────────────────────
        /// <summary>Available browser tabs.</summary>
        public enum BrowserTab { MyLiveries, TeamLiveries, CommunityGallery }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Switches to the given browser tab and refreshes the list.</summary>
        public void SwitchTab(BrowserTab tab)
        {
            ActiveTab = tab;
            RefreshList();
        }

        /// <summary>Applies a search query to the current tab.</summary>
        public void SetSearchQuery(string query)
        {
            SearchQuery = query ?? string.Empty;
            RefreshList();
        }

        /// <summary>Returns the liveries relevant to the current tab and search query.</summary>
        public IReadOnlyList<LiverySaveData> GetDisplayedLiveries()
        {
            switch (ActiveTab)
            {
                case BrowserTab.TeamLiveries:
                    return teamManager?.TeamLivery != null
                        ? new List<LiverySaveData> { teamManager.TeamLivery }.AsReadOnly()
                        : new List<LiverySaveData>().AsReadOnly();

                case BrowserTab.CommunityGallery:
                    return gallery != null
                        ? gallery.Search(SearchQuery)
                        : new List<LiverySaveData>().AsReadOnly();

                default: // MyLiveries
                {
                    var all = manager != null
                        ? manager.GetAllLiveries()
                        : new List<LiverySaveData>().AsReadOnly();

                    if (string.IsNullOrWhiteSpace(SearchQuery)) return all;

                    string q = SearchQuery.ToLowerInvariant();
                    var filtered = new List<LiverySaveData>();
                    foreach (var l in all)
                        if ((l.Metadata.Name ?? "").ToLowerInvariant().Contains(q))
                            filtered.Add(l);
                    return filtered.AsReadOnly();
                }
            }
        }

        /// <summary>Opens the selected livery in the editor.</summary>
        public void SelectLivery(LiverySaveData livery)
        {
            manager?.LoadLivery(livery);
            OnLiverySelected?.Invoke(livery);
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private void RefreshList()
        {
            // In a full implementation this would rebuild the scroll view.
        }
    }
}
