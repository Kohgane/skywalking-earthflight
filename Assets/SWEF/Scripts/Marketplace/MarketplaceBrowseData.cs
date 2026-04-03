// MarketplaceBrowseData.cs — SWEF Community Content Marketplace (Phase 94)
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Marketplace
{
    /// <summary>
    /// Metadata used to populate the marketplace browse/home UI:
    /// category cards with icon paths, featured banner image paths, and trending tag list.
    /// </summary>
    [System.Serializable]
    public class MarketplaceCategoryEntry
    {
        /// <summary>Content category represented by this card.</summary>
        [Tooltip("Content category.")]
        public MarketplaceCategory category;

        /// <summary>Resource path for the category icon sprite.</summary>
        [Tooltip("Resource path for the category icon sprite.")]
        public string iconPath = string.Empty;

        /// <summary>Localisation key for the category display name (prefix: <c>marketplace_</c>).</summary>
        [Tooltip("Localisation key for the category display name.")]
        public string labelKey = string.Empty;
    }

    /// <summary>
    /// Metadata for a featured banner shown on the marketplace home page.
    /// </summary>
    [System.Serializable]
    public class MarketplaceFeaturedBanner
    {
        /// <summary>Resource path for the banner image.</summary>
        [Tooltip("Resource path for the banner image.")]
        public string imagePath = string.Empty;

        /// <summary>Listing ID this banner links to, or empty for a curated collection.</summary>
        [Tooltip("Target listing ID (empty = collection link).")]
        public string targetListingId = string.Empty;

        /// <summary>Short headline text shown on the banner.</summary>
        [Tooltip("Headline text.")]
        public string headline = string.Empty;
    }

    /// <summary>
    /// Static browse/home data used by the Marketplace UI layer:
    /// category entries, featured banners, and trending tags.
    /// </summary>
    public static class MarketplaceBrowseData
    {
        /// <summary>One entry per <see cref="MarketplaceCategory"/> for category browse cards.</summary>
        public static readonly List<MarketplaceCategoryEntry> Categories = new List<MarketplaceCategoryEntry>
        {
            new MarketplaceCategoryEntry { category = MarketplaceCategory.AircraftBuild,  iconPath = "UI/Marketplace/icon_aircraft",  labelKey = "marketplace_category_aircraft_build"  },
            new MarketplaceCategoryEntry { category = MarketplaceCategory.Livery,         iconPath = "UI/Marketplace/icon_livery",    labelKey = "marketplace_category_livery"          },
            new MarketplaceCategoryEntry { category = MarketplaceCategory.Decal,          iconPath = "UI/Marketplace/icon_decal",     labelKey = "marketplace_category_decal"           },
            new MarketplaceCategoryEntry { category = MarketplaceCategory.FlightRoute,    iconPath = "UI/Marketplace/icon_route",     labelKey = "marketplace_category_flight_route"    },
            new MarketplaceCategoryEntry { category = MarketplaceCategory.RaceTrack,      iconPath = "UI/Marketplace/icon_racetrack", labelKey = "marketplace_category_race_track"      },
            new MarketplaceCategoryEntry { category = MarketplaceCategory.WaypointPack,   iconPath = "UI/Marketplace/icon_waypoint",  labelKey = "marketplace_category_waypoint_pack"   },
            new MarketplaceCategoryEntry { category = MarketplaceCategory.PhotoPreset,    iconPath = "UI/Marketplace/icon_photo",     labelKey = "marketplace_category_photo_preset"    },
        };

        /// <summary>Featured banners shown in the hero carousel on the marketplace home page.</summary>
        public static readonly List<MarketplaceFeaturedBanner> FeaturedBanners = new List<MarketplaceFeaturedBanner>();

        /// <summary>Trending tag strings shown as quick-filter chips below the search bar.</summary>
        public static readonly List<string> TrendingTags = new List<string>
        {
            "aerobatic", "vintage", "military", "speedrun", "scenic", "custom",
        };
    }
}
