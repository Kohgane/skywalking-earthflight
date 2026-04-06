// RandomLiveryGenerator.cs — Phase 115: Advanced Aircraft Livery Editor
// AI-assisted random livery generation: style-based procedural design.
// Namespace: SWEF.LiveryEditor

using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Procedurally generates randomised liveries using style seeds.
    /// Can be guided by a <see cref="LiveryTemplateCategory"/> for thematic coherence.
    /// </summary>
    public class RandomLiveryGenerator : MonoBehaviour
    {
        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when a random livery has been generated.</summary>
        public event Action<LiverySaveData> OnLiveryGenerated;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Generates a fully random livery with an optional style hint.
        /// </summary>
        /// <param name="aircraftId">Target aircraft identifier.</param>
        /// <param name="category">Optional thematic category to guide colour and pattern choices.</param>
        /// <param name="seed">Random seed (0 = use system time).</param>
        /// <param name="textureResolution">Canvas resolution.</param>
        /// <returns>A new random <see cref="LiverySaveData"/>.</returns>
        public LiverySaveData Generate(string aircraftId,
            LiveryTemplateCategory category = LiveryTemplateCategory.Commercial,
            int seed = 0, int textureResolution = 2048)
        {
            if (seed != 0) Random.InitState(seed);

            Color  primary   = PickColorForCategory(category);
            Color  secondary = PickComplementaryColor(primary);
            var    pattern   = PickPatternForCategory(category);
            string styleName = PickStyleName(category);

            var metadata = new LiveryMetadata
            {
                LiveryId              = Guid.NewGuid().ToString(),
                Name                  = $"Random {styleName} {Random.Range(100, 999)}",
                Author                = "Generator",
                Description           = $"Auto-generated {category} livery.",
                FormatVersion         = 1,
                CreatedAtUtc          = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ModifiedAtUtc         = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                CompatibleAircraftIds = new List<string> { aircraftId },
                Tags                  = new List<string> { category.ToString(), "random" }
            };

            var livery = new LiverySaveData
            {
                Metadata          = metadata,
                TextureResolution = textureResolution
            };

            OnLiveryGenerated?.Invoke(livery);
            return livery;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static Color PickColorForCategory(LiveryTemplateCategory cat) => cat switch
        {
            LiveryTemplateCategory.Military  => new Color(Random.value * 0.5f, Random.value * 0.5f, 0f),
            LiveryTemplateCategory.Racing    => new Color(Random.value, Random.value * 0.2f, 0f),
            LiveryTemplateCategory.Fantasy   => Color.HSVToRGB(Random.value, 0.9f, 1f),
            LiveryTemplateCategory.Historic  => new Color(0.8f + Random.value * 0.2f, 0.7f + Random.value * 0.2f, 0.4f),
            _                                => Color.HSVToRGB(Random.value, 0.5f + Random.value * 0.5f, 0.8f + Random.value * 0.2f)
        };

        private static Color PickComplementaryColor(Color primary)
        {
            Color.RGBToHSV(primary, out float h, out float s, out float v);
            return Color.HSVToRGB((h + 0.5f) % 1f, s, v);
        }

        private static PatternType PickPatternForCategory(LiveryTemplateCategory cat) => cat switch
        {
            LiveryTemplateCategory.Military  => PatternType.Camouflage,
            LiveryTemplateCategory.Racing    => Random.value > 0.5f ? PatternType.Chevrons : PatternType.Chequered,
            LiveryTemplateCategory.Fantasy   => PatternType.Noise,
            LiveryTemplateCategory.Historic  => PatternType.Stripes,
            _                                => (PatternType)Random.Range(0, Enum.GetValues(typeof(PatternType)).Length)
        };

        private static string PickStyleName(LiveryTemplateCategory cat)
        {
            string[] commercial = { "Horizon", "Pacific", "Atlantic", "Global", "Express" };
            string[] military   = { "Raptor", "Eagle", "Thunder", "Viper", "Falcon" };
            string[] racing     = { "Blaze", "Turbo", "Sprint", "Apex", "Rocket" };
            string[] fantasy    = { "Aurora", "Nebula", "Phoenix", "Dragon", "Comet" };

            var pool = cat switch
            {
                LiveryTemplateCategory.Military => military,
                LiveryTemplateCategory.Racing   => racing,
                LiveryTemplateCategory.Fantasy  => fantasy,
                _                               => commercial
            };
            return pool[Random.Range(0, pool.Length)];
        }
    }
}
