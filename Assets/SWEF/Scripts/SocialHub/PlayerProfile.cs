using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SocialHub
{
    /// <summary>
    /// Serializable snapshot of a player's public profile, aggregated from the
    /// Progression, Achievement, DailyChallenge, SeasonPass, and Cosmetics systems.
    /// Instances are created by <see cref="PlayerProfileManager"/> and may represent
    /// the local player or a remote/friend player.
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        // ── Identity ──────────────────────────────────────────────────────────────

        /// <summary>Unique player identifier (UUID).</summary>
        public string playerId;

        /// <summary>Player-chosen display name (2–20 characters).</summary>
        public string displayName;

        /// <summary>Identifier of the selected avatar asset.</summary>
        public string avatarId;

        /// <summary>Cosmetic title id equipped by the player (NameTag category).</summary>
        public string titleId;

        // ── Progression ───────────────────────────────────────────────────────────

        /// <summary>Current pilot rank level (1–50).</summary>
        public int pilotRankLevel;

        /// <summary>Display name of the current rank (e.g. "Ace Pilot").</summary>
        public string pilotRankName;

        /// <summary>Total accumulated XP across all sessions.</summary>
        public long totalXP;

        // ── Flight Statistics ─────────────────────────────────────────────────────

        /// <summary>Total flight time in minutes.</summary>
        public float totalFlightTimeMinutes;

        /// <summary>Total distance flown in kilometres.</summary>
        public float totalDistanceKm;

        /// <summary>Highest altitude ever reached, in metres.</summary>
        public float maxAltitudeMeters;

        /// <summary>Top speed ever achieved, in km/h.</summary>
        public float maxSpeedKmh;

        /// <summary>Total number of completed flights.</summary>
        public int totalFlights;

        // ── Achievements ──────────────────────────────────────────────────────────

        /// <summary>Number of achievements the player has unlocked.</summary>
        public int achievementsUnlocked;

        /// <summary>Total number of achievements available in the game.</summary>
        public int achievementsTotal;

        // ── Daily Challenge & Season Pass ─────────────────────────────────────────

        /// <summary>Current consecutive-day login/challenge streak.</summary>
        public int dailyStreak;

        /// <summary>Current season-pass tier (0-based).</summary>
        public int seasonTier;

        /// <summary>Whether the player has the premium season-pass unlocked.</summary>
        public bool isPremium;

        // ── Cosmetics ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Mapping of cosmetic category name to equipped cosmetic id.
        /// Keys match <see cref="SWEF.Progression.CosmeticCategory"/> enum names.
        /// Not serialized directly by JsonUtility; handled via
        /// <see cref="equippedCosmeticKeys"/> / <see cref="equippedCosmeticValues"/>.
        /// </summary>
        [NonSerialized]
        public Dictionary<string, string> equippedCosmetics = new Dictionary<string, string>();

        // Parallel lists used for JsonUtility serialization of the dictionary above.
        public List<string> equippedCosmeticKeys   = new List<string>();
        public List<string> equippedCosmeticValues = new List<string>();

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Reconstructs <see cref="equippedCosmetics"/> from the serialized parallel lists.
        /// Call after deserializing from JSON.
        /// </summary>
        public void RebuildCosmeticsDict()
        {
            equippedCosmetics = new Dictionary<string, string>();
            int count = Mathf.Min(equippedCosmeticKeys.Count, equippedCosmeticValues.Count);
            for (int i = 0; i < count; i++)
                equippedCosmetics[equippedCosmeticKeys[i]] = equippedCosmeticValues[i];
        }

        /// <summary>
        /// Serializes <see cref="equippedCosmetics"/> back into the parallel lists.
        /// Call before serializing to JSON.
        /// </summary>
        public void FlushCosmeticsDict()
        {
            equippedCosmeticKeys.Clear();
            equippedCosmeticValues.Clear();
            foreach (var kv in equippedCosmetics)
            {
                equippedCosmeticKeys.Add(kv.Key);
                equippedCosmeticValues.Add(kv.Value);
            }
        }
    }
}
