using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ReplayTheater
{
    /// <summary>
    /// MonoBehaviour responsible for post-processing effects applied to replay clips.
    /// Handles colour grading presets, slow-motion speed segments, text overlays,
    /// individual effect toggles (vignette, bloom, film-grain), and picture-in-picture.
    /// </summary>
    public class ReplayEffectsProcessor : MonoBehaviour
    {
        #region Inspector

        [Header("Color Grading")]
        [SerializeField] private ColorGradingPreset activePreset = ColorGradingPreset.None;

        [Header("Overlay Settings")]
        [SerializeField] private bool vignetteEnabled;
        [SerializeField] private bool bloomEnabled;
        [SerializeField] private bool filmGrainEnabled;

        [Header("PiP Settings")]
        [SerializeField] private bool          pipEnabled;
        [SerializeField] private RenderTexture pipTexture;

        #endregion

        #region State

        private float _playbackSpeedMultiplier = 1f;
        private List<(float start, float end, float speed)> _speedSegments
            = new List<(float, float, float)>();

        private readonly List<(string text, float startTime, float duration, Vector2 position)> _textOverlays
            = new List<(string, float, float, Vector2)>();

        #endregion

        #region Properties

        /// <summary>The colour-grading preset currently applied.</summary>
        public ColorGradingPreset ActivePreset => activePreset;

        /// <summary>Whether picture-in-picture is currently enabled.</summary>
        public bool IsPiPEnabled => pipEnabled;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Debug.Log("[SWEF] ReplayEffectsProcessor: Initialised.");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Switches the active colour-grading look preset.
        /// </summary>
        /// <param name="preset">Preset to apply.</param>
        public void SetColorGrading(ColorGradingPreset preset)
        {
            activePreset = preset;
            Debug.Log($"[SWEF] ReplayEffectsProcessor: Color grading → {preset}.");
        }

        /// <summary>
        /// Registers a slow-motion (or fast-forward) speed segment for a time range.
        /// </summary>
        /// <param name="startTime">Start of the segment in seconds.</param>
        /// <param name="endTime">End of the segment in seconds.</param>
        /// <param name="speed">Playback speed multiplier for the segment.</param>
        public void SetSlowMotion(float startTime, float endTime, float speed)
        {
            if (startTime >= endTime)
            {
                Debug.LogWarning("[SWEF] ReplayEffectsProcessor: SetSlowMotion — startTime must be less than endTime.");
                return;
            }
            _speedSegments.Add((startTime, endTime, Mathf.Max(0.01f, speed)));
            Debug.Log($"[SWEF] ReplayEffectsProcessor: Slow-motion [{startTime:F2}, {endTime:F2}] @ {speed}x added.");
        }

        /// <summary>
        /// Adds a text overlay that appears during a given time window.
        /// </summary>
        /// <param name="text">Text content to display.</param>
        /// <param name="startTime">Time at which the overlay appears (seconds).</param>
        /// <param name="duration">How long the overlay remains visible (seconds).</param>
        /// <param name="position">Normalised screen-space position (0–1 in both axes).</param>
        public void AddTextOverlay(string text, float startTime, float duration, Vector2 position)
        {
            _textOverlays.Add((text, startTime, duration, position));
            Debug.Log($"[SWEF] ReplayEffectsProcessor: Text overlay '{text}' added at t={startTime:F2}.");
        }

        /// <summary>Enables or disables the vignette effect.</summary>
        /// <param name="enable"><c>true</c> to enable; <c>false</c> to disable.</param>
        public void EnableVignette(bool enable)
        {
            vignetteEnabled = enable;
            Debug.Log($"[SWEF] ReplayEffectsProcessor: Vignette → {enable}.");
        }

        /// <summary>Enables or disables the bloom effect.</summary>
        /// <param name="enable"><c>true</c> to enable; <c>false</c> to disable.</param>
        public void EnableBloom(bool enable)
        {
            bloomEnabled = enable;
            Debug.Log($"[SWEF] ReplayEffectsProcessor: Bloom → {enable}.");
        }

        /// <summary>Enables or disables the film-grain overlay.</summary>
        /// <param name="enable"><c>true</c> to enable; <c>false</c> to disable.</param>
        public void EnableFilmGrain(bool enable)
        {
            filmGrainEnabled = enable;
            Debug.Log($"[SWEF] ReplayEffectsProcessor: Film grain → {enable}.");
        }

        /// <summary>Enables or disables the picture-in-picture overlay.</summary>
        /// <param name="enable"><c>true</c> to enable; <c>false</c> to disable.</param>
        public void EnablePictureInPicture(bool enable)
        {
            pipEnabled = enable;
            Debug.Log($"[SWEF] ReplayEffectsProcessor: PiP → {enable}.");
        }

        /// <summary>
        /// Bakes the current effects configuration into the clip's effect list.
        /// </summary>
        /// <param name="clip">The <see cref="ReplayClip"/> to update.</param>
        public void ApplyEffectsToClip(ReplayClip clip)
        {
            if (clip == null) return;

            clip.effects.Clear();

            if (activePreset != ColorGradingPreset.None)
                clip.effects.Add($"ColorGrading:{activePreset}");
            if (vignetteEnabled)
                clip.effects.Add("Vignette");
            if (bloomEnabled)
                clip.effects.Add("Bloom");
            if (filmGrainEnabled)
                clip.effects.Add("FilmGrain");
            if (pipEnabled)
                clip.effects.Add("PiP");

            foreach (var seg in _speedSegments)
                clip.effects.Add($"SlowMo:{seg.start:F3}-{seg.end:F3}@{seg.speed:F2}");

            foreach (var ov in _textOverlays)
                clip.effects.Add($"Text:{ov.startTime:F3}+{ov.duration:F3}:{ov.text}");

            Debug.Log($"[SWEF] ReplayEffectsProcessor: {clip.effects.Count} effects baked into clip '{clip.clipId}'.");
        }

        /// <summary>Clears all effects, overlays, and speed segments from this processor.</summary>
        public void ClearEffects()
        {
            activePreset             = ColorGradingPreset.None;
            vignetteEnabled          = false;
            bloomEnabled             = false;
            filmGrainEnabled         = false;
            pipEnabled               = false;
            _playbackSpeedMultiplier = 1f;
            _speedSegments.Clear();
            _textOverlays.Clear();
            Debug.Log("[SWEF] ReplayEffectsProcessor: All effects cleared.");
        }

        #endregion
    }
}
