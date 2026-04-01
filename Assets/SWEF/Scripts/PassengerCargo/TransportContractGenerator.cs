using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.PassengerCargo
{
    /// <summary>
    /// Static utility that procedurally generates <see cref="TransportContract"/>
    /// instances based on player position, pilot rank, time of day and weather.
    ///
    /// Usage:
    /// <code>
    ///   var contracts = TransportContractGenerator.GenerateContracts(5, playerPos, pilotRank);
    /// </code>
    /// </summary>
    public static class TransportContractGenerator
    {
        // ── Rank thresholds ───────────────────────────────────────────────────
        private const int VipMinRank       = 5;   // "Captain" threshold
        private const float VipChance      = 0.10f;
        private const float EmergencyChance = 0.15f; // during storm weather

        // ── Reward scaling ────────────────────────────────────────────────────
        private const float BaseRewardPerKm  = 0.5f;  // coins per km
        private const float BaseXPPerKm      = 0.3f;  // XP per km
        private const long  MinBaseReward    = 100;
        private const long  MinBaseXP        = 75;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Generates a list of transport contracts appropriate for the player's
        /// current context.
        /// </summary>
        /// <param name="count">Number of contracts to generate.</param>
        /// <param name="playerPos">Current world-space position of the player.</param>
        /// <param name="pilotRank">Player's current rank level (1–50).</param>
        /// <returns>List of generated <see cref="TransportContract"/> instances.</returns>
        public static List<TransportContract> GenerateContracts(
            int count, Vector3 playerPos, int pilotRank)
        {
            var results = new List<TransportContract>(count);

            var airports = SWEF.Landing.AirportRegistry.Instance;
            bool isStormy = IsStormyWeather();
            int  hour     = System.DateTime.Now.Hour;

            for (int i = 0; i < count; i++)
            {
                MissionType type = PickMissionType(pilotRank, isStormy);
                var contract = BuildContract(airports, playerPos, pilotRank, type, i, hour);
                if (contract != null)
                    results.Add(contract);
            }

            return results;
        }

        // ── Internal helpers ──────────────────────────────────────────────────
        private static MissionType PickMissionType(int pilotRank, bool isStormy)
        {
            float roll = UnityEngine.Random.value;

            // Emergency medical during storms (rank independent).
            if (isStormy && roll < EmergencyChance)
                return MissionType.EmergencyMedical;

            // VIP only at Captain+ rank.
            if (pilotRank >= VipMinRank && roll < VipChance)
                return MissionType.PassengerVIP;

            // Weight the remaining types by rank.
            float[] weights = BuildTypeWeights(pilotRank);
            return PickWeighted(weights);
        }

        private static float[] BuildTypeWeights(int rank)
        {
            // Order: Standard Pax, Charter, Cargo Standard, Cargo Fragile,
            //        Cargo Hazardous, Cargo Oversized
            float rankNorm = Mathf.Clamp01(rank / 20f);

            return new float[]
            {
                Mathf.Lerp(0.40f, 0.20f, rankNorm),  // PassengerStandard decreases
                Mathf.Lerp(0.15f, 0.20f, rankNorm),  // PassengerCharter
                Mathf.Lerp(0.25f, 0.15f, rankNorm),  // CargoStandard
                Mathf.Lerp(0.10f, 0.15f, rankNorm),  // CargoFragile
                Mathf.Lerp(0.05f, 0.15f, rankNorm),  // CargoHazardous (rank-gated)
                Mathf.Lerp(0.05f, 0.15f, rankNorm),  // CargoOversized  (rank-gated)
            };
        }

        private static MissionType PickWeighted(float[] weights)
        {
            MissionType[] types =
            {
                MissionType.PassengerStandard,
                MissionType.PassengerCharter,
                MissionType.CargoStandard,
                MissionType.CargoFragile,
                MissionType.CargoHazardous,
                MissionType.CargoOversized
            };

            float total = 0f;
            foreach (float w in weights) total += w;

            float roll  = UnityEngine.Random.Range(0f, total);
            float accum = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                accum += weights[i];
                if (roll <= accum) return types[i];
            }
            return MissionType.PassengerStandard;
        }

        private static TransportContract BuildContract(
            SWEF.Landing.AirportRegistry airports,
            Vector3 playerPos,
            int pilotRank,
            MissionType type,
            int seed,
            int hour)
        {
            // Pick a random origin near the player and a random destination.
            string origin      = PickNearbyAirport(airports, playerPos, seed);
            string destination = PickDestinationAirport(airports, playerPos, seed + 1);

            if (string.IsNullOrEmpty(origin) || string.IsNullOrEmpty(destination)
                || origin == destination)
                return null;

            float distKm = EstimateDistanceKm(airports, origin, destination);

            var contract = ScriptableObject.CreateInstance<TransportContract>();
            contract.contractId    = $"gen_{type}_{seed}_{System.DateTime.Now.Ticks}";
            contract.missionType   = type;
            contract.origin        = origin;
            contract.destination   = destination;
            contract.requiredRank  = ComputeRequiredRank(type, pilotRank);
            contract.baseReward    = Mathf.RoundToInt(
                Mathf.Max(MinBaseReward, distKm * BaseRewardPerKm * RankMultiplier(pilotRank)));
            contract.baseXP        = Mathf.RoundToInt(
                Mathf.Max(MinBaseXP, distKm * BaseXPPerKm * RankMultiplier(pilotRank)));
            contract.bonusReward   = contract.baseReward / 4;
            contract.timeLimitSeconds = distKm > 0f ? distKm * 12f : 0f; // ~12 s/km

            ApplyTypePayload(contract, type, pilotRank, hour);
            return contract;
        }

        private static string PickNearbyAirport(
            SWEF.Landing.AirportRegistry reg, Vector3 pos, int seed)
        {
            if (reg == null) return $"ORIG_{seed}";
            var list = reg.GetAirportsInRange(pos, 500_000f);
            if (list == null || list.Count == 0) return $"ORIG_{seed}";
            return list[seed % list.Count].airportId;
        }

        private static string PickDestinationAirport(
            SWEF.Landing.AirportRegistry reg, Vector3 pos, int seed)
        {
            if (reg == null) return $"DEST_{seed}";
            var list = reg.GetAirportsInRange(pos, 2_000_000f);
            if (list == null || list.Count == 0) return $"DEST_{seed}";
            return list[seed % list.Count].airportId;
        }

        private static float EstimateDistanceKm(
            SWEF.Landing.AirportRegistry reg, string a, string b)
        {
            if (reg == null) return 200f;
            var airportA = reg.GetAirportById(a);
            var airportB = reg.GetAirportById(b);
            if (airportA == null || airportB == null) return 200f;

            // Use runway threshold positions if available; fall back to lat/lon.
            Vector3 posA = airportA.runways != null && airportA.runways.Count > 0
                ? airportA.runways[0].thresholdPosition
                : LatLonToApprox(airportA);
            Vector3 posB = airportB.runways != null && airportB.runways.Count > 0
                ? airportB.runways[0].thresholdPosition
                : LatLonToApprox(airportB);

            return Vector3.Distance(posA, posB) / 1000f;
        }

        private static Vector3 LatLonToApprox(SWEF.Landing.AirportData airport)
        {
            const float MetersPerDegreeLat = 111320f;
            float latRad = (float)(airport.latitude * System.Math.PI / 180.0);
            float z = (float)airport.latitude  * MetersPerDegreeLat;
            float x = (float)airport.longitude * MetersPerDegreeLat * Mathf.Cos(latRad);
            return new Vector3(x, airport.elevation, z);
        }

        private static int ComputeRequiredRank(MissionType type, int pilotRank)
        {
            return type switch
            {
                MissionType.CargoHazardous  => Mathf.Max(3, pilotRank - 5),
                MissionType.CargoOversized  => Mathf.Max(3, pilotRank - 4),
                MissionType.PassengerVIP    => Mathf.Max(VipMinRank, pilotRank - 3),
                MissionType.EmergencyMedical => Mathf.Max(2, pilotRank - 6),
                _ => 0
            };
        }

        private static float RankMultiplier(int rank) => 1f + rank * 0.05f;

        private static void ApplyTypePayload(
            TransportContract c, MissionType type, int rank, int hour)
        {
            switch (type)
            {
                case MissionType.PassengerStandard:
                    c.passengerProfile.passengerCount     = UnityEngine.Random.Range(10, 100);
                    c.passengerProfile.vipLevel           = 0;
                    break;

                case MissionType.PassengerVIP:
                    c.passengerProfile.passengerCount     = UnityEngine.Random.Range(1, 4);
                    c.passengerProfile.vipLevel           = 2;
                    c.passengerProfile.comfortSensitivity = 0.9f;
                    c.baseReward   = (long)(c.baseReward   * 1.5f);
                    c.baseXP       = (long)(c.baseXP       * 1.5f);
                    break;

                case MissionType.PassengerCharter:
                    c.passengerProfile.passengerCount     = UnityEngine.Random.Range(5, 30);
                    c.passengerProfile.vipLevel           = 1;
                    break;

                case MissionType.CargoStandard:
                    c.cargoManifest.weight   = UnityEngine.Random.Range(200f, 5000f);
                    c.cargoManifest.category = CargoCategory.General;
                    break;

                case MissionType.CargoFragile:
                    c.cargoManifest.weight          = UnityEngine.Random.Range(100f, 2000f);
                    c.cargoManifest.category        = CargoCategory.Fragile;
                    c.cargoManifest.fragilityRating = UnityEngine.Random.Range(0.5f, 1.0f);
                    break;

                case MissionType.CargoHazardous:
                    c.cargoManifest.weight   = UnityEngine.Random.Range(500f, 3000f);
                    c.cargoManifest.category = CargoCategory.Hazardous;
                    break;

                case MissionType.CargoOversized:
                    c.cargoManifest.weight  = UnityEngine.Random.Range(2000f, 10000f);
                    c.cargoManifest.volume  = UnityEngine.Random.Range(10f, 50f);
                    c.cargoManifest.category = CargoCategory.Oversized;
                    break;

                case MissionType.EmergencyMedical:
                    c.cargoManifest.weight   = UnityEngine.Random.Range(50f, 500f);
                    c.cargoManifest.category = CargoCategory.Medical;
                    c.timeLimitSeconds       = Mathf.Max(c.timeLimitSeconds * 0.5f, 120f);
                    c.baseReward             = (long)(c.baseReward * 2f);
                    c.baseXP                 = (long)(c.baseXP     * 2f);
                    break;
            }
        }

        private static bool IsStormyWeather()
        {
            var wfm = SWEF.Weather.WeatherFlightModifier.Instance;
            if (wfm == null) return false;
            return wfm.TurbulenceIntensity > 0.6f;
        }
    }
}
