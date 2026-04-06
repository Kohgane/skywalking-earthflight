// TemplateCustomizer.cs — Phase 115: Advanced Aircraft Livery Editor
// Template customization: swap colors, change logo placement, adjust pattern scale.
// Namespace: SWEF.LiveryEditor

using System;
using UnityEngine;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Applies customizations to a <see cref="LiveryTemplate"/> and
    /// generates a <see cref="LiverySaveData"/> ready for further editing.
    /// </summary>
    public class TemplateCustomizer : MonoBehaviour
    {
        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when a template is successfully applied to produce a livery.</summary>
        public event Action<LiverySaveData> OnTemplateApplied;

        // ── Internal state ────────────────────────────────────────────────────────
        private LiveryTemplate _sourceTemplate;
        private Color _primaryOverride;
        private Color _secondaryOverride;
        private float _patternScale = 1f;
        private bool  _colorsOverridden;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Selects a template to customise.</summary>
        public void SelectTemplate(LiveryTemplate template)
        {
            _sourceTemplate    = template ?? throw new ArgumentNullException(nameof(template));
            _primaryOverride   = template.PrimaryColor;
            _secondaryOverride = template.SecondaryColor;
            _patternScale      = 1f;
            _colorsOverridden  = false;
        }

        /// <summary>Overrides the primary colour used by the template.</summary>
        public void SetPrimaryColor(Color color)
        {
            _primaryOverride  = color;
            _colorsOverridden = true;
        }

        /// <summary>Overrides the secondary colour used by the template.</summary>
        public void SetSecondaryColor(Color color)
        {
            _secondaryOverride = color;
            _colorsOverridden  = true;
        }

        /// <summary>Sets the pattern scale multiplier (1 = default).</summary>
        public void SetPatternScale(float scale) =>
            _patternScale = Mathf.Clamp(scale, 0.1f, 10f);

        /// <summary>
        /// Generates a <see cref="LiverySaveData"/> from the current template
        /// and customisation settings.
        /// </summary>
        /// <param name="liveryName">Name for the new livery.</param>
        /// <param name="aircraftId">Target aircraft identifier.</param>
        /// <param name="textureResolution">Canvas resolution for the generated livery.</param>
        /// <returns>A new <see cref="LiverySaveData"/> built from the template.</returns>
        public LiverySaveData Apply(string liveryName, string aircraftId, int textureResolution = 2048)
        {
            if (_sourceTemplate == null)
                throw new InvalidOperationException("No template selected. Call SelectTemplate first.");

            Color primary   = _colorsOverridden ? _primaryOverride   : _sourceTemplate.PrimaryColor;
            Color secondary = _colorsOverridden ? _secondaryOverride : _sourceTemplate.SecondaryColor;

            // Generate the base pattern texture.
            var pattern = PatternGenerator.Generate(
                _sourceTemplate.Pattern,
                textureResolution, textureResolution,
                primary, secondary,
                _patternScale);

            var metadata = new LiveryMetadata
            {
                LiveryId         = Guid.NewGuid().ToString(),
                Name             = liveryName,
                Author           = "Player",
                Description      = $"Based on template: {_sourceTemplate.Name}",
                FormatVersion    = 1,
                CreatedAtUtc     = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ModifiedAtUtc    = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                CompatibleAircraftIds = new System.Collections.Generic.List<string> { aircraftId },
                Tags             = new System.Collections.Generic.List<string> { _sourceTemplate.Category.ToString() }
            };

            var livery = new LiverySaveData
            {
                Metadata          = metadata,
                TextureResolution = textureResolution
            };

            OnTemplateApplied?.Invoke(livery);
            return livery;
        }

        /// <summary>Returns the currently selected template, or <c>null</c>.</summary>
        public LiveryTemplate SourceTemplate => _sourceTemplate;

        /// <summary>Current primary colour override.</summary>
        public Color PrimaryOverride => _primaryOverride;

        /// <summary>Current secondary colour override.</summary>
        public Color SecondaryOverride => _secondaryOverride;

        /// <summary>Current pattern scale.</summary>
        public float PatternScale => _patternScale;
    }
}
