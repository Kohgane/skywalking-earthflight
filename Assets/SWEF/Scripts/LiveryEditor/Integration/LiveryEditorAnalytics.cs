// LiveryEditorAnalytics.cs — Phase 115: Advanced Aircraft Livery Editor
// Telemetry: editor usage, popular tools, template popularity, sharing stats.
// Namespace: SWEF.LiveryEditor

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Collects anonymous telemetry about livery editor usage.
    /// All tracking is gated behind <c>SWEF_ANALYTICS_AVAILABLE</c>.
    /// </summary>
    public static class LiveryEditorAnalytics
    {
        // ── Session counters ──────────────────────────────────────────────────────
        private static int _sessionsOpened;
        private static int _liveriesCreated;
        private static int _liveriesSaved;
        private static int _liveriesExported;
        private static int _decalsPlaced;
        private static int _brushStrokes;
        private static readonly Dictionary<string, int> _templateUsage = new Dictionary<string, int>();
        private static readonly Dictionary<LiveryEditorToolbar.EditorTool, int> _toolUsage =
            new Dictionary<LiveryEditorToolbar.EditorTool, int>();

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Records the opening of an editor session.</summary>
        public static void TrackEditorOpened()
        {
            _sessionsOpened++;
#if SWEF_ANALYTICS_AVAILABLE
            SendEvent("livery_editor_opened");
#endif
        }

        /// <summary>Records creation of a new livery.</summary>
        public static void TrackLiveryCreated(string liveryId)
        {
            _liveriesCreated++;
#if SWEF_ANALYTICS_AVAILABLE
            SendEvent("livery_created", new[] { ("id", liveryId) });
#endif
        }

        /// <summary>Records saving a livery.</summary>
        public static void TrackLiverySaved(string liveryId)
        {
            _liveriesSaved++;
#if SWEF_ANALYTICS_AVAILABLE
            SendEvent("livery_saved", new[] { ("id", liveryId) });
#endif
        }

        /// <summary>Records exporting a livery.</summary>
        public static void TrackLiveryExported(string liveryId, LiveryExportFormat format)
        {
            _liveriesExported++;
#if SWEF_ANALYTICS_AVAILABLE
            SendEvent("livery_exported", new[] { ("id", liveryId), ("format", format.ToString()) });
#endif
        }

        /// <summary>Records a decal placement.</summary>
        public static void TrackDecalPlaced(DecalCategory category)
        {
            _decalsPlaced++;
#if SWEF_ANALYTICS_AVAILABLE
            SendEvent("decal_placed", new[] { ("category", category.ToString()) });
#endif
        }

        /// <summary>Records a paint brush stroke.</summary>
        public static void TrackBrushStroke(BrushType brushType)
        {
            _brushStrokes++;
#if SWEF_ANALYTICS_AVAILABLE
            SendEvent("brush_stroke", new[] { ("type", brushType.ToString()) });
#endif
        }

        /// <summary>Records use of a livery template.</summary>
        public static void TrackTemplateUsed(string templateId)
        {
            if (!_templateUsage.ContainsKey(templateId)) _templateUsage[templateId] = 0;
            _templateUsage[templateId]++;
#if SWEF_ANALYTICS_AVAILABLE
            SendEvent("template_used", new[] { ("id", templateId) });
#endif
        }

        /// <summary>Records use of a specific editor tool.</summary>
        public static void TrackToolSelected(LiveryEditorToolbar.EditorTool tool)
        {
            if (!_toolUsage.ContainsKey(tool)) _toolUsage[tool] = 0;
            _toolUsage[tool]++;
#if SWEF_ANALYTICS_AVAILABLE
            SendEvent("tool_selected", new[] { ("tool", tool.ToString()) });
#endif
        }

        // ── Session summary ───────────────────────────────────────────────────────

        /// <summary>Returns a summary of the current session's telemetry counters.</summary>
        public static string GetSessionSummary() =>
            $"Sessions={_sessionsOpened} Created={_liveriesCreated} Saved={_liveriesSaved} " +
            $"Exported={_liveriesExported} Decals={_decalsPlaced} Strokes={_brushStrokes}";

        // ── Counters (for testing) ─────────────────────────────────────────────────
        /// <summary>Total editor sessions opened this runtime.</summary>
        public static int SessionsOpened   => _sessionsOpened;

        /// <summary>Total liveries created this runtime.</summary>
        public static int LiveriesCreated  => _liveriesCreated;

        /// <summary>Total liveries saved this runtime.</summary>
        public static int LiveriesSaved    => _liveriesSaved;

        /// <summary>Total liveries exported this runtime.</summary>
        public static int LiveriesExported => _liveriesExported;

        /// <summary>Total decals placed this runtime.</summary>
        public static int DecalsPlaced     => _decalsPlaced;

        /// <summary>Total brush strokes this runtime.</summary>
        public static int BrushStrokes     => _brushStrokes;

        /// <summary>Resets all session counters (for testing).</summary>
        public static void ResetCounters()
        {
            _sessionsOpened = _liveriesCreated = _liveriesSaved =
                _liveriesExported = _decalsPlaced = _brushStrokes = 0;
            _templateUsage.Clear();
            _toolUsage.Clear();
        }

        // ── Internal ──────────────────────────────────────────────────────────────

#if SWEF_ANALYTICS_AVAILABLE
        private static void SendEvent(string eventName, (string key, string value)[] props = null)
        {
            // TODO: forward to Analytics singleton when SWEF_ANALYTICS_AVAILABLE
            Debug.Log($"[SWEF Analytics] {eventName}");
        }
#endif
    }
}
