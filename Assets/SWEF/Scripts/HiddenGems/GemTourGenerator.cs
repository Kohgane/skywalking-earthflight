using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SWEF.GuidedTour;

namespace SWEF.HiddenGems
{
    /// <summary>
    /// Static utility that generates <see cref="TourData"/> ScriptableObjects from
    /// hidden gem selections.  Call <see cref="TourManager.StartTour"/> with the
    /// returned object to begin the tour immediately.
    /// </summary>
    public static class GemTourGenerator
    {
        // ── Constants ─────────────────────────────────────────────────────────────
        private const float DefaultTriggerRadius  = 300f;
        private const float DefaultStayDuration   = 5f;
        private const float AverageFlightSpeedKmH = 400f; // used for time estimate

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a tour visiting all discovered (or all) gems on the given continent
        /// in an optimised order using the nearest-neighbour TSP heuristic.
        /// </summary>
        public static TourData GenerateContinentTour(GemContinent continent)
        {
            var mgr  = HiddenGemManager.Instance;
            var gems = mgr != null
                ? mgr.GetGemsByContinent(continent)
                : HiddenGemDatabase.GetAllGems().Where(g => g.continent == continent).ToList();

            string id = $"tour_continent_{continent.ToString().ToLowerInvariant()}";
            return BuildTour(id, $"gem_tour_continent", gems);
        }

        /// <summary>
        /// Creates a tour that hunts all gems of the specified rarity.
        /// </summary>
        public static TourData GenerateRarityHunt(GemRarity rarity)
        {
            var mgr  = HiddenGemManager.Instance;
            var gems = mgr != null
                ? mgr.GetGemsByRarity(rarity)
                : HiddenGemDatabase.GetAllGems().Where(g => g.rarity == rarity).ToList();

            string id = $"tour_rarity_{rarity.ToString().ToLowerInvariant()}";
            return BuildTour(id, $"gem_tour_rarity", gems);
        }

        /// <summary>
        /// Creates a tour of gems within <paramref name="radiusKm"/> km of
        /// <paramref name="playerPos"/>, limited to at most <paramref name="maxStops"/> stops.
        /// </summary>
        public static TourData GenerateNearbyTour(Vector3 playerPos, float radiusKm, int maxStops = 10)
        {
            float radiusUnits = radiusKm * 1000f;
            var   all         = HiddenGemDatabase.GetAllGems();

            var nearby = all
                .Where(g =>
                {
                    float d = Vector3.Distance(playerPos, HiddenGemManager.GetWorldPosition(g));
                    return d <= radiusUnits;
                })
                .OrderBy(g => Vector3.Distance(playerPos, HiddenGemManager.GetWorldPosition(g)))
                .Take(maxStops)
                .ToList();

            return BuildTour("tour_nearby", "gem_tour_nearby", nearby);
        }

        /// <summary>
        /// Creates a tour visiting a custom list of gems identified by their <c>gemId</c>.
        /// The order of the input list is preserved.
        /// </summary>
        public static TourData GenerateCustomTour(List<string> gemIds)
        {
            var db   = HiddenGemDatabase.GetAllGems().ToDictionary(g => g.gemId);
            var gems = gemIds
                .Where(id => db.ContainsKey(id))
                .Select(id => db[id])
                .ToList();

            return BuildTour("tour_custom", "gem_tour_custom", gems);
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private static TourData BuildTour(string tourId, string locKey,
                                          List<HiddenGemDefinition> gems)
        {
            var ordered = NearestNeighbour(gems);

            var tour       = ScriptableObject.CreateInstance<TourData>();
            tour.tourId    = tourId;
            tour.tourName  = locKey;
            tour.region    = "Hidden Gems";

            float totalDist = 0f;
            var   waypoints = new List<TourData.WaypointData>();

            Vector3? prev = null;
            foreach (var gem in ordered)
            {
                Vector3 pos = HiddenGemManager.GetWorldPosition(gem);
                pos.y = gem.altitudeHint;

                if (prev.HasValue)
                    totalDist += Vector3.Distance(prev.Value, pos);

                waypoints.Add(new TourData.WaypointData
                {
                    position            = pos,
                    lookAtTarget        = pos,
                    waypointName        = gem.nameKey,
                    narrationKey        = gem.descriptionKey,
                    stayDurationSeconds = DefaultStayDuration,
                    triggerRadius       = DefaultTriggerRadius
                });
                prev = pos;
            }

            tour.waypoints = waypoints;

            // Estimate duration: distance (km) / speed (km/h) * 60 = minutes
            float distKm = totalDist / 1000f;
            tour.estimatedDurationMinutes = (distKm / AverageFlightSpeedKmH) * 60f;

            return tour;
        }

        /// <summary>
        /// Nearest-neighbour TSP heuristic starting from the first gem.
        /// </summary>
        private static List<HiddenGemDefinition> NearestNeighbour(List<HiddenGemDefinition> gems)
        {
            if (gems == null || gems.Count == 0) return new List<HiddenGemDefinition>();

            var remaining = new List<HiddenGemDefinition>(gems);
            var ordered   = new List<HiddenGemDefinition>();

            // Start from first entry
            var current = remaining[0];
            remaining.RemoveAt(0);
            ordered.Add(current);

            while (remaining.Count > 0)
            {
                Vector3 curPos   = HiddenGemManager.GetWorldPosition(current);
                int     bestIdx  = 0;
                float   bestDist = float.MaxValue;

                for (int i = 0; i < remaining.Count; i++)
                {
                    float d = Vector3.Distance(curPos, HiddenGemManager.GetWorldPosition(remaining[i]));
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestIdx  = i;
                    }
                }
                current = remaining[bestIdx];
                remaining.RemoveAt(bestIdx);
                ordered.Add(current);
            }
            return ordered;
        }
    }
}
