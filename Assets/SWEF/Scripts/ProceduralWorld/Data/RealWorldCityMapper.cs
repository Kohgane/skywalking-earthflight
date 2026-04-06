// RealWorldCityMapper.cs — Phase 113: Procedural City & Airport Generation
// Map real-world city locations to procedural parameters
// (population → density, climate → building style).
// Namespace: SWEF.ProceduralWorld

using UnityEngine;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// Maps real-world city attributes such as population and climate zone
    /// to procedural generation parameters like density and building style.
    /// </summary>
    public class RealWorldCityMapper : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Population Mapping")]
        [Tooltip("Population above which a city is classified as Metropolis.")]
        [SerializeField] private int metropolisThreshold = 1_000_000;

        [Tooltip("Population above which a city is classified as Town.")]
        [SerializeField] private int townThreshold = 50_000;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Derives a <see cref="CityType"/> from real-world population count.
        /// </summary>
        public CityType CityTypeFromPopulation(int population)
        {
            if (population >= metropolisThreshold) return CityType.Metropolis;
            if (population >= townThreshold) return CityType.Town;
            return CityType.Village;
        }

        /// <summary>
        /// Converts population to a normalised building density [0..1].
        /// </summary>
        public float DensityFromPopulation(int population)
        {
            float norm = Mathf.Log10(Mathf.Max(1, population)) / Mathf.Log10(10_000_000f);
            return Mathf.Clamp01(norm);
        }

        /// <summary>
        /// Maps a climate zone name to a descriptive building style tag used
        /// when selecting prefab variants.
        /// </summary>
        public string BuildingStyleFromClimate(string climateZone)
        {
            if (string.IsNullOrEmpty(climateZone)) return "Temperate";
            return climateZone.ToLower() switch
            {
                "tropical" => "Tropical",
                "desert"   => "Desert",
                "arctic"   => "Arctic",
                "oceanic"  => "Oceanic",
                _          => "Temperate"
            };
        }

        /// <summary>
        /// Builds a complete <see cref="CityDescription"/> seeded from real-world data.
        /// </summary>
        public CityDescription MapRealCity(
            string name, int population, Vector3 worldPosition, bool isCoastal, string climateZone)
        {
            int seed = Mathf.Abs(name.GetHashCode());
            return new CityDescription
            {
                seed = seed,
                cityName = name,
                cityType = isCoastal ? CityType.Coastal : CityTypeFromPopulation(population),
                centre = worldPosition,
                radiusMetres = Mathf.Lerp(500f, 20000f, DensityFromPopulation(population)),
                population = population
            };
        }
    }
}
