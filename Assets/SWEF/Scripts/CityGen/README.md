# Phase 77 — Procedural City & Landmark Generation System

**Namespace:** `SWEF.CityGen`  
**Directory:** `Assets/SWEF/Scripts/CityGen/`

---

## Overview

A comprehensive procedural city and landmark generation system that creates believable urban environments entirely at runtime. The system streams settlement geometry, roads, vegetation, lighting, and ambient effects relative to the player's world position, delivering smooth LOD transitions, night/day lighting cycles, and a localized landmark discovery experience — all with zero hand-placed assets.

Key capabilities:
- Settlement streaming: megacities to hamlets from a single `CityGenSettings` asset
- Organic, grid, and mixed road-network layouts
- Per-building LOD chain with performance-adaptive quality scaling
- Landmark discovery events integrated with `SWEF.Narration`
- Day/night lighting driven by `SWEF.TimeOfDay`
- Crowd, smoke, bird, and fountain ambient particles
- 35 localization keys covering settlement types, building types, road classifications, and architecture styles

---

## Architecture

```
CityManager (Singleton, DontDestroyOnLoad)
│   ├── CityGenSettings         ← Serializable configuration asset
│   ├── Streaming radius check  ← Spawn / despawn city blocks by player distance
│   ├── ActiveBlocks list       ← Live CityBlock instances
│   ├── GenerateCity()          ← Trigger full settlement pipeline
│   └── Events: OnCityGenerated / OnCityUnloaded / OnLandmarkDiscovered
│
CityLayoutGenerator
│   ├── LayoutStyle selection   ← Grid / Organic / Mixed
│   ├── CityBlock subdivision   ← Recursive quad-tree for grid; radial for organic
│   ├── RoadNetwork output      ← List<RoadSegment> fed to RoadNetworkRenderer
│   └── CityLayout result       ← Passed back to CityManager
│
RoadNetworkRenderer
│   ├── Per-RoadSegment quad mesh  ← Width from RoadType
│   ├── Material selection         ← Road type → material index
│   └── Intersection merging       ← T-junctions, crossings
│
ProceduralBuildingGenerator
│   ├── BuildingDefinition lookup  ← By BuildingType + ArchitectureStyle
│   ├── Mesh assembly              ← Base box + floor repetition + roof cap
│   ├── RoofType variants          ← Flat / Pitched / Dome / Spire / Antenna
│   ├── Object pool                ← Reuse inactive GameObjects across blocks
│   └── BatchMesh combine          ← Static batching per CityBlock
│
LandmarkPlacer
│   ├── LandmarkDefinition list    ← Loaded from CityGenSettings
│   ├── Streaming distance check   ← Spawn / despawn per player proximity
│   ├── Discovery trigger          ← OnTriggerEnter → OnLandmarkDiscovered
│   └── Narration bridge           ← SWEF.Narration (#if SWEF_NARRATION_AVAILABLE)
│
BuildingLODController (per CityBlock)
│   ├── LOD0 (<200 m)   ← Full mesh + materials
│   ├── LOD1 (<500 m)   ← Reduced mesh
│   ├── LOD2 (<1000 m)  ← Impostor billboard
│   ├── LOD3 (<2000 m)  ← Single quad
│   └── Culled (>2000 m)
│
CityLightingController
│   ├── Night lights toggle        ← TimeOfDay bridge (#if SWEF_TIMEOFDAY_AVAILABLE)
│   ├── Window emission toggle
│   └── Street-lamp enable / disable
│
VegetationPlacer
│   ├── Tree / bush placement      ← Park and boulevard zones
│   ├── Density from SettlementType
│   └── LOD billboard swap at distance
│
CityAmbientController
    ├── Crowd particle systems
    ├── Chimney / industrial smoke
    ├── Bird flocks over rooftops
    └── Fountain spray particles
```

---

## Scripts

### 1. `CityGenData.cs`
Pure data layer — no MonoBehaviour dependencies.

| Type | Kind | Description |
|------|------|-------------|
| `SettlementType` | enum | Megacity / City / Town / Village / Hamlet / Industrial / Resort / HistoricCenter |
| `BuildingType` | enum | Residential / Commercial / Industrial / Skyscraper / Church / Mosque / Temple / Stadium / Airport / Park / Monument / Bridge / Tower |
| `RoadType` | enum | Highway / MainRoad / Street / Alley / Pedestrian / Bridge |
| `ArchitectureStyle` | enum | Modern / Classical / Asian / MiddleEastern / Tropical / Nordic / Mediterranean / Futuristic |
| `LandmarkCategory` | enum | Natural / Historical / Architectural / Religious / Cultural / Engineering |
| `RoofType` | enum | Flat / Pitched / Dome / Spire / Antenna |
| `LayoutStyle` | enum | Grid / Organic / Mixed |
| `BuildingDefinition` | serializable class | Per-archetype spec: buildingType, minHeight, maxHeight, minWidth, maxWidth, roofType, architectureStyle, windowDensity, materialIndex |
| `SettlementDefinition` | serializable class | Settlement spec: settlementType, layoutStyle, architectureStyle, radius, density, landmarkCount |
| `LandmarkDefinition` | serializable class | Landmark spec: nameLocKey, category, prefabIndex, discoveryRadius, discoveryNarrationKey |
| `RoadSegment` | serializable class | Individual road: startPoint, endPoint, roadType, width |
| `RoadNetwork` | serializable class | Full road graph: List<RoadSegment> segments, List<Vector2> intersections |
| `CityBlock` | serializable class | Subdivision cell: bounds, buildings, roadEdges, vegetationPoints |
| `CityGenSettings` | ScriptableObject | Master config: streamingRadius, qualityTier, buildingDefinitions, settlementDefinitions, landmarkDefinitions, seed |
| `CityLayout` | serializable class | Generation result: settlement, blocks, roads, landmarks, generationTimeSec |

---

### 2. `CityManager.cs`
Singleton MonoBehaviour — top-level streaming orchestrator.

**Public API:**

| Member | Type | Description |
|--------|------|-------------|
| `Instance` | `CityManager` | Singleton accessor; falls back to `FindFirstObjectByType` |
| `Settings` | `CityGenSettings` | Runtime-readable config asset |
| `ActiveLayout` | `CityLayout` | Most recently generated layout |
| `GenerateCity(SettlementDefinition)` | `void` | Trigger full city generation pipeline for a settlement |
| `UnloadCity()` | `void` | Destroy all active blocks and reset state |
| `SetStreamingRadius(float)` | `void` | Override streaming radius at runtime |
| `OnCityGenerated` | `event Action<CityLayout>` | Fired after generation completes |
| `OnCityUnloaded` | `event Action` | Fired when city is fully unloaded |
| `OnLandmarkDiscovered` | `event Action<LandmarkDefinition>` | Fired on first-time landmark discovery |

---

### 3. `ProceduralBuildingGenerator.cs`
Mesh generation engine with object pool.

**Public API:**

| Member | Type | Description |
|--------|------|-------------|
| `GenerateBuilding(BuildingDefinition, Vector3, Quaternion)` | `GameObject` | Spawn or reuse a pooled building from definition |
| `ReleaseBuilding(GameObject)` | `void` | Return a building to the pool |
| `CombineBlock(CityBlock)` | `void` | Static-batch all buildings in a block for reduced draw calls |
| `SetQualityTier(int)` | `void` | 0–3 quality level; adjusts poly count and texture resolution |

---

### 4. `CityLayoutGenerator.cs`
Grid / organic / mixed layout algorithms.

**Public API:**

| Member | Type | Description |
|--------|------|-------------|
| `Generate(SettlementDefinition, int seed)` | `CityLayout` | Synchronous layout generation; returns complete CityLayout |
| `GenerateAsync(SettlementDefinition, int seed)` | `IEnumerator` | Coroutine variant; yields each block for incremental streaming |
| `SetLayoutStyle(LayoutStyle)` | `void` | Override layout algorithm at runtime |

---

### 5. `RoadNetworkRenderer.cs`
Road quad-mesh renderer.

**Public API:**

| Member | Type | Description |
|--------|------|-------------|
| `RenderNetwork(RoadNetwork)` | `void` | Build quad meshes for all road segments |
| `ClearNetwork()` | `void` | Destroy all road mesh GameObjects |
| `SetRoadMaterial(RoadType, Material)` | `void` | Override material for a road type at runtime |

---

### 6. `LandmarkPlacer.cs`
Landmark streaming and discovery.

**Public API:**

| Member | Type | Description |
|--------|------|-------------|
| `PlaceLandmarks(List<LandmarkDefinition>, CityLayout)` | `void` | Spawn landmark GameObjects at layout positions |
| `RemoveLandmarks()` | `void` | Despawn all active landmarks |
| `IsDiscovered(LandmarkDefinition)` | `bool` | Check if a landmark has been discovered this session |
| `OnLandmarkDiscovered` | `event Action<LandmarkDefinition>` | Forwarded from CityManager |

---

### 7. `BuildingLODController.cs`
Performance-adaptive LOD management per city block.

**Public API:**

| Member | Type | Description |
|--------|------|-------------|
| `SetLODLevel(int)` | `void` | Force LOD level (0–4; 4 = culled) |
| `UpdateLOD(float distanceSq)` | `void` | Called by CityManager each frame; selects LOD from distance |
| `OnQualityChanged(int tier)` | `void` | Callback from performance system to scale LOD thresholds |

---

### 8. `CityLightingController.cs`
Day/night window and street-lamp lighting.

**Public API:**

| Member | Type | Description |
|--------|------|-------------|
| `SetNightMode(bool)` | `void` | Enable / disable night-mode window emission and street lamps |
| `SetIntensity(float)` | `void` | Scale overall city lighting intensity (0–1) |
| `OnTimeOfDayChanged(float)` | `void` | Callback from `SWEF.TimeOfDay` (guarded `#if SWEF_TIMEOFDAY_AVAILABLE`) |

---

### 9. `VegetationPlacer.cs`
Tree and park vegetation placement.

**Public API:**

| Member | Type | Description |
|--------|------|-------------|
| `PlaceVegetation(CityLayout)` | `void` | Spawn trees and bushes at park / boulevard points |
| `ClearVegetation()` | `void` | Despawn all vegetation instances |
| `SetDensityMultiplier(float)` | `void` | Runtime density scale (0–2) |

---

### 10. `CityAmbientController.cs`
Crowd, smoke, birds, and fountain ambient particles.

**Public API:**

| Member | Type | Description |
|--------|------|-------------|
| `StartAmbient()` | `void` | Enable all ambient particle systems |
| `StopAmbient()` | `void` | Disable all ambient particle systems |
| `SetIntensity(float)` | `void` | Scale particle emission rates (0–1) |
| `SetTimeOfDay(float)` | `void` | Adjust crowd density by time (0 = midnight, 0.5 = noon) |

---

## Integration Points

| Script | Integrates With |
|--------|----------------|
| `CityManager` | `SWEF.Flight.FlightController` — player world position for streaming (null-safe) |
| `CityManager` | `SWEF.Terrain.TerrainManager` — ground height sampling for building placement (null-safe) |
| `CityManager` | `SWEF.LOD.LODManager` — global quality tier callback (null-safe) |
| `LandmarkPlacer` | `SWEF.Narration.NarrationManager` — landmark discovery narration (`#if SWEF_NARRATION_AVAILABLE`) |
| `CityLightingController` | `SWEF.TimeOfDay.TimeOfDayManager` — solar time for night mode (`#if SWEF_TIMEOFDAY_AVAILABLE`) |
| `CityAmbientController` | `SWEF.Audio.AudioManager` — crowd / ambient audio (null-safe) |
| `CityManager` | `SWEF.Analytics.UserBehaviorTracker` — city_generated / landmark_discovered events (null-safe) |
| `BuildingLODController` | `SWEF.Performance.PerformanceManager` — adaptive quality tier (null-safe) |

---

## Localization Keys

35 keys added to all 8 language files (`lang_en.json`, `lang_de.json`, `lang_es.json`, `lang_fr.json`, `lang_ja.json`, `lang_ko.json`, `lang_pt.json`, `lang_zh.json`):

### Settlement Types (8 keys)
| Key | English |
|-----|---------|
| `city_settlement_megacity` | Megacity |
| `city_settlement_city` | City |
| `city_settlement_town` | Town |
| `city_settlement_village` | Village |
| `city_settlement_hamlet` | Hamlet |
| `city_settlement_industrial` | Industrial Zone |
| `city_settlement_resort` | Resort |
| `city_settlement_historic` | Historic Center |

### Building Types (13 keys)
| Key | English |
|-----|---------|
| `city_building_residential` | Residential |
| `city_building_commercial` | Commercial |
| `city_building_industrial` | Industrial |
| `city_building_skyscraper` | Skyscraper |
| `city_building_church` | Church |
| `city_building_mosque` | Mosque |
| `city_building_temple` | Temple |
| `city_building_stadium` | Stadium |
| `city_building_airport` | Airport |
| `city_building_park` | Park |
| `city_building_monument` | Monument |
| `city_building_bridge` | Bridge |
| `city_building_tower` | Tower |

### Road Types (6 keys)
| Key | English |
|-----|---------|
| `city_road_highway` | Highway |
| `city_road_main` | Main Road |
| `city_road_street` | Street |
| `city_road_alley` | Alley |
| `city_road_pedestrian` | Pedestrian |
| `city_road_bridge` | Bridge Road |

### Architecture Styles (8 keys)
| Key | English |
|-----|---------|
| `city_style_modern` | Modern |
| `city_style_classical` | Classical |
| `city_style_asian` | Asian |
| `city_style_middleeastern` | Middle Eastern |
| `city_style_tropical` | Tropical |
| `city_style_nordic` | Nordic |
| `city_style_mediterranean` | Mediterranean |
| `city_style_futuristic` | Futuristic |

---

## Setup Instructions

1. **Create CityGenSettings asset** — Right-click in Project → Create → SWEF → CityGen → CityGenSettings
2. **Configure settlement definitions** — Add one or more `SettlementDefinition` entries with type, layout, architecture style, radius, and density
3. **Add building definitions** — Populate `buildingDefinitions` with archetype specs for each `BuildingType` you want to use
4. **Add landmark definitions** — Populate `landmarkDefinitions` referencing prefabs and localization keys
5. **Attach CityManager to a persistent GameObject** — Set the `Settings` field to your asset
6. **Add subsystem components** — Attach `CityLayoutGenerator`, `RoadNetworkRenderer`, `ProceduralBuildingGenerator`, `LandmarkPlacer`, `BuildingLODController`, `CityLightingController`, `VegetationPlacer`, and `CityAmbientController` to child GameObjects under CityManager
7. **Enable optional integrations** — Add `#define SWEF_NARRATION_AVAILABLE` and/or `#define SWEF_TIMEOFDAY_AVAILABLE` to your scripting define symbols in Player Settings if those systems are present
8. **Trigger generation** — Call `CityManager.Instance.GenerateCity(mySettlementDef)` from flight initialization or world-load code
