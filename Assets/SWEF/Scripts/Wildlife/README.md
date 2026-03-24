# Wildlife & Fauna Encounter System — Phase 75

Namespace: `SWEF.Wildlife`  
Directory: `Assets/SWEF/Scripts/Wildlife/`

---

## Overview

Phase 75 implements a complete Wildlife & Fauna Encounter system that spawns and manages birds, marine life, and land animals that players encounter during flight. Wildlife adds life and immersion to the world, with threat-reactive behavior, spatial audio, a collection journal, and full LOD scaling.

---

## System Architecture

```
WildlifeManager (Singleton, DontDestroyOnLoad)
│   ├── WildlifeConfig — performance caps + tuning knobs
│   ├── speciesDatabase — 15 default species registered on Awake
│   ├── activeGroups — live WildlifeGroupState list
│   ├── Coroutine spawn/despawn loop (spawnInterval)
│   ├── Per-frame bird strike detection
│   └── Events: OnGroupSpawned / OnGroupDespawned / OnSpeciesDiscovered
│                OnBirdStrike / OnEncounterRecorded
│
WildlifeSpawnSystem
│   ├── Ring placement: random angle, distance ∈ [spawnRadius×0.5, spawnRadius]
│   ├── Altitude / biome / water validity checks
│   ├── Per-WildlifeCategory Queue<GameObject> object pool (pre-warmed)
│   └── Group instantiation: root GO + boid children + controller components
│
AnimalGroupController (per-group GameObject)
│   ├── State machine: Idle → Roaming ↔ Feeding → Fleeing → Roaming
│   │                  Migrating, Sleeping
│   ├── Threat tracking: None → Aware → Alarmed → Fleeing → Panicked
│   ├── Center-of-mass movement + terrain raycast avoidance
│   └── Discovery reporting → WildlifeManager.RecordEncounter
│
BirdFlockController              MarineLifeController
│   ├── Boid algorithm            │   ├── Surfacing coroutine
│   │   (sep/align/cohesion)      │   ├── Whale breach (parabolic arc)
│   ├── 5 formation types         │   ├── Swim depth management
│   └── Staggered LOD updates     │   └── WaterSurfaceManager integration
│
AnimalAnimationController (per-individual)
│   ├── Tier 1 (<150 m): full Animator or procedural
│   ├── Tier 2 (<500 m): procedural only (sine wing flap, tail oscillation)
│   └── Tier 3 (≥500 m): billboard / dot (no per-frame work)
│
WildlifeAudioController
│   ├── One 3D AudioSource per active group
│   ├── Clip key: category + behavior (idle / alarm)
│   └── Null-safe AudioManager + AccessibilityManager
│
WildlifeJournalIntegration
│   ├── Cooldown + session dedup on encounter logging
│   ├── HashSet<string> discoveredSpecies + JSON persistence
│   ├── Achievement milestones (first encounter, bird watcher, photographer)
│   └── Photo mode bridge
│
WildlifeDebugOverlay (#if UNITY_EDITOR || DEVELOPMENT_BUILD)
    ├── OnDrawGizmos: spawn/despawn rings, group spheres, threat lines
    ├── OnGUI panel: counts, event log, perf notes
    └── Debug controls: force-spawn, force-flee, clear-all
```

---

## Spawn Pipeline

1. `WildlifeManager.SpawnLoop` coroutine fires every `config.spawnInterval` seconds.
2. Player position sampled → `BiomeClassifier` queried (null-safe).
3. `GetSpeciesForBiome(biome)` returns candidates; filtered by altitude and activity pattern.
4. Weighted random selection by `WildlifeSpecies.spawnWeight`.
5. Random ring position computed; `WildlifeSpawnSystem.SpawnGroup` instantiates the group.
6. Root `AnimalGroupController` + per-category sub-controller + boid children created from pool.
7. `WildlifeGroupState` registered in `activeGroups`.

---

## Biome-to-Category Mapping

| SpawnBiome | Typical Categories |
|------------|-------------------|
| Ocean | Seabird, MarineMammal, Fish, MigratoryBird |
| Coast | Seabird, Waterfowl, MarineMammal |
| Lake / River | Waterfowl, Fish |
| Forest | Bird, Raptor, LandMammal, Insect |
| Grassland | Bird, LandMammal, Insect, MigratoryBird |
| Mountain | Raptor, Bird, LandMammal |
| Arctic | MigratoryBird, MarineMammal |
| Tropical | Bird, Insect, LandMammal |
| Wetland | Waterfowl, Bird |

---

## Threat Response

| ThreatLevel | Trigger Distance | Behavior Change |
|-------------|-----------------|-----------------|
| None | > awareDistance | Normal |
| Aware | ≤ awareDistance | Head up, watch |
| Alarmed | ≤ fleeDistance | Prepare to flee |
| Fleeing | ≤ fleeDistance × 0.6 | Active evasion |
| Panicked | ≤ fleeDistance × 0.3 | Chaotic scatter |

---

## Performance Budget

| Setting | Default | Low-End |
|---------|---------|---------|
| `maxActiveGroups` | 15 | 5–8 |
| `maxIndividualsTotal` | 200 | 50–80 |
| `qualityScaleMultiplier` | 1.0 | 0.4 |
| Boid update (staggered) | 5 boids/frame | 2 boids/frame |
| Full anim LOD | < 150 m | < 80 m |
| Procedural LOD | < 500 m | < 200 m |

---

## Default Species (15)

| ID | Category | Biomes |
|----|----------|--------|
| `bald_eagle` | Raptor | Forest, Mountain |
| `seagull` | Seabird | Coast, Ocean |
| `albatross` | Seabird | Ocean |
| `canada_goose` | Waterfowl | Lake, River, Grassland |
| `sparrow` | Bird | Forest, Grassland, Urban |
| `arctic_tern` | MigratoryBird | Arctic, Ocean, Coast |
| `flamingo` | Bird | Wetland, Lake |
| `pelican` | Seabird | Coast, Lake |
| `dolphin` | MarineMammal | Ocean, Coast |
| `humpback_whale` | MarineMammal | Ocean |
| `blue_whale` | MarineMammal | Ocean |
| `salmon` | Fish | River, Lake, Ocean |
| `deer` | LandMammal | Forest, Grassland |
| `bison` | LandMammal | Grassland |
| `butterfly` | Insect | Forest, Grassland, Tropical |

---

## Integration Points

| Script | Integrates With |
|--------|----------------|
| `WildlifeManager` | `SWEF.Biome.BiomeClassifier` — biome detection (null-safe) |
| `WildlifeManager` | `SWEF.TimeOfDay.TimeOfDayManager` — activity filtering (null-safe) |
| `WildlifeManager` | `SWEF.Damage.DamageModel` — bird strike damage (null-safe) |
| `MarineLifeController` | `SWEF.Water.WaterSurfaceManager` — water height (null-safe) |
| `MarineLifeController` | `SWEF.Water.SplashEffectController` — splash effects (null-safe) |
| `WildlifeAudioController` | `SWEF.Audio.AudioManager` — audio clips (null-safe) |
| `WildlifeAudioController` | `SWEF.Accessibility.AccessibilityManager` — reduced audio (null-safe) |
| `WildlifeJournalIntegration` | `SWEF.Journal.JournalManager` — encounter entries (null-safe) |
| `WildlifeJournalIntegration` | `SWEF.Achievement.AchievementManager` — milestones (null-safe) |
| `WildlifeJournalIntegration` | `SWEF.PhotoMode.PhotoCaptureManager` — photo detection (null-safe) |
| `WildlifeSpawnSystem` | `SWEF.Water.WaterSurfaceManager` — marine spawn check (null-safe) |
| `WildlifeDebugOverlay` | `SWEF.DebugOverlay.DebugOverlayController` — panel toggle (null-safe) |

---

## Localization Keys (Phase 75)

**59 keys** added to all 8 language files (`lang_en.json` … `lang_pt.json`):

- Species names: `wildlife_species_bald_eagle` … `wildlife_species_pelican` (15 keys)
- Species descriptions: `wildlife_desc_bald_eagle` … `wildlife_desc_pelican` (15 keys)
- Categories: `wildlife_cat_bird` … `wildlife_cat_mythical` (10 keys)
- UI: `wildlife_journal_title`, `wildlife_journal_encountered`, `wildlife_journal_photographed`, `wildlife_collection_progress`, `wildlife_collection_complete`, `wildlife_discovered_new`, `wildlife_bird_strike_warning` (7 keys)
- Biomes: `wildlife_biome_ocean` … `wildlife_biome_wetland` (12 keys)

---

## Script Reference

| File | Role |
|------|------|
| `WildlifeData.cs` | Core enums & serializable data structures |
| `WildlifeManager.cs` | Singleton — streaming, encounter detection, events |
| `AnimalGroupController.cs` | Per-group state machine, threat reaction, discovery |
| `BirdFlockController.cs` | Boid algorithm, formation types, LOD |
| `MarineLifeController.cs` | Surfacing, breach, swim depth, water integration |
| `AnimalAnimationController.cs` | Procedural animation, LOD-aware switching |
| `WildlifeSpawnSystem.cs` | Object pool, ring placement, group instantiation |
| `WildlifeAudioController.cs` | 3D spatial audio, behavioral triggers |
| `WildlifeJournalIntegration.cs` | Collection tracking, journal, achievements, photo |
| `WildlifeDebugOverlay.cs` | Editor/dev-build gizmos, HUD, debug controls |
