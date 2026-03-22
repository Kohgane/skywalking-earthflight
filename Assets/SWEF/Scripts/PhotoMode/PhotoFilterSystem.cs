using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.PhotoMode
{
    /// <summary>
    /// Applies real-time post-processing filters to the viewfinder and captured photos.
    /// Supports 12 built-in presets, adjustable per-filter intensity, LUT-based colour
    /// grading, a before/after comparison swipe, and custom filter presets.
    /// </summary>
    public class PhotoFilterSystem : MonoBehaviour
    {
        #region Constants
        private const float DefaultIntensity = 1f;
        private const int   MaxCustomFilters = 20;
        #endregion

        #region Inspector
        [Header("References (auto-found if null)")]
        [Tooltip("PhotoCameraController to read base settings from.")]
        [SerializeField] private PhotoCameraController cameraController;

        [Header("LUT Textures (optional — leave null to skip LUT grading)")]
        [Tooltip("Array of LUT textures indexed by PhotoFilter enum value.")]
        [SerializeField] private Texture2D[] filterLUTs;
        #endregion

        #region Public properties
        /// <summary>Currently active filter.</summary>
        public PhotoFilter ActiveFilter { get; private set; } = PhotoFilter.None;

        /// <summary>Intensity of the active filter (0–1).</summary>
        public float ActiveIntensity { get; private set; } = DefaultIntensity;

        /// <summary>Read-only collection of starred (favourite) filters.</summary>
        public IReadOnlyList<PhotoFilter> FavoriteFilters => _favorites;
        #endregion

        #region Private state
        private readonly List<PhotoFilter>          _favorites      = new List<PhotoFilter>();
        private readonly Dictionary<string, string> _customPresets  = new Dictionary<string, string>(); // name → JSON
        private bool _comparisonMode;
        #endregion

        #region Unity lifecycle
        private void Awake()
        {
            if (cameraController == null)
                cameraController = FindObjectOfType<PhotoCameraController>();
        }
        #endregion

        #region Public API
        /// <summary>
        /// Applies <paramref name="filter"/> at <paramref name="intensity"/> and updates the
        /// viewfinder in real time.
        /// </summary>
        /// <param name="filter">Filter preset to apply.</param>
        /// <param name="intensity">Blend intensity, clamped to [0, 1].</param>
        public void ApplyFilter(PhotoFilter filter, float intensity = DefaultIntensity)
        {
            ActiveFilter    = filter;
            ActiveIntensity = Mathf.Clamp01(intensity);

            ApplyFilterEffects(filter, ActiveIntensity);

            if (cameraController != null)
                cameraController.Settings.filter = filter;
        }

        /// <summary>
        /// Removes any active filter and restores neutral post-processing.
        /// </summary>
        public void RemoveFilter()
        {
            ApplyFilter(PhotoFilter.None, 0f);
        }

        /// <summary>
        /// Serialises the current post-processing settings as a named custom preset.
        /// </summary>
        /// <param name="name">Unique display name for the preset.</param>
        public void SaveCustomFilter(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (_customPresets.Count >= MaxCustomFilters)
            {
                Debug.LogWarning("[PhotoFilterSystem] Maximum custom filter limit reached.");
                return;
            }

            string json = cameraController != null
                ? JsonUtility.ToJson(cameraController.Settings)
                : "{}";
            _customPresets[name] = json;
        }

        /// <summary>
        /// Loads a previously saved custom filter preset by name.
        /// </summary>
        /// <param name="name">Name of the preset to load.</param>
        public void LoadCustomFilter(string name)
        {
            if (!_customPresets.TryGetValue(name, out string json)) return;
            if (cameraController == null) return;

            CameraSettings snapshot = JsonUtility.FromJson<CameraSettings>(json);
            if (snapshot != null)
                cameraController.ApplySettings(snapshot);
        }

        /// <summary>
        /// Generates a small thumbnail texture representing what <paramref name="filter"/>
        /// would look like applied to the current frame.
        /// </summary>
        /// <param name="filter">Filter to preview.</param>
        /// <returns>A 64×64 tinted <see cref="Texture2D"/> preview, or null.</returns>
        public Texture2D GetFilterPreview(PhotoFilter filter)
        {
            // In a full implementation this would render an off-screen pass.
            // Returns a tinted solid-colour placeholder here.
            Color tint = GetFilterTintPreview(filter);
            Texture2D tex = new Texture2D(64, 64, TextureFormat.RGB24, false);
            Color[] pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = tint;
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Toggles a filter in the favourites list.
        /// </summary>
        /// <param name="filter">Filter to star or unstar.</param>
        public void ToggleFavorite(PhotoFilter filter)
        {
            if (_favorites.Contains(filter))
                _favorites.Remove(filter);
            else
                _favorites.Add(filter);
        }

        /// <summary>
        /// Enables or disables before/after comparison mode (swipe to compare).
        /// </summary>
        /// <param name="enable">True to enable comparison.</param>
        public void SetComparisonMode(bool enable)
        {
            _comparisonMode = enable;
        }
        #endregion

        #region Private helpers
        private void ApplyFilterEffects(PhotoFilter filter, float intensity)
        {
            // In a full URP implementation these parameters are written to
            // a Volume with ColorAdjustments, Vignette, ChromaticAberration,
            // Bloom, LensDistortion and FilmGrain overrides.
            // This method is a documented stub that shows intent.
            switch (filter)
            {
                case PhotoFilter.None:        break;
                case PhotoFilter.Vintage:     ApplyVintage(intensity);     break;
                case PhotoFilter.Noir:        ApplyNoir(intensity);        break;
                case PhotoFilter.Warm:        ApplyWarm(intensity);        break;
                case PhotoFilter.Cool:        ApplyCool(intensity);        break;
                case PhotoFilter.HDR:         ApplyHDR(intensity);         break;
                case PhotoFilter.Cinematic:   ApplyCinematic(intensity);   break;
                case PhotoFilter.Sunset:      ApplySunset(intensity);      break;
                case PhotoFilter.NightVision: ApplyNightVision(intensity); break;
                case PhotoFilter.Sketch:      ApplySketch(intensity);      break;
                case PhotoFilter.Tiltshift:   ApplyTiltshift(intensity);   break;
                case PhotoFilter.Bokeh:       ApplyBokeh(intensity);       break;
            }
        }

        // ── Per-filter stubs ──────────────────────────────────────────────────────
        private void ApplyVintage(float t)     { /* warm tone + vignette + grain */ }
        private void ApplyNoir(float t)        { /* desaturate + high contrast + grain */ }
        private void ApplyWarm(float t)        { /* shift colour temperature warmer */ }
        private void ApplyCool(float t)        { /* shift colour temperature cooler */ }
        private void ApplyHDR(float t)         { /* boost bloom + local contrast */ }
        private void ApplyCinematic(float t)   { /* crop bars + colour grade */ }
        private void ApplySunset(float t)      { /* orange/red tones + soft bloom */ }
        private void ApplyNightVision(float t) { /* green monochrome + noise */ }
        private void ApplySketch(float t)      { /* edge detection overlay */ }
        private void ApplyTiltshift(float t)   { /* radial DoF gradient blur */ }
        private void ApplyBokeh(float t)       { /* strong aperture DoF */ }

        private static Color GetFilterTintPreview(PhotoFilter filter)
        {
            switch (filter)
            {
                case PhotoFilter.Vintage:     return new Color(0.9f, 0.8f, 0.5f);
                case PhotoFilter.Noir:        return new Color(0.3f, 0.3f, 0.3f);
                case PhotoFilter.Warm:        return new Color(1.0f, 0.8f, 0.6f);
                case PhotoFilter.Cool:        return new Color(0.6f, 0.8f, 1.0f);
                case PhotoFilter.HDR:         return new Color(0.9f, 1.0f, 0.9f);
                case PhotoFilter.Cinematic:   return new Color(0.5f, 0.4f, 0.7f);
                case PhotoFilter.Sunset:      return new Color(1.0f, 0.6f, 0.3f);
                case PhotoFilter.NightVision: return new Color(0.2f, 0.8f, 0.2f);
                case PhotoFilter.Sketch:      return new Color(0.9f, 0.9f, 0.9f);
                case PhotoFilter.Tiltshift:   return new Color(0.7f, 0.9f, 1.0f);
                case PhotoFilter.Bokeh:       return new Color(0.8f, 0.7f, 1.0f);
                default:                      return Color.grey;
            }
        }
        #endregion
    }
}
