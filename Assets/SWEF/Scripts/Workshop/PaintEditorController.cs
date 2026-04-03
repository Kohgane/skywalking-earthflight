// PaintEditorController.cs — SWEF Aircraft Workshop & Part Customization (Phase 90)
using UnityEngine;

namespace SWEF.Workshop
{
    /// <summary>
    /// MonoBehaviour that drives the paint / livery editor panel in the Workshop UI.
    /// Provides colour pickers for primary / secondary / accent channels, metallic
    /// and roughness sliders, pattern selection, and live preview on the aircraft model.
    ///
    /// <para>
    /// Attach to the same GameObject as the Workshop UI canvas root.
    /// </para>
    /// </summary>
    public class PaintEditorController : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Preview")]
        [Tooltip("Renderer on the aircraft preview model whose material will be updated in real time.")]
        [SerializeField] private Renderer _previewRenderer;

        [Header("Shader Property Names")]
        [Tooltip("Shader property name for the primary colour channel.")]
        [SerializeField] private string _primaryColorProp   = "_PrimaryColor";

        [Tooltip("Shader property name for the secondary colour channel.")]
        [SerializeField] private string _secondaryColorProp = "_SecondaryColor";

        [Tooltip("Shader property name for the accent colour channel.")]
        [SerializeField] private string _accentColorProp    = "_AccentColor";

        [Tooltip("Shader property name for the metallic channel.")]
        [SerializeField] private string _metallicProp       = "_Metallic";

        [Tooltip("Shader property name for the roughness/smoothness channel.")]
        [SerializeField] private string _roughnessProp      = "_Roughness";

        // ── State ──────────────────────────────────────────────────────────────

        private PaintSchemeData _currentScheme = new PaintSchemeData();

        // ── Unity Lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (WorkshopManager.Instance != null)
            {
                var build = WorkshopManager.Instance.ActiveBuild;
                if (build?.paintScheme != null)
                    _currentScheme = build.paintScheme;
            }
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Applies the current in-editor scheme to the aircraft preview model and
        /// writes it back into the active build.
        /// </summary>
        public void ApplyPaintScheme()
        {
            UpdatePreviewMaterial();

            if (WorkshopManager.Instance?.ActiveBuild == null)
            {
                Debug.LogWarning("[SWEF] Workshop: ApplyPaintScheme — no active build.");
                return;
            }

            WorkshopManager.Instance.ActiveBuild.paintScheme = _currentScheme;
            WorkshopAnalytics.RecordPaintApplied(_currentScheme.pattern.ToString());
        }

        /// <summary>
        /// Resets the current paint scheme to the default white/grey values and
        /// refreshes the preview.
        /// </summary>
        public void ResetToDefault()
        {
            _currentScheme = new PaintSchemeData();
            UpdatePreviewMaterial();
        }

        /// <summary>
        /// Persists the current scheme by saving the active build.
        /// </summary>
        public void SaveScheme()
        {
            ApplyPaintScheme();
            WorkshopManager.Instance?.SaveBuild();
        }

        /// <summary>
        /// Loads the paint scheme from the currently active build into the editor.
        /// </summary>
        public void LoadScheme()
        {
            var build = WorkshopManager.Instance?.ActiveBuild;
            if (build == null)
            {
                Debug.LogWarning("[SWEF] Workshop: LoadScheme — no active build.");
                return;
            }

            _currentScheme = build.paintScheme ?? new PaintSchemeData();
            UpdatePreviewMaterial();
        }

        /// <summary>Sets the primary colour channel and refreshes the preview.</summary>
        /// <param name="color">New primary colour.</param>
        public void SetPrimaryColor(Color color)
        {
            _currentScheme.primaryColor = color;
            UpdatePreviewMaterial();
        }

        /// <summary>Sets the secondary colour channel and refreshes the preview.</summary>
        /// <param name="color">New secondary colour.</param>
        public void SetSecondaryColor(Color color)
        {
            _currentScheme.secondaryColor = color;
            UpdatePreviewMaterial();
        }

        /// <summary>Sets the accent colour channel and refreshes the preview.</summary>
        /// <param name="color">New accent colour.</param>
        public void SetAccentColor(Color color)
        {
            _currentScheme.accentColor = color;
            UpdatePreviewMaterial();
        }

        /// <summary>Sets the metallic value [0–1] and refreshes the preview.</summary>
        /// <param name="value">Metallic factor (0 = matte, 1 = mirror).</param>
        public void SetMetallic(float value)
        {
            _currentScheme.metallic = Mathf.Clamp01(value);
            UpdatePreviewMaterial();
        }

        /// <summary>Sets the roughness value [0–1] and refreshes the preview.</summary>
        /// <param name="value">Roughness factor (0 = smooth, 1 = rough).</param>
        public void SetRoughness(float value)
        {
            _currentScheme.roughness = Mathf.Clamp01(value);
            UpdatePreviewMaterial();
        }

        /// <summary>Selects a livery pattern and refreshes the preview.</summary>
        /// <param name="pattern">Pattern to apply.</param>
        public void SetPattern(PaintPattern pattern)
        {
            _currentScheme.pattern = pattern;
            UpdatePreviewMaterial();
        }

        /// <summary>Returns the scheme currently being edited.</summary>
        public PaintSchemeData GetCurrentScheme() => _currentScheme;

        // ── Private ────────────────────────────────────────────────────────────

        private void UpdatePreviewMaterial()
        {
            if (_previewRenderer == null) return;

            var mat = _previewRenderer.material;
            if (mat == null) return;

            if (mat.HasProperty(_primaryColorProp))   mat.SetColor(_primaryColorProp,   _currentScheme.primaryColor);
            if (mat.HasProperty(_secondaryColorProp)) mat.SetColor(_secondaryColorProp, _currentScheme.secondaryColor);
            if (mat.HasProperty(_accentColorProp))    mat.SetColor(_accentColorProp,    _currentScheme.accentColor);
            if (mat.HasProperty(_metallicProp))       mat.SetFloat(_metallicProp,       _currentScheme.metallic);
            if (mat.HasProperty(_roughnessProp))      mat.SetFloat(_roughnessProp,      _currentScheme.roughness);
        }
    }
}
