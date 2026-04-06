// CityGenerator.cs — Phase 113: Procedural City & Airport Generation
// Procedural city layout generator using noise-based zoning.
// Namespace: SWEF.ProceduralWorld

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// Generates a complete <see cref="CityLayout"/> and <see cref="CityDescription"/>
    /// using Perlin-noise-based zoning: commercial centre, residential suburbs,
    /// and industrial districts placed according to density and noise sampling.
    /// </summary>
    public class CityGenerator : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("City Names")]
        [SerializeField] private string[] cityNamePrefixes = { "New", "North", "South", "East", "West", "Port" };
        [SerializeField] private string[] cityNameSuffixes = { "ville", "ton", "burg", "haven", "ford", "field" };

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Generates a <see cref="CityDescription"/> centred at <paramref name="worldPosition"/>
        /// using a deterministic <paramref name="seed"/>.
        /// </summary>
        public CityDescription Generate(Vector3 worldPosition, int seed, ProceduralWorldConfig cfg)
        {
            var rng = new System.Random(seed);
            var layout = GenerateLayout(worldPosition, seed, cfg, rng);
            var city = new CityDescription
            {
                seed = seed,
                cityName = GenerateName(rng),
                cityType = DetermineType(layout),
                centre = worldPosition,
                radiusMetres = layout.radiusMetres,
                population = EstimatePopulation(layout, rng)
            };

            PlaceBuildings(city, layout, cfg, rng);
            return city;
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private CityLayout GenerateLayout(Vector3 centre, int seed, ProceduralWorldConfig cfg, System.Random rng)
        {
            int blocksX = Mathf.RoundToInt(cfg.roadGridDensity * 4);
            int blocksZ = Mathf.RoundToInt(cfg.roadGridDensity * 4);
            float blockW = 80f;
            float blockD = 60f;
            float radius = blocksX * blockW * 0.5f;

            var layout = new CityLayout
            {
                seed = seed,
                centre = centre,
                radiusMetres = radius,
                cityType = CityType.Town,
                blocksX = blocksX,
                blocksZ = blocksZ,
                blockSize = new Vector2(blockW, blockD),
                zoneMap = new int[blocksX, blocksZ],
                densityMap = new float[blocksX, blocksZ]
            };

            float offsetX = (float)rng.NextDouble() * 1000f;
            float offsetZ = (float)rng.NextDouble() * 1000f;

            for (int bx = 0; bx < blocksX; bx++)
            {
                for (int bz = 0; bz < blocksZ; bz++)
                {
                    float nx = (float)bx / blocksX + offsetX;
                    float nz = (float)bz / blocksZ + offsetZ;
                    float noise = Mathf.PerlinNoise(nx * cfg.noiseScale * 1000f, nz * cfg.noiseScale * 1000f);

                    // Distance from centre for radial zoning
                    float dx = (float)(bx - blocksX / 2) / (blocksX / 2);
                    float dz = (float)(bz - blocksZ / 2) / (blocksZ / 2);
                    float dist = Mathf.Sqrt(dx * dx + dz * dz);

                    // Zone assignment: centre = commercial, mid = residential, edge = industrial
                    BuildingType zone;
                    if (dist < 0.3f) zone = BuildingType.Commercial;
                    else if (dist < 0.7f) zone = BuildingType.Residential;
                    else zone = BuildingType.Industrial;

                    // Occasional landmark
                    if (noise > 0.9f && dist < 0.4f) zone = BuildingType.Landmark;
                    if (noise < 0.1f && dist < 0.2f) zone = BuildingType.Government;

                    layout.zoneMap[bx, bz] = (int)zone;
                    layout.densityMap[bx, bz] = Mathf.Clamp01(1f - dist + noise * 0.3f);
                }
            }

            BuildRoadGrid(layout, centre, cfg);
            return layout;
        }

        private void BuildRoadGrid(CityLayout layout, Vector3 centre, ProceduralWorldConfig cfg)
        {
            float startX = centre.x - layout.blocksX * layout.blockSize.x * 0.5f;
            float startZ = centre.z - layout.blocksZ * layout.blockSize.y * 0.5f;

            // Horizontal roads
            for (int bz = 0; bz <= layout.blocksZ; bz++)
            {
                float z = startZ + bz * layout.blockSize.y;
                var seg = new RoadSegmentData
                {
                    start = new Vector3(startX, centre.y, z),
                    end = new Vector3(startX + layout.blocksX * layout.blockSize.x, centre.y, z),
                    roadType = bz == 0 || bz == layout.blocksZ ? RoadType.MainRoad : RoadType.SideStreet,
                    widthMetres = bz == 0 || bz == layout.blocksZ ? 12f : 6f
                };
                layout.roadSegments.Add(seg);
            }

            // Vertical roads
            for (int bx = 0; bx <= layout.blocksX; bx++)
            {
                float x = startX + bx * layout.blockSize.x;
                var seg = new RoadSegmentData
                {
                    start = new Vector3(x, centre.y, startZ),
                    end = new Vector3(x, centre.y, startZ + layout.blocksZ * layout.blockSize.y),
                    roadType = bx == 0 || bx == layout.blocksX ? RoadType.MainRoad : RoadType.SideStreet,
                    widthMetres = bx == 0 || bx == layout.blocksX ? 12f : 6f
                };
                layout.roadSegments.Add(seg);
            }
        }

        private void PlaceBuildings(CityDescription city, CityLayout layout, ProceduralWorldConfig cfg, System.Random rng)
        {
            float startX = city.centre.x - layout.blocksX * layout.blockSize.x * 0.5f;
            float startZ = city.centre.z - layout.blocksZ * layout.blockSize.y * 0.5f;

            for (int bx = 0; bx < layout.blocksX; bx++)
            {
                for (int bz = 0; bz < layout.blocksZ; bz++)
                {
                    float density = layout.GetDensityAt(bx, bz) * cfg.generationDensity;
                    if ((float)rng.NextDouble() > density) continue;

                    var zone = layout.GetZoneAt(bx, bz);
                    int floors = zone switch
                    {
                        BuildingType.Commercial => rng.Next(cfg.minFloors * 3, cfg.maxFloors + 1),
                        BuildingType.Industrial => rng.Next(1, 5),
                        BuildingType.Landmark  => rng.Next(cfg.maxFloors / 2, cfg.maxFloors + 1),
                        _                       => rng.Next(cfg.minFloors, cfg.maxFloors / 4 + 1)
                    };

                    float px = startX + (bx + 0.5f) * layout.blockSize.x + (float)(rng.NextDouble() - 0.5) * layout.blockSize.x * 0.4f;
                    float pz = startZ + (bz + 0.5f) * layout.blockSize.y + (float)(rng.NextDouble() - 0.5) * layout.blockSize.y * 0.4f;

                    city.buildings.Add(new BuildingInstance(new Vector3(px, city.centre.y, pz), zone, floors));
                }
            }
        }

        private CityType DetermineType(CityLayout layout)
        {
            float avgDensity = 0f;
            for (int bx = 0; bx < layout.blocksX; bx++)
                for (int bz = 0; bz < layout.blocksZ; bz++)
                    avgDensity += layout.densityMap[bx, bz];
            avgDensity /= layout.TotalPlots;

            if (avgDensity > 0.8f) return CityType.Metropolis;
            if (avgDensity > 0.5f) return CityType.Town;
            return CityType.Village;
        }

        private int EstimatePopulation(CityLayout layout, System.Random rng)
        {
            int basePopulation = layout.TotalPlots * 50;
            return Mathf.RoundToInt(basePopulation * (0.5f + (float)rng.NextDouble()));
        }

        private string GenerateName(System.Random rng)
        {
            string prefix = cityNamePrefixes.Length > 0
                ? cityNamePrefixes[rng.Next(cityNamePrefixes.Length)]
                : "Port";
            string suffix = cityNameSuffixes.Length > 0
                ? cityNameSuffixes[rng.Next(cityNameSuffixes.Length)]
                : "ville";
            return $"{prefix}{suffix}";
        }
    }
}
