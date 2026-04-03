// MarketplaceCategory.cs — SWEF Community Content Marketplace (Phase 94)
namespace SWEF.Marketplace
{
    /// <summary>
    /// Content categories available in the Community Content Marketplace.
    /// </summary>
    public enum MarketplaceCategory
    {
        /// <summary>Complete aircraft builds exported from the Workshop.</summary>
        AircraftBuild,

        /// <summary>Paint scheme / livery applied to an aircraft.</summary>
        Livery,

        /// <summary>Decal set for aircraft decoration.</summary>
        Decal,

        /// <summary>Pre-planned flight route exported from the Flight Plan system.</summary>
        FlightRoute,

        /// <summary>Racing track / course exported from the Competitive Racing system.</summary>
        RaceTrack,

        /// <summary>Bundle of shared waypoints exported from the Waypoint system.</summary>
        WaypointPack,

        /// <summary>Photo-mode preset (exposure, filters, settings).</summary>
        PhotoPreset,
    }
}
