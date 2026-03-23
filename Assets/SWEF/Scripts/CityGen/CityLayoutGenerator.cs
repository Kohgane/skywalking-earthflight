using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.CityGen
{
    /// <summary>
    /// Phase 52 — Generates the street grid and block layout for a settlement.
    ///
    /// <para>Three layout strategies are supported:
    /// <list type="bullet">
    ///   <item><see cref="LayoutStyle.Grid"/> — uniform Manhattan-style grid.</item>
    ///   <item><see cref="LayoutStyle.Organic"/> — irregular radial streets from a historic core.</item>
    ///   <item><see cref="LayoutStyle.Mixed"/> — organic center surrounded by a grid.</item>
    /// </list>
    /// </para>
    ///
    /// <para>Call <see cref="GenerateLayout"/> to receive a fully-populated
    /// <see cref="CityLayout"/> ready for building and road generation.</para>
    /// </summary>
    public class CityLayoutGenerator : MonoBehaviour
    {
        #region Constants

        private const float BlockSize         = 80f;   // metres per city block
        private const float MinorRoadWidth    = 6f;
        private const float MainRoadWidth     = 12f;
        private const float HighwayWidth      = 20f;
        private const float ParkProbability   = 0.12f;
        private const float PlazaProbability  = 0.05f;
        private const int   OrganicRayCount   = 8;     // radial streets from center

        #endregion

        #region Inspector

        [Header("Layout Defaults")]
        [Tooltip("Default block size override (0 = use constant).")]
        [SerializeField] private float blockSizeOverride = 0f;

        [Tooltip("Default park probability per block.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float parkProbability = ParkProbability;

        #endregion

        #region Public API

        /// <summary>
        /// Generates a complete <see cref="CityLayout"/> for the given
        /// <paramref name="def"/> centered at <paramref name="center"/> using
        /// <paramref name="seed"/> for deterministic randomness.
        /// </summary>
        public CityLayout GenerateLayout(SettlementDefinition def, Vector3 center, int seed)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));

            var rng    = new System.Random(seed);
            var layout = new CityLayout
            {
                definition = def,
                center     = center,
                seed       = seed
            };

            float bs = blockSizeOverride > 0f ? blockSizeOverride : BlockSize;

            layout.layoutStyle = PickLayoutStyle(def.settlementType, rng);

            switch (layout.layoutStyle)
            {
                case LayoutStyle.Grid:
                    GenerateGridLayout(layout, bs, rng);
                    break;
                case LayoutStyle.Organic:
                    GenerateOrganicLayout(layout, bs, rng);
                    break;
                default:
                    GenerateMixedLayout(layout, bs, rng);
                    break;
            }

            ApplyDensityGradient(layout, rng);
            return layout;
        }

        #endregion

        #region Grid Layout

        private void GenerateGridLayout(CityLayout layout, float bs, System.Random rng)
        {
            float radius  = layout.definition.areaRadius;
            int   extent  = Mathf.RoundToInt(radius / bs);
            Vector3 c     = layout.center;

            for (int ix = -extent; ix <= extent; ix++)
            {
                for (int iz = -extent; iz <= extent; iz++)
                {
                    Vector3 blockOrigin = c + new Vector3(ix * bs, 0f, iz * bs);
                    if ((blockOrigin - c).sqrMagnitude > radius * radius) continue;

                    bool isMainAxis = (ix % 3 == 0) || (iz % 3 == 0);
                    float rw = isMainAxis ? MainRoadWidth : MinorRoadWidth;
                    RoadType rt = isMainAxis ? RoadType.MainRoad : RoadType.Street;

                    AddGridRoads(layout, blockOrigin, bs, rw, rt);

                    var block = CreateBlock(blockOrigin, bs, rng);
                    layout.blocks.Add(block);

                    if ((ix % 3 == 0) && (iz % 3 == 0))
                        AddIntersectionPlaza(layout, blockOrigin, rng);
                }
            }

            // Outer highway ring
            AddHighwayRing(layout, radius * 1.05f, rng);
        }

        private static void AddGridRoads(CityLayout layout, Vector3 origin, float bs,
                                          float roadWidth, RoadType rt)
        {
            float h = bs * 0.5f;
            layout.roadNetwork.segments.Add(new RoadSegment
            {
                start    = origin + new Vector3(-h, 0f, -h),
                end      = origin + new Vector3( h, 0f, -h),
                width    = roadWidth,
                roadType = rt
            });
            layout.roadNetwork.segments.Add(new RoadSegment
            {
                start    = origin + new Vector3(-h, 0f, -h),
                end      = origin + new Vector3(-h, 0f,  h),
                width    = roadWidth,
                roadType = rt
            });
        }

        #endregion

        #region Organic Layout

        private void GenerateOrganicLayout(CityLayout layout, float bs, System.Random rng)
        {
            float radius = layout.definition.areaRadius;
            Vector3 c    = layout.center;

            // Radial roads from center.
            for (int r = 0; r < OrganicRayCount; r++)
            {
                float angle = r * (360f / OrganicRayCount) * Mathf.Deg2Rad;
                float wobble = (float)(rng.NextDouble() - 0.5) * 15f * Mathf.Deg2Rad;
                angle += wobble;

                Vector3 end = c + new Vector3(Mathf.Cos(angle) * radius,
                                               0f,
                                               Mathf.Sin(angle) * radius);
                layout.roadNetwork.segments.Add(new RoadSegment
                {
                    start    = c,
                    end      = end,
                    width    = MainRoadWidth,
                    roadType = RoadType.MainRoad
                });
            }

            // Concentric ring roads.
            int rings = Mathf.RoundToInt(radius / (bs * 1.5f));
            for (int ring = 1; ring <= rings; ring++)
            {
                float r = ring * bs * 1.5f;
                int segs = Mathf.Max(8, ring * 4);
                Vector3 prev = Vector3.zero;
                for (int s = 0; s <= segs; s++)
                {
                    float angle = s * (360f / segs) * Mathf.Deg2Rad;
                    float wobble = (float)(rng.NextDouble() - 0.5) * 0.1f * r;
                    Vector3 pt = c + new Vector3(
                        Mathf.Cos(angle) * r + wobble,
                        0f,
                        Mathf.Sin(angle) * r + wobble);

                    if (s > 0)
                    {
                        layout.roadNetwork.segments.Add(new RoadSegment
                        {
                            start    = prev,
                            end      = pt,
                            width    = MinorRoadWidth,
                            roadType = RoadType.Street
                        });
                    }
                    prev = pt;
                }
            }

            // Fill with irregular blocks between rings.
            int blockCount = Mathf.RoundToInt(Mathf.PI * radius * radius / (bs * bs) * 0.4f);
            for (int i = 0; i < blockCount; i++)
            {
                float angle = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                float dist  = (float)(rng.NextDouble() * radius * 0.9);
                Vector3 pos = c + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                float size  = bs * (0.5f + (float)rng.NextDouble() * 0.8f);
                layout.blocks.Add(CreateBlock(pos, size, rng));
            }
        }

        #endregion

        #region Mixed Layout

        private void GenerateMixedLayout(CityLayout layout, float bs, System.Random rng)
        {
            // Organic core (inner 40 % of radius).
            float innerRadius = layout.definition.areaRadius * 0.4f;
            var innerDef = CloneWithRadius(layout.definition, innerRadius);
            var innerLayout = new CityLayout
            {
                definition  = innerDef,
                center      = layout.center,
                seed        = layout.seed + 1,
                layoutStyle = LayoutStyle.Organic
            };
            GenerateOrganicLayout(innerLayout, bs * 0.7f, new System.Random(layout.seed + 1));
            layout.blocks.AddRange(innerLayout.blocks);
            layout.roadNetwork.segments.AddRange(innerLayout.roadNetwork.segments);
            layout.roadNetwork.intersectionPoints.AddRange(innerLayout.roadNetwork.intersectionPoints);

            // Grid outskirts.
            var outerDef = CloneWithRadius(layout.definition, layout.definition.areaRadius);
            var outerLayout = new CityLayout
            {
                definition  = outerDef,
                center      = layout.center,
                seed        = layout.seed + 2,
                layoutStyle = LayoutStyle.Grid
            };
            GenerateGridLayout(outerLayout, bs, new System.Random(layout.seed + 2));
            // Only add outer blocks (skip those inside inner radius).
            float innerSqr = innerRadius * innerRadius;
            foreach (var block in outerLayout.blocks)
                if ((block.bounds.center - layout.center).sqrMagnitude > innerSqr)
                    layout.blocks.Add(block);
            layout.roadNetwork.segments.AddRange(outerLayout.roadNetwork.segments);
        }

        #endregion

        #region Helpers

        private static LayoutStyle PickLayoutStyle(SettlementType type, System.Random rng)
        {
            return type switch
            {
                SettlementType.HistoricCenter => LayoutStyle.Organic,
                SettlementType.Village        => LayoutStyle.Organic,
                SettlementType.Hamlet         => LayoutStyle.Organic,
                SettlementType.Megacity       => LayoutStyle.Mixed,
                SettlementType.City           => rng.Next(2) == 0 ? LayoutStyle.Grid : LayoutStyle.Mixed,
                _                             => LayoutStyle.Grid
            };
        }

        private CityBlock CreateBlock(Vector3 center, float size, System.Random rng)
        {
            bool isPark  = rng.NextDouble() < parkProbability;
            bool isPlaza = !isPark && rng.NextDouble() < PlazaProbability;

            var block = new CityBlock
            {
                bounds         = new Bounds(center, new Vector3(size, 0f, size)),
                parkPercentage = isPark ? 1f : (isPlaza ? 0.5f : 0f),
                roadFrontage   = size
            };

            if (!isPark && !isPlaza)
                PopulateBlockWithBuildings(block, rng);

            return block;
        }

        private static void PopulateBlockWithBuildings(CityBlock block, System.Random rng)
        {
            int count = rng.Next(2, 8);
            for (int i = 0; i < count; i++)
            {
                block.buildings.Add(new BuildingDefinition
                {
                    buildingType = (BuildingType)(rng.Next(0, 4)),
                    minHeight    = 6f,
                    maxHeight    = 30f,
                    minWidth     = 8f,
                    maxWidth     = 20f,
                    minFloors    = 2,
                    maxFloors    = 10,
                    roofType     = RoofType.Flat,
                    materialIndex = rng.Next(0, 4)
                });
            }
        }

        private static void AddIntersectionPlaza(CityLayout layout, Vector3 pos, System.Random rng)
        {
            if (rng.NextDouble() > PlazaProbability * 3) return;
            layout.roadNetwork.intersectionPoints.Add(pos);
        }

        private void AddHighwayRing(CityLayout layout, float radius, System.Random rng)
        {
            int segs = 16;
            Vector3 c = layout.center;
            Vector3 prev = Vector3.zero;
            for (int s = 0; s <= segs; s++)
            {
                float angle = s * (360f / segs) * Mathf.Deg2Rad;
                Vector3 pt  = c + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                if (s > 0)
                {
                    layout.roadNetwork.segments.Add(new RoadSegment
                    {
                        start    = prev,
                        end      = pt,
                        width    = HighwayWidth,
                        roadType = RoadType.Highway
                    });
                }
                prev = pt;
            }
        }

        private static void ApplyDensityGradient(CityLayout layout, System.Random rng)
        {
            float radiusSqr = layout.definition.areaRadius * layout.definition.areaRadius;
            foreach (var block in layout.blocks)
            {
                float distSqr = (block.bounds.center - layout.center).sqrMagnitude;
                float t = Mathf.Clamp01(distSqr / radiusSqr); // 0 = center, 1 = edge
                // Reduce building count towards edges.
                int remove = Mathf.RoundToInt(block.buildings.Count * t * 0.5f);
                while (remove-- > 0 && block.buildings.Count > 0)
                    block.buildings.RemoveAt(rng.Next(block.buildings.Count));

                // Scale building heights: taller in center.
                foreach (var b in block.buildings)
                {
                    float scale = Mathf.Lerp(1.2f, 0.6f, t);
                    b.maxHeight *= scale;
                    b.maxHeight  = Mathf.Max(b.maxHeight, b.minHeight);
                }
            }
        }

        private static SettlementDefinition CloneWithRadius(SettlementDefinition src, float radius)
        {
            return new SettlementDefinition
            {
                settlementType    = src.settlementType,
                minPopulation     = src.minPopulation,
                maxPopulation     = src.maxPopulation,
                areaRadius        = radius,
                buildingDensity   = src.buildingDensity,
                roadDensity       = src.roadDensity,
                architectureStyle = src.architectureStyle,
                hasAirport        = src.hasAirport,
                hasWaterfront     = src.hasWaterfront
            };
        }

        #endregion
    }
}
