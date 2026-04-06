// AirportGenerator.cs — Phase 113: Procedural City & Airport Generation
// Procedural airport layout generation based on airport type and size.
// Namespace: SWEF.ProceduralWorld

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// Generates a complete <see cref="AirportLayout"/> for a given location and
    /// <see cref="AirportType"/> using deterministic seeded randomisation.
    /// </summary>
    public class AirportGenerator : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [SerializeField] private RunwayGenerator runwayGenerator;
        [SerializeField] private TerminalGenerator terminalGenerator;

        // ── ICAO generation ───────────────────────────────────────────────────────
        private static readonly string[] IcaoLetters = { "P", "K", "E", "L", "Y", "Z" };

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Generates an <see cref="AirportLayout"/> at <paramref name="position"/> with the
        /// specified type and seed.
        /// </summary>
        public AirportLayout Generate(Vector3 position, AirportType type, int seed, ProceduralWorldConfig cfg)
        {
            var rng = new System.Random(seed);
            var layout = new AirportLayout
            {
                icaoCode = GenerateICAO(rng),
                airportName = GenerateName(type, rng),
                airportType = type,
                referencePoint = position,
                elevationMetres = position.y,
                hasControlTower = type != AirportType.Helipad,
                gateCount = GateCount(type, rng)
            };

            // Generate runways
            int runwayCount = RunwayCount(type, rng);
            float prevHeading = (float)rng.NextDouble() * 180f; // Keep in 0-180 range for reciprocal pairing
            for (int i = 0; i < runwayCount; i++)
            {
                if (i > 0) prevHeading = (prevHeading + (float)(rng.NextDouble() * 45f + 15f)) % 180f;
                var runway = BuildRunway(position, prevHeading, type, i, rng);
                layout.runways.Add(runway);
            }

            return layout;
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private RunwayData BuildRunway(Vector3 apRef, float heading, AirportType type, int index, System.Random rng)
        {
            float length = type switch
            {
                AirportType.International => (float)(rng.NextDouble() * 1000f + 3000f),
                AirportType.Regional      => (float)(rng.NextDouble() * 800f + 1800f),
                AirportType.Military      => (float)(rng.NextDouble() * 1000f + 2500f),
                AirportType.Helipad       => 30f,
                _                         => 1000f
            };

            int designatorNum = Mathf.RoundToInt(heading / 10f);
            if (designatorNum == 0) designatorNum = 36;

            char suffix = index == 0 ? 'L' : 'R';
            Vector3 offset = new Vector3(
                Mathf.Sin(heading * Mathf.Deg2Rad) * 200f * index,
                0f,
                Mathf.Cos(heading * Mathf.Deg2Rad) * 200f * index);

            return new RunwayData
            {
                designator = $"{designatorNum:D2}{suffix}",
                heading = heading,
                lengthMetres = length,
                widthMetres = type == AirportType.International ? 60f : 45f,
                thresholdPosition = apRef + offset,
                hasILS = type == AirportType.International || type == AirportType.Regional
            };
        }

        private static int RunwayCount(AirportType type, System.Random rng) => type switch
        {
            AirportType.International => rng.Next(2, 5),
            AirportType.Regional      => rng.Next(1, 3),
            AirportType.Military      => rng.Next(1, 3),
            AirportType.Helipad       => 1,
            _                         => 1
        };

        private static int GateCount(AirportType type, System.Random rng) => type switch
        {
            AirportType.International => rng.Next(15, 60),
            AirportType.Regional      => rng.Next(2, 15),
            AirportType.Military      => 0,
            AirportType.Helipad       => 0,
            _                         => 0
        };

        private static string GenerateICAO(System.Random rng)
        {
            string prefix = IcaoLetters[rng.Next(IcaoLetters.Length)];
            return $"{prefix}W{rng.Next(10, 100):D2}";
        }

        private static string GenerateName(AirportType type, System.Random rng)
        {
            string[] adjectives = { "International", "Regional", "Municipal", "Executive" };
            string adj = adjectives[rng.Next(adjectives.Length)];
            return type == AirportType.Military
                ? $"AFB {rng.Next(1, 99)}"
                : $"{adj} Airport {rng.Next(1, 99)}";
        }
    }
}
