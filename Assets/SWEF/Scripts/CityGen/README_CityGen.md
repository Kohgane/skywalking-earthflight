# CityGen — Procedural City & Landmark Generation
**Phase 52 · Skywalking Earthflight**

---

## System Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          CityManager  (Singleton)                       │
│   Streaming loop · Settlement registry · Placement algorithm            │
│   Events: OnCityLoaded, OnCityUnloaded, OnLandmarkDiscovered            │
└──────┬────────────────┬───────────────┬──────────────────┬──────────────┘
       │                │               │                  │
       ▼                ▼               ▼                  ▼
┌────────────┐  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│ CityLayout │  │ RoadNetwork  │ │ LandmarkPlacer│ │BuildingLOD   │
│ Generator  │  │ Renderer     │ │              │ │Controller    │
│            │  │              │ │ Discovery    │ │              │
│ Grid/Organic│  │ Asphalt/Cobble│ │ Glow · Narr. │ │ Perf-adaptive│
│ /Mixed     │  │ Lane markings│ │ integration  │ │ LOD scaling  │
└──────┬─────┘  └──────────────┘ └──────────────┘ └──────────────┘
       │
       ▼
┌──────────────────────────────────────────────────────────────────┐
│         ProceduralBuildingGenerator                              │
│  Footprints: Rect · L-shape · U-shape · T-shape · Cylinder      │
│  LOD0(full) → LOD1(simplified) → LOD2(box) → LOD3(billboard)    │
│  Object pool · Batch mesh combining                              │
└──────────────────────────────────────────────────────────────────┘
       │
       ├──────────────────────────────────────┐
       ▼                                      ▼
┌───────────────────┐              ┌──────────────────────┐
│ CityLighting      │              │ VegetationPlacer      │
│ Controller        │              │                       │
│ Day/night windows │              │ Street trees · Parks  │
│ Street lights     │              │ Biome-aware selection │
│ Neon / glow       │              │ GPU instancing · LOD  │
└───────────────────┘              └──────────────────────┘
       │
       ▼
┌────────────────────────┐
│ CityAmbientController  │
│ Crowd audio · Smoke    │
│ Birds · Fountains      │
│ Flag animation         │
│ Altitude-gated budget  │
└────────────────────────┘
```

---

## City Generation Pipeline

```
1. PLACEMENT
   CityManager.TryLoadNearbySettlements()
   → deterministic ring-based candidate positions around camera
   → Ocean.OceanManager.IsPositionUnderwater() — reject water
   → IsAlreadyLoaded() — minimum separation between settlements

2. LAYOUT
   CityLayoutGenerator.GenerateLayout()
   → PickLayoutStyle (Grid / Organic / Mixed based on SettlementType)
   → Grid   : regular blocks, main-road axes, highway ring
   → Organic: radial streets + concentric ring roads + irregular blocks
   → Mixed  : organic core (40% radius) + grid outskirts
   → ApplyDensityGradient: taller/denser center, shorter/sparser edges

3. ROADS
   RoadNetworkRenderer.RenderNetwork()
   → flat quad meshes per RoadSegment
   → batched by material (asphalt / cobblestone / highway)
   → camera-distance culling

4. BUILDINGS
   ProceduralBuildingGenerator.GenerateSettlement()
   → per-block building placement with position jitter
   → per-building: random height/width from BuildingDefinition range
   → 4-level LODGroup (full mesh → simplified → box → billboard)
   → BuildingLODController: performance-adaptive LOD scaling

5. VEGETATION
   VegetationPlacer.PlaceStreetTrees() + PlaceParkVegetation()
   → street trees along road segments (both sides, configurable spacing)
   → park clusters for blocks with parkPercentage > 0
   → biome-aware tree selection (palm / deciduous / conifer)

6. LIGHTING
   CityLightingController
   → window material swap at dusk / dawn
   → per-renderer emissive toggle for distant buildings
   → street light placement along road segments
   → random window flicker within budget

7. AMBIENT
   CityAmbientController
   → crowd audio sources with spatial rolloff
   → chimney smoke / fountain particles
   → bird flock spawning over parks
   → flag wave animation
   → altitude cutoff (all effects hidden above 800 m)
```

---

## LOD Strategy

| Level | Trigger Distance | Content |
|-------|-----------------|---------|
| LOD0  | < 200 m         | Full mesh, windows, roof detail |
| LOD1  | 200–500 m       | Simplified mesh, no windows |
| LOD2  | 500–1000 m      | Single box per building |
| LOD3  | 1000–2000 m     | Billboard quad (impostor) |
| Culled | > 2000 m       | Hidden |

`BuildingLODController` monitors frame rate (1 Hz) and scales the
above thresholds by a `_lodScale` factor:
- FPS well above target → relax thresholds (more detail)
- FPS below target      → tighten thresholds (more aggressive LOD)

City blocks at LOD2+ can be batch-merged into a single draw call by
calling `ProceduralBuildingGenerator.GenerateSettlement()` which
combines all buildings per block under one parent `GameObject`.

---

## Performance Considerations

| Concern | Mitigation |
|---------|-----------|
| Draw calls | Batch mesh combining per city block; material batching in `RoadNetworkRenderer` |
| Memory | Object pool in `ProceduralBuildingGenerator` (pre-allocated, reused) |
| CPU streaming | Settlement loading on 2-second interval; roads/buildings generated in same frame but can be moved to coroutines |
| Lighting | Per-window toggling only within `detailLightRadius`; emissive color change for distant buildings |
| Particles | Budget cap via `CityAmbientController.maxActiveEffects` |
| Trees | `VegetationPlacer` distance-culls at `cullDistance`; GPU instancing via Unity's built-in instanced rendering |
| LOD | `BuildingLODController` adapts distances to maintain target FPS |

---

## Integration Points

### Ocean (`SWEF.Ocean`)
`CityManager` queries `OceanManager.IsPositionUnderwater(pos)` before
placing any settlement.  If `OceanManager` is not present the check is
skipped gracefully.

### Narration (`SWEF.Narration`)
`LandmarkPlacer.DiscoverLandmark()` fires
`OnLandmarkDiscovered` and, when `#define SWEF_NARRATION_AVAILABLE` is
set, calls `NarrationManager.TriggerById(narrationTriggerId)`.

### TimeOfDay (`SWEF.TimeOfDay`)
`CityLightingController` reads `TimeOfDayManager.Instance.CurrentHour`
when `#define SWEF_TIMEOFDAY_AVAILABLE` is set.  Fallback: uses
`System.DateTime.Now` wall-clock time.

### Terrain (`SWEF.Terrain`)
Settlement placement currently uses a flat-world assumption.  To
integrate with `ProceduralTerrainGenerator` set each building's Y
position via a terrain height query in `CityManager.LoadSettlement()`.

### LOD System (`SWEF.LOD`)
`BuildingLODController` works alongside Unity's `LODGroup` component.
It does not replace the existing SWEF LOD system but can be connected
to it by registering groups through `Register(LODGroup)`.

---

## Namespace & File Layout

```
Assets/SWEF/Scripts/CityGen/
├── CityGenData.cs               — enums, data classes, settings
├── CityManager.cs               — singleton streaming manager
├── ProceduralBuildingGenerator.cs — mesh generation + pool
├── CityLayoutGenerator.cs       — grid / organic / mixed layouts
├── RoadNetworkRenderer.cs       — road quad meshes
├── LandmarkPlacer.cs            — landmark streaming + discovery
├── BuildingLODController.cs     — perf-adaptive LOD
├── CityLightingController.cs    — day/night lighting
├── VegetationPlacer.cs          — trees and parks
├── CityAmbientController.cs     — crowd / smoke / birds / fountains
└── README_CityGen.md            — this file
```

All scripts use namespace `SWEF.CityGen` and follow the existing SWEF
conventions (singleton pattern, `[SerializeField]` + `FindFirstObjectByType`
fallback, `#region` blocks, XML doc-comments).
