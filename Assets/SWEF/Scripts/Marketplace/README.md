# Marketplace — Community Content Marketplace

**Phase 94** | Namespace: `SWEF.Marketplace` | Directory: `Assets/SWEF/Scripts/Marketplace/`

The Community Content Marketplace lets players publish, browse, purchase, review, and manage custom content created by the community. It integrates tightly with the Workshop, Multiplayer, Navigation, Racing, Security, Progression, Achievement, Telemetry, and Social systems.

---

## Architecture

```
MarketplaceManager        ← Singleton; listings + library persistence
MarketplaceSearchController ← Search, trending, featured, recommendations
ContentPackager           ← Pack/unpack Workshop/Navigation/Racing content
ReviewManager             ← Singleton; review CRUD + rating aggregation
CreatorDashboardController ← Earnings, analytics, follower system
ContentModerationController ← Singleton; auto-validate + community reports
MarketplaceBridge         ← Static cross-system integration
MarketplaceAnalytics      ← Static telemetry wrapper
```

---

## Content Types

| Category | Source System | Pack Method | Unpack Target |
|----------|--------------|-------------|--------------|
| `AircraftBuild` | Workshop | `PackageAircraftBuild` | `WorkshopManager.ImportBuild` |
| `Livery` | Workshop | `PackageLivery` | `PaintEditorController.ImportScheme` |
| `Decal` | Workshop | `PackageDecalSet` | `DecalEditorController.ImportDecal` |
| `FlightRoute` | FlightPlan | `PackageFlightRoute` | `MarketplaceBridge.ImportContent` |
| `RaceTrack` | CompetitiveRacing | `PackageRaceTrack` | `MarketplaceBridge.ImportContent` |
| `WaypointPack` | Multiplayer | `PackageWaypointPack` | `SharedWaypointManager.AddWaypoint` |
| `PhotoPreset` | AdvancedPhotography | `PackagePhotoPreset` | `MarketplaceBridge.ImportContent` |

---

## Transaction Flow

```
Player clicks "Purchase"
  → MarketplaceManager.PurchaseListing(id)
      → MarketplaceBridge.TryDeductCurrency(price)  [ProgressionManager]
      → Transaction recorded → library saved
      → listing.downloadCount++
      → OnListingPurchased event
      → MarketplaceBridge.OnListingPurchased(listing, txn)
          → CreatorDashboardController.RecordEarning(txn)
          → ContentPackager.UnpackContent(listing)
          → Achievement: first_purchase, content_collector
          → SocialActivityFeed.PostActivity("marketplace_purchase")
```

---

## Review System

- One review per player per listing (enforced by `ReviewManager`)
- Profanity filter applied to all comment text via `SWEF.Security.ProfanityFilter`
- `MarkHelpful(reviewId)` increments the helpful-vote counter
- `ReportReview(reviewId, reason)` forwards to `ContentModerationController`
- Rating aggregation is recalculated on every submit/edit/delete

---

## Creator Dashboard

| Feature | Method |
|---------|--------|
| View stats | `GetCreatorStats()` |
| Earnings history | `GetEarningsHistory()` |
| Pending balance | `GetPendingBalance()` |
| Withdraw earnings | `WithdrawEarnings()` → `ProgressionManager.AddCurrency` |
| Follow creator | `FollowCreator(id)` |
| Unfollow creator | `UnfollowCreator(id)` |

---

## Moderation Rules

| Trigger | Action |
|---------|--------|
| Publish with profanity in title/description | Rejected at validation |
| Content data > 65 536 chars | Rejected at validation |
| ≥ 3 distinct community reports | Auto-unpublish listing |
| Security `ValidateContentData` fails | Rejected at validation |

Report reasons: `Inappropriate`, `CopyrightViolation`, `Broken`, `Spam`, `Cheating`

---

## Persistence Files

All files are written to `Application.persistentDataPath`.

| File | Manager | Contents |
|------|---------|----------|
| `marketplace_listings.json` | `MarketplaceManager` | All published listings |
| `marketplace_library.json` | `MarketplaceManager` | Player's acquired content |
| `marketplace_reviews.json` | `ReviewManager` | All submitted reviews |
| `creator_profile.json` | `CreatorDashboardController` | Creator profile data |
| `creator_earnings.json` | `CreatorDashboardController` | Sales transaction ledger |
| `moderation_reports.json` | `ContentModerationController` | Community reports |

---

## Integration Points

| System | Integration | Guard Symbol |
|--------|------------|--------------|
| `SWEF.Progression.ProgressionManager` | `AddCurrency` (deduct on purchase, add on withdraw), `AddXP` | `#if SWEF_PROGRESSION_AVAILABLE` |
| `SWEF.Workshop.WorkshopManager` | `ImportBuild` on purchased aircraft builds | `#if SWEF_WORKSHOP_AVAILABLE` |
| `SWEF.Workshop.PaintEditorController` | `ImportScheme` on purchased liveries | `#if SWEF_WORKSHOP_AVAILABLE` |
| `SWEF.Workshop.DecalEditorController` | `ImportDecal` on purchased decal sets | `#if SWEF_WORKSHOP_AVAILABLE` |
| `SWEF.Multiplayer.SharedWaypointManager` | `AddWaypoint` on purchased waypoint packs | `#if SWEF_MULTIPLAYER_AVAILABLE` |
| `SWEF.Achievement.AchievementManager` | `ReportProgress` for 6 marketplace achievements | `#if SWEF_ACHIEVEMENT_AVAILABLE` |
| `SWEF.SocialHub.SocialActivityFeed` | `PostActivity` on publish/purchase/review/follow | `#if SWEF_SOCIAL_AVAILABLE` |
| `SWEF.Analytics.TelemetryDispatcher` | 9 events via `MarketplaceAnalytics` | `#if SWEF_ANALYTICS_AVAILABLE` |
| `SWEF.Security.ProfanityFilter` | Comment/title/description validation | `#if SWEF_SECURITY_AVAILABLE` |
| `SWEF.Security.InputSanitizer` | Content-data payload validation | `#if SWEF_SECURITY_AVAILABLE` |

---

## Achievements

| Key | Trigger | Threshold |
|-----|---------|-----------|
| `first_listing` | Publish first listing | 1 |
| `first_purchase` | Make first purchase/download | 1 |
| `top_creator` | Cumulative downloads reach 100 | 100 |
| `marketplace_mogul` | Cumulative sales reach 50 | 50 |
| `five_star_creator` | Receive a 5-star review | 1 |
| `content_collector` | Acquire 25 items from the marketplace | 25 |

---

## Telemetry Events

| Event | Source |
|-------|--------|
| `listing_published` | `MarketplaceManager.PublishListing` |
| `listing_purchased` | `MarketplaceManager.PurchaseListing` |
| `listing_downloaded` | `MarketplaceManager.DownloadFreeContent` |
| `listing_removed` | `MarketplaceManager.UnpublishListing` |
| `review_submitted` | `ReviewManager.SubmitReview` |
| `creator_followed` | `CreatorDashboardController.FollowCreator` |
| `search_performed` | `MarketplaceSearchController.Search` |
| `content_reported` | `ContentModerationController.ReportListing/Creator` |
| `earnings_withdrawn` | `CreatorDashboardController.WithdrawEarnings` |

---

## Localization Prefix

All UI localization keys use the prefix `marketplace_`.

Examples: `marketplace_category_aircraft_build`, `marketplace_notif_sale_title`,
`marketplace_search_placeholder`, `marketplace_publish_success`, `marketplace_purchase_failed`.
