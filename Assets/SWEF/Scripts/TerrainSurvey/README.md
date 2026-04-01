# TerrainSurvey — Phase 81

**Namespace:** `SWEF.TerrainSurvey`  
**Directory:** `Assets/SWEF/Scripts/TerrainSurvey/`

Real-time terrain scanning and geological survey system. Players can analyse the terrain below during flight, view heatmap overlays, discover geological POIs, and track survey history in the flight journal and minimap.

---

## Architecture Overview

```
TerrainScannerController (Singleton)
│   ├── Configurable scan grid (scanRadius × scanResolution)
│   ├── Raycast terrain sampling at configurable interval
│   ├── Collects SurveySample[] per scan cycle
│   └── Events: OnScanStarted / OnScanCompleted / OnScanPaused
│
GeologicalClassifier (Static)
│   ├── Classify(altitude, slope, biomeId, temperature) → GeologicalFeatureType
│   ├── GetFeatureDisplayName(type) → localization key
│   └── GetFeatureColor(type) → Color for heatmap
│
HeatmapOverlayRenderer
│   ├── Subscribes to OnScanCompleted
│   ├── 5 SurveyMode visualization modes
│   ├── Procedural mesh generation per scan area
│   └── SetOpacity / SetMode / SetVisible
│
SurveyPOIManager (Singleton)
│   ├── Proximity-based deduplication (default 500 m threshold)
│   ├── JSON persistence (survey_pois.json)
│   ├── Max POI cap with oldest-first eviction
│   └── Events: OnPOIDiscovered / OnPOIRemoved
│
SurveyMinimapIntegration
│   ├── Subscribes to OnPOIDiscovered
│   └── Registers MinimapManager blips per POI (null-safe)
│
SurveyJournalBridge
│   ├── Subscribes to OnPOIDiscovered
│   ├── Auto-creates JournalManager entries (null-safe)
│   └── Reports milestones to AchievementManager (null-safe)
│
TerrainSurveyHUD
│   ├── Pulsing scan indicator
│   ├── Live terrain classification label
│   ├── Survey mode selector (5 modes)
│   └── POI discovery toast + cooldown bar
│
TerrainSurveyUI
│   ├── Full POI catalog with filters
│   ├── Navigate-to-POI via WaypointNavigator (null-safe)
│   └── Statistics panel + CSV export
│
TerrainSurveyAnalytics
│   └── Telemetry events via TelemetryDispatcher (null-safe)
```

---

## Scripts

| File | Kind | Description |
|------|------|-------------|
| `TerrainSurveyData.cs` | Data | `GeologicalFeatureType` (12 values), `SurveyMode` (5 values), `SurveySample` struct, `SurveyPOI` class, `TerrainSurveyConfig` ScriptableObject |
| `TerrainScannerController.cs` | Singleton MB | Drives the scan loop; raycasts terrain in a grid; fires scan events |
| `GeologicalClassifier.cs` | Static utility | Classifies altitude/slope/temperature into `GeologicalFeatureType`; provides localization keys and colors |
| `HeatmapOverlayRenderer.cs` | MB | Procedural mesh overlay with 5 visualization modes |
| `SurveyPOIManager.cs` | Singleton MB | Deduplication, JSON persistence, max-cap eviction, POI events |
| `SurveyMinimapIntegration.cs` | MB | Registers POI blips in `MinimapManager` |
| `SurveyJournalBridge.cs` | MB | Writes journal entries and reports achievement milestones |
| `TerrainSurveyHUD.cs` | MB | Scan indicator, classification readout, mode selector, toast, cooldown bar |
| `TerrainSurveyUI.cs` | MB | Full-screen POI catalog, filters, navigate-to, statistics, export |
| `TerrainSurveyAnalytics.cs` | MB | Telemetry event dispatch with session summary |

---

## Public APIs

### `TerrainScannerController`

```csharp
TerrainScannerController.Instance.StartScanning();
TerrainScannerController.Instance.StopScanning();
TerrainScannerController.Instance.PauseScanning();
TerrainScannerController.Instance.ResumeScanning();

// Properties
bool  IsScanning        { get; }
bool  IsPaused          { get; }
float CooldownRemaining { get; }

// Events
event Action              OnScanStarted;
event Action<SurveySample[]> OnScanCompleted;
event Action              OnScanPaused;
```

### `GeologicalClassifier`

```csharp
GeologicalFeatureType type = GeologicalClassifier.Classify(altitude, slope, biomeId, temperature);
string locKey = GeologicalClassifier.GetFeatureDisplayName(type);
Color  color  = GeologicalClassifier.GetFeatureColor(type);
```

### `SurveyPOIManager`

```csharp
SurveyPOI poi = SurveyPOIManager.Instance.DiscoverPOI(sample);
IReadOnlyList<SurveyPOI> all = SurveyPOIManager.Instance.GetAllPOIs();
IEnumerable<SurveyPOI> mountains = SurveyPOIManager.Instance.GetPOIsByFeature(GeologicalFeatureType.Mountain);
SurveyPOIManager.Instance.AcknowledgePOI(poi.id);

// Events
event Action<SurveyPOI> OnPOIDiscovered;
event Action<SurveyPOI> OnPOIRemoved;
```

### `HeatmapOverlayRenderer`

```csharp
renderer.SetMode(SurveyMode.Altitude);
renderer.SetOpacity(0.7f);
renderer.SetVisible(true);
```

### `TerrainSurveyUI`

```csharp
surveyUI.OpenCatalog();
surveyUI.CloseCatalog();
surveyUI.NavigateToPOI(poi);
surveyUI.ExportSurveyData(); // writes survey_export.csv
```

---

## Data Types

### `GeologicalFeatureType` (enum)

`Mountain`, `Desert`, `Volcano`, `Plains`, `Forest`, `Glacier`, `Coastline`, `Canyon`, `Wetland`, `Tundra`, `Plateau`, `RiftValley`

### `SurveyMode` (enum)

`Altitude`, `Slope`, `Biome`, `Temperature`, `Mineral`

### `SurveySample` (struct)

| Field | Type | Description |
|-------|------|-------------|
| `position` | `Vector3` | World-space sample point |
| `featureType` | `GeologicalFeatureType` | Classified feature |
| `altitude` | `float` | Metres above sea level |
| `slope` | `float` | Degrees from horizontal |
| `biomeId` | `int` | Biome ID from `BiomeClassifier` |
| `timestamp` | `long` | UTC Unix seconds |

### `SurveyPOI` (class)

| Field | Type | Description |
|-------|------|-------------|
| `id` | `string` | GUID |
| `position` | `Vector3` | World position |
| `featureType` | `GeologicalFeatureType` | Dominant feature |
| `nameLocKey` | `string` | Localization key |
| `discoveredTimestamp` | `long` | UTC Unix seconds |
| `isNew` | `bool` | True until acknowledged |

---

## Setup Instructions

1. **Create the config asset** — right-click in Project window → *SWEF → Terrain Survey Config*. Assign to `TerrainScannerController.config` and other components.
2. **Add singletons** — add `TerrainScannerController` and `SurveyPOIManager` MonoBehaviours to a persistent scene GameObject.
3. **Add integration bridges** — add `SurveyMinimapIntegration`, `SurveyJournalBridge`, and `TerrainSurveyAnalytics` to the same or child GameObjects.
4. **Wire the HUD** — add `TerrainSurveyHUD` to the HUD canvas and populate inspector references (labels, buttons, sliders).
5. **Wire the Catalog UI** — add `TerrainSurveyUI` to a full-screen canvas panel; assign a POI entry prefab (with `Text` and `Button` children).
6. **Add the heatmap renderer** — add `HeatmapOverlayRenderer` to a world-space GameObject below the aircraft.
7. **Start scanning** — call `TerrainScannerController.Instance.StartScanning()` when flight begins.

---

## Integration Points

| Component | Integrates With | Guard |
|-----------|----------------|-------|
| `TerrainScannerController` | `SWEF.Flight.FlightController` | `#if SWEF_FLIGHT_AVAILABLE` |
| `GeologicalClassifier` | `SWEF.Biome.BiomeClassifier` | null-safe pass-through |
| `SurveyMinimapIntegration` | `SWEF.Minimap.MinimapManager` | `#if SWEF_MINIMAP_AVAILABLE` |
| `SurveyJournalBridge` | `SWEF.Journal.JournalManager` | `#if SWEF_JOURNAL_AVAILABLE` |
| `SurveyJournalBridge` | `SWEF.Achievement.AchievementManager` | `#if SWEF_ACHIEVEMENT_AVAILABLE` |
| `TerrainSurveyUI` | `SWEF.GuidedTour.WaypointNavigator` | `#if SWEF_GUIDEDTOUR_AVAILABLE` |
| `TerrainSurveyAnalytics` | `SWEF.Analytics.TelemetryDispatcher` | `#if SWEF_ANALYTICS_AVAILABLE` |

---

## Persistence

| File | Location | Contents |
|------|----------|----------|
| `survey_pois.json` | `Application.persistentDataPath` | All discovered `SurveyPOI` records |
| `survey_export.csv` | `Application.persistentDataPath` | CSV export triggered from catalog UI |

---

## Localization Keys

24 keys with prefix `survey_` added to all 8 language files (`lang_en.json` – `lang_pt.json`):

**Geological Features (12):** `survey_feature_mountain`, `survey_feature_desert`, `survey_feature_volcano`, `survey_feature_plains`, `survey_feature_forest`, `survey_feature_glacier`, `survey_feature_coastline`, `survey_feature_canyon`, `survey_feature_wetland`, `survey_feature_tundra`, `survey_feature_plateau`, `survey_feature_rift_valley`

**Survey Modes (5):** `survey_mode_altitude`, `survey_mode_slope`, `survey_mode_biome`, `survey_mode_temperature`, `survey_mode_mineral`

**UI (7):** `survey_hud_scanning`, `survey_hud_idle`, `survey_poi_discovered`, `survey_catalog_title`, `survey_stats_title`, `survey_navigate_to`, `survey_export_data`
