// PartTier.cs — SWEF Aircraft Workshop & Part Customization (Phase 90)
namespace SWEF.Workshop
{
    /// <summary>
    /// Quality tier assigned to every <see cref="AircraftPartData"/> instance.
    /// Higher tiers provide better stats but require more progression to unlock.
    /// </summary>
    public enum PartTier
    {
        /// <summary>Common — grey (#9E9E9E). Starter parts available from the beginning.</summary>
        Common = 0,

        /// <summary>Uncommon — green (#4CAF50). Mid-range parts with moderate stat gains.</summary>
        Uncommon = 1,

        /// <summary>Rare — blue (#2196F3). High-performance parts with noticeable improvements.</summary>
        Rare = 2,

        /// <summary>Epic — purple (#9C27B0). Near end-game parts with large stat bonuses.</summary>
        Epic = 3,

        /// <summary>Legendary — orange/gold (#FF9800). Peak performance; maximum unlock requirements.</summary>
        Legendary = 4
    }

    /// <summary>
    /// Helper utilities for <see cref="PartTier"/> colour codes and display names.
    /// </summary>
    public static class PartTierExtensions
    {
        /// <summary>
        /// Returns the HTML hex colour string associated with the given tier,
        /// suitable for use with Unity's Rich Text <c>&lt;color&gt;</c> tag.
        /// </summary>
        /// <param name="tier">The part tier to query.</param>
        /// <returns>HTML colour string including the leading <c>#</c>.</returns>
        public static string ToColorHex(this PartTier tier)
        {
            return tier switch
            {
                PartTier.Common    => "#9E9E9E",
                PartTier.Uncommon  => "#4CAF50",
                PartTier.Rare      => "#2196F3",
                PartTier.Epic      => "#9C27B0",
                PartTier.Legendary => "#FF9800",
                _                  => "#FFFFFF"
            };
        }

        /// <summary>Returns a localisation-key suffix for the tier name label.</summary>
        /// <param name="tier">The part tier to query.</param>
        /// <returns>Localisation key of the form <c>workshop_tier_&lt;name&gt;</c>.</returns>
        public static string ToLocKey(this PartTier tier)
        {
            return tier switch
            {
                PartTier.Common    => "workshop_tier_common",
                PartTier.Uncommon  => "workshop_tier_uncommon",
                PartTier.Rare      => "workshop_tier_rare",
                PartTier.Epic      => "workshop_tier_epic",
                PartTier.Legendary => "workshop_tier_legendary",
                _                  => "workshop_tier_unknown"
            };
        }
    }
}
