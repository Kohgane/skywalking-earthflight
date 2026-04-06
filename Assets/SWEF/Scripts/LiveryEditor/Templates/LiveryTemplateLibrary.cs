// LiveryTemplateLibrary.cs — Phase 115: Advanced Aircraft Livery Editor
// Pre-made livery templates: real-world inspired airline schemes, military camo, racing liveries.
// Namespace: SWEF.LiveryEditor

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Catalogue of pre-made livery templates grouped by
    /// <see cref="LiveryTemplateCategory"/>.
    /// </summary>
    [Serializable]
    public class LiveryTemplate
    {
        /// <summary>Unique identifier for this template.</summary>
        public string TemplateId;
        /// <summary>Display name shown in the template picker.</summary>
        public string Name;
        /// <summary>Thematic category.</summary>
        public LiveryTemplateCategory Category;
        /// <summary>Short description of the template's inspiration.</summary>
        public string Description;
        /// <summary>Primary colour used by this template.</summary>
        public Color PrimaryColor  = Color.white;
        /// <summary>Secondary / accent colour.</summary>
        public Color SecondaryColor = Color.blue;
        /// <summary>Pattern type applied by this template.</summary>
        public PatternType Pattern = PatternType.Stripes;
        /// <summary>Thumbnail texture for preview in the gallery.</summary>
        public Texture2D Thumbnail;
    }

    /// <summary>
    /// Phase 115 — Maintains the built-in template catalogue and provides
    /// lookup and filtering helpers.
    /// </summary>
    public class LiveryTemplateLibrary : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [SerializeField] private List<LiveryTemplate> customTemplates = new List<LiveryTemplate>();

        // ── Internal catalogue ────────────────────────────────────────────────────
        private readonly Dictionary<string, LiveryTemplate> _catalogue =
            new Dictionary<string, LiveryTemplate>(StringComparer.OrdinalIgnoreCase);

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            SeedBuiltIns();
            foreach (var t in customTemplates)
                if (t != null && !string.IsNullOrWhiteSpace(t.TemplateId))
                    _catalogue[t.TemplateId] = t;

            Debug.Log($"[SWEF] LiveryTemplateLibrary: {_catalogue.Count} templates loaded.");
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns all registered templates.</summary>
        public IReadOnlyList<LiveryTemplate> GetAll() =>
            _catalogue.Values.ToList().AsReadOnly();

        /// <summary>Returns all templates for a given category.</summary>
        public IReadOnlyList<LiveryTemplate> GetByCategory(LiveryTemplateCategory category) =>
            _catalogue.Values.Where(t => t.Category == category).ToList().AsReadOnly();

        /// <summary>Looks up a template by id.</summary>
        public LiveryTemplate Find(string templateId)
        {
            _catalogue.TryGetValue(templateId, out var t);
            return t;
        }

        /// <summary>Number of templates in the catalogue.</summary>
        public int Count => _catalogue.Count;

        // ── Seeding ───────────────────────────────────────────────────────────────

        private void SeedBuiltIns()
        {
            Seed("tmpl_airline_white",   "White Global",    LiveryTemplateCategory.Commercial, Color.white,          new Color(0.12f, 0.38f, 0.74f), PatternType.Stripes,    "Classic white-and-blue international airline");
            Seed("tmpl_airline_red",     "Red Express",     LiveryTemplateCategory.Commercial, Color.red,            Color.white,                    PatternType.Stripes,    "Bold red livery inspired by short-haul carriers");
            Seed("tmpl_military_camo",   "Olive Camo",      LiveryTemplateCategory.Military,   new Color(0.33f, 0.42f, 0.18f), new Color(0.25f, 0.22f, 0.10f), PatternType.Camouflage, "Multi-role fighter camouflage scheme");
            Seed("tmpl_military_grey",   "Air Defence Grey",LiveryTemplateCategory.Military,   new Color(0.45f, 0.45f, 0.45f), new Color(0.60f, 0.60f, 0.60f), PatternType.Stripes,    "Two-tone grey air superiority scheme");
            Seed("tmpl_racing_speed",    "Speed Demon",     LiveryTemplateCategory.Racing,     Color.yellow,         Color.black,                    PatternType.Chevrons,   "High-contrast racing livery with chevrons");
            Seed("tmpl_racing_chequered","Chequered Flag",  LiveryTemplateCategory.Racing,     Color.white,          Color.black,                    PatternType.Chequered,  "Classic motorsport chequered pattern");
            Seed("tmpl_historic_biplane","Biplane Cream",   LiveryTemplateCategory.Historic,   new Color(0.96f, 0.91f, 0.72f), new Color(0.55f, 0.27f, 0.07f), PatternType.Stripes,    "1920s-era barnstormer colour scheme");
            Seed("tmpl_fantasy_nebula",  "Nebula",          LiveryTemplateCategory.Fantasy,    new Color(0.3f, 0.0f, 0.5f), new Color(0.0f, 0.5f, 1f),    PatternType.Noise,      "Cosmic gradient fantasy livery");
        }

        private void Seed(string id, string name, LiveryTemplateCategory cat,
            Color primary, Color secondary, PatternType pattern, string desc)
        {
            if (_catalogue.ContainsKey(id)) return;
            _catalogue[id] = new LiveryTemplate
            {
                TemplateId    = id,
                Name          = name,
                Category      = cat,
                PrimaryColor  = primary,
                SecondaryColor = secondary,
                Pattern        = pattern,
                Description    = desc
            };
        }
    }
}
