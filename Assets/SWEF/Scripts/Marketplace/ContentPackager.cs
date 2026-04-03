// ContentPackager.cs — SWEF Community Content Marketplace (Phase 94)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Marketplace
{
    /// <summary>
    /// Static utility that converts Workshop, Navigation, Racing, and Social content objects
    /// into <see cref="MarketplaceListingData"/> payloads (pack) and restores them back into
    /// their originating systems (unpack).
    ///
    /// <para>All methods perform basic integrity validation before returning — invalid or
    /// null inputs return <c>null</c> with a warning.</para>
    /// </summary>
    public static class ContentPackager
    {
        // ── Pack ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Packages an <c>AircraftBuildData</c> from the Workshop into a marketplace listing skeleton.
        /// The caller is responsible for filling <c>title</c>, <c>description</c>, <c>price</c>,
        /// <c>isFree</c>, and <c>tags</c> before publishing.
        /// </summary>
#if SWEF_WORKSHOP_AVAILABLE
        public static MarketplaceListingData PackageAircraftBuild(SWEF.Workshop.AircraftBuildData build)
        {
            if (build == null)
            {
                Debug.LogWarning("[SWEF] Marketplace: PackageAircraftBuild — null build.");
                return null;
            }
            return CreateListing(MarketplaceCategory.AircraftBuild,
                build.buildName, JsonUtility.ToJson(build));
        }
#else
        public static MarketplaceListingData PackageAircraftBuild(object build)
        {
            if (build == null) { Debug.LogWarning("[SWEF] Marketplace: PackageAircraftBuild — null build."); return null; }
            return CreateListing(MarketplaceCategory.AircraftBuild, "Aircraft Build", JsonUtility.ToJson(build));
        }
#endif

        /// <summary>Packages a <c>PaintSchemeData</c> livery into a marketplace listing skeleton.</summary>
#if SWEF_WORKSHOP_AVAILABLE
        public static MarketplaceListingData PackageLivery(SWEF.Workshop.PaintSchemeData scheme)
        {
            if (scheme == null)
            {
                Debug.LogWarning("[SWEF] Marketplace: PackageLivery — null scheme.");
                return null;
            }
            return CreateListing(MarketplaceCategory.Livery,
                scheme.schemeName, JsonUtility.ToJson(scheme));
        }
#else
        public static MarketplaceListingData PackageLivery(object scheme)
        {
            if (scheme == null) { Debug.LogWarning("[SWEF] Marketplace: PackageLivery — null scheme."); return null; }
            return CreateListing(MarketplaceCategory.Livery, "Livery", JsonUtility.ToJson(scheme));
        }
#endif

        /// <summary>Packages a list of <c>DecalData</c> items into a marketplace listing skeleton.</summary>
#if SWEF_WORKSHOP_AVAILABLE
        public static MarketplaceListingData PackageDecalSet(List<SWEF.Workshop.DecalData> decals)
        {
            if (decals == null || decals.Count == 0)
            {
                Debug.LogWarning("[SWEF] Marketplace: PackageDecalSet — null or empty decal list.");
                return null;
            }
            var wrapper = new DecalListWrapper { decals = decals };
            return CreateListing(MarketplaceCategory.Decal,
                $"Decal Set ({decals.Count})", JsonUtility.ToJson(wrapper));
        }
        [Serializable] private class DecalListWrapper { public List<SWEF.Workshop.DecalData> decals; }
#else
        public static MarketplaceListingData PackageDecalSet(object decals)
        {
            if (decals == null) { Debug.LogWarning("[SWEF] Marketplace: PackageDecalSet — null decals."); return null; }
            return CreateListing(MarketplaceCategory.Decal, "Decal Set", JsonUtility.ToJson(decals));
        }
#endif

        /// <summary>Packages a flight plan / route into a marketplace listing skeleton.</summary>
        public static MarketplaceListingData PackageFlightRoute(object flightPlanData)
        {
            if (flightPlanData == null)
            {
                Debug.LogWarning("[SWEF] Marketplace: PackageFlightRoute — null flight plan.");
                return null;
            }
            return CreateListing(MarketplaceCategory.FlightRoute,
                "Flight Route", JsonUtility.ToJson(flightPlanData));
        }

        /// <summary>Packages a race-track / course into a marketplace listing skeleton.</summary>
        public static MarketplaceListingData PackageRaceTrack(object raceTrackData)
        {
            if (raceTrackData == null)
            {
                Debug.LogWarning("[SWEF] Marketplace: PackageRaceTrack — null race track.");
                return null;
            }
            return CreateListing(MarketplaceCategory.RaceTrack,
                "Race Track", JsonUtility.ToJson(raceTrackData));
        }

        /// <summary>Packages a list of <c>SharedWaypointData</c> into a marketplace listing skeleton.</summary>
#if SWEF_MULTIPLAYER_AVAILABLE
        public static MarketplaceListingData PackageWaypointPack(List<SWEF.Multiplayer.SharedWaypointData> waypoints)
        {
            if (waypoints == null || waypoints.Count == 0)
            {
                Debug.LogWarning("[SWEF] Marketplace: PackageWaypointPack — null or empty waypoint list.");
                return null;
            }
            var wrapper = new WaypointListWrapper { waypoints = waypoints };
            return CreateListing(MarketplaceCategory.WaypointPack,
                $"Waypoint Pack ({waypoints.Count})", JsonUtility.ToJson(wrapper));
        }
        [Serializable] private class WaypointListWrapper { public List<SWEF.Multiplayer.SharedWaypointData> waypoints; }
#else
        public static MarketplaceListingData PackageWaypointPack(object waypoints)
        {
            if (waypoints == null) { Debug.LogWarning("[SWEF] Marketplace: PackageWaypointPack — null waypoints."); return null; }
            return CreateListing(MarketplaceCategory.WaypointPack, "Waypoint Pack", JsonUtility.ToJson(waypoints));
        }
#endif

        /// <summary>Packages a photo-mode preset into a marketplace listing skeleton.</summary>
        public static MarketplaceListingData PackagePhotoPreset(object photoPresetData)
        {
            if (photoPresetData == null)
            {
                Debug.LogWarning("[SWEF] Marketplace: PackagePhotoPreset — null preset.");
                return null;
            }
            return CreateListing(MarketplaceCategory.PhotoPreset,
                "Photo Preset", JsonUtility.ToJson(photoPresetData));
        }

        // ── Unpack ────────────────────────────────────────────────────────────

        /// <summary>
        /// Unpacks the content payload of a listing and applies it to the appropriate SWEF subsystem.
        /// </summary>
        /// <param name="listing">Listing whose <c>contentData</c> should be applied.</param>
        /// <returns><c>true</c> if unpacking succeeded.</returns>
        public static bool UnpackContent(MarketplaceListingData listing)
        {
            if (listing == null || string.IsNullOrEmpty(listing.contentData))
            {
                Debug.LogWarning("[SWEF] Marketplace: UnpackContent — null listing or empty contentData.");
                return false;
            }

            try
            {
                switch (listing.category)
                {
                    case MarketplaceCategory.AircraftBuild:
                        return UnpackAircraftBuild(listing.contentData);

                    case MarketplaceCategory.Livery:
                        return UnpackLivery(listing.contentData);

                    case MarketplaceCategory.Decal:
                        return UnpackDecalSet(listing.contentData);

                    case MarketplaceCategory.WaypointPack:
                        return UnpackWaypointPack(listing.contentData);

                    case MarketplaceCategory.FlightRoute:
                    case MarketplaceCategory.RaceTrack:
                    case MarketplaceCategory.PhotoPreset:
                        // These categories are applied to their target systems via dedicated managers.
                        // Log the import; concrete dispatch is handled by the caller via the Bridge.
                        Debug.Log($"[SWEF] Marketplace: UnpackContent — {listing.category} content ready for import (id: {listing.listingId}).");
                        return true;

                    default:
                        Debug.LogWarning($"[SWEF] Marketplace: UnpackContent — unhandled category {listing.category}.");
                        return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Marketplace: UnpackContent failed — {ex.Message}");
                return false;
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static MarketplaceListingData CreateListing(MarketplaceCategory category,
            string defaultTitle, string contentData)
        {
            return new MarketplaceListingData
            {
                category    = category,
                title       = defaultTitle,
                contentData = contentData,
            };
        }

        private static bool UnpackAircraftBuild(string json)
        {
#if SWEF_WORKSHOP_AVAILABLE
            var build = JsonUtility.FromJson<SWEF.Workshop.AircraftBuildData>(json);
            if (build == null) return false;
            SWEF.Workshop.WorkshopManager.Instance?.ImportBuild(build);
            return true;
#else
            Debug.LogWarning("[SWEF] Marketplace: UnpackAircraftBuild — Workshop not available.");
            return false;
#endif
        }

        private static bool UnpackLivery(string json)
        {
#if SWEF_WORKSHOP_AVAILABLE
            var scheme = JsonUtility.FromJson<SWEF.Workshop.PaintSchemeData>(json);
            if (scheme == null) return false;
            SWEF.Workshop.PaintEditorController.Instance?.ImportScheme(scheme);
            return true;
#else
            Debug.LogWarning("[SWEF] Marketplace: UnpackLivery — Workshop not available.");
            return false;
#endif
        }

        private static bool UnpackDecalSet(string json)
        {
#if SWEF_WORKSHOP_AVAILABLE
            var wrapper = JsonUtility.FromJson<DecalListWrapper>(json);
            if (wrapper?.decals == null) return false;
            var editor = SWEF.Workshop.DecalEditorController.Instance;
            if (editor == null) return false;
            foreach (var d in wrapper.decals) editor.ImportDecal(d);
            return true;
#else
            Debug.LogWarning("[SWEF] Marketplace: UnpackDecalSet — Workshop not available.");
            return false;
#endif
        }

        private static bool UnpackWaypointPack(string json)
        {
#if SWEF_MULTIPLAYER_AVAILABLE
            var wrapper = JsonUtility.FromJson<WaypointListWrapper>(json);
            if (wrapper?.waypoints == null) return false;
            var mgr = SWEF.Multiplayer.SharedWaypointManager.Instance;
            if (mgr == null) return false;
            foreach (var w in wrapper.waypoints) mgr.AddWaypoint(w);
            return true;
#else
            Debug.LogWarning("[SWEF] Marketplace: UnpackWaypointPack — Multiplayer not available.");
            return false;
#endif
        }

#if !SWEF_WORKSHOP_AVAILABLE
        [Serializable] private class DecalListWrapper { }
#endif
#if !SWEF_MULTIPLAYER_AVAILABLE
        [Serializable] private class WaypointListWrapper { }
#endif
    }
}
