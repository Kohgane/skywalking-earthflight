# Wildlife & Fauna Ecosystem — Phase 53

Namespace: `SWEF.Wildlife`  
Directory: `Assets/SWEF/Scripts/Wildlife/`

---

## System Architecture

```
WildlifeManager (singleton)
  │
  ├── WildlifeSpawnSystem          ← chunk-based spawn/despawn pipeline
  │     └── AnimalGroup            ← runtime group state
  │
  ├── AnimalGroupController        ← boid AI, state machine, terrain follow
  │     └── AnimalAnimationController ← procedural anim, LOD switching
  │
  ├── BirdFlockController          ← V-formation, murmuration, soaring
  ├── MarineLifeController         ← whales, dolphins, fish schools
  │
  ├── WildlifeAudioController      ← 3D spatial audio, biome ambience
  └── WildlifeJournalIntegration   ← codex, discovery tracking, save/load
       └── WildlifeDebugOverlay    ← debug HUD (Editor/dev builds only)
```

---

## Wildlife Spawn Pipeline

```
1. StreamingRoutine (every 3 s)
   └── SampleBiome(playerPos)          → BiomeHabitat
       GetFilteredCandidates(biome, alt) → List<AnimalSpecies>
       WildlifeSpawnSystem.RequestSpawn()
           └── PickSpawnPosition()     → deterministic seeded position
               PassesAntiCluster()    → minimum spacing check
               AnimalGroup created    → WildlifeManager.RegisterGroup()
                                        AnimalGroupController spawned
```

---

## Biome-to-Fauna Mapping

| Biome        | Example Species                              |
|--------------|----------------------------------------------|
| Savanna      | Wildebeest, Zebra, Lion, Elephant, Flamingo  |
| Forest       | Deer, Fox, Owl, Woodpecker, Badger           |
| Desert       | Camel, Roadrunner, Scorpion, Sand Viper      |
| Arctic       | Polar Bear, Walrus, Arctic Fox, Snowy Owl    |
| Ocean        | Blue Whale, Dolphin, Manta Ray, Tuna         |
| River        | Crocodile, Hippopotamus, Kingfisher          |
| Mountain     | Snow Leopard, Eagle, Ibex, Mountain Goat     |
| Jungle       | Jaguar, Toucan, Tree Frog, Macaw, Gorilla    |
| Wetland      | Heron, Alligator, Turtle, Manatee            |
| Grassland    | Bison, Prairie Dog, Red-tailed Hawk          |
| Coral        | Clownfish, Sea Turtle, Reef Shark, Ray       |
| DeepSea      | Anglerfish, Giant Squid, Bioluminescent Jelly|
| Coast        | Seagull, Pelican, Sea Lion, Crab             |
| Urban        | Pigeon, Crow, Rat, Sparrow                   |

---

## Performance Budget Guidelines

| Budget Item             | Recommended Limit |
|-------------------------|-------------------|
| Max simultaneous animals| 150               |
| Active groups           | 30–40             |
| Spawn radius            | 2 000 m           |
| Despawn radius          | 2 500 m           |
| LOD0 (full anim)        | < 150 m           |
| LOD1 (simplified)       | < 600 m           |
| LOD2 (static/off)       | ≥ 600 m           |
| Audio sources (pool)    | 16                |
| Chunk size              | 1 000 m           |
| Anti-cluster distance   | 200 m             |
| Streaming tick interval | 3 s               |

---

## Integration Points

| System                    | How Wildlife Connects                                       |
|---------------------------|-------------------------------------------------------------|
| `Terrain.TerrainBiomeMapper` | `SampleBiome()` — biome-based fauna selection           |
| `Ocean.OceanManager`      | Marine life placement zones                                 |
| `Weather.WeatherManager`  | Storm shelter; reduced sounds during storms                 |
| `TimeOfDay.TimeOfDayManager` | Nocturnal/diurnal filter on candidate species           |
| `Flight.FlightController` | `GetPlayerAltitude()` — altitude zone filtering             |
| `Audio.AudioManager`      | AudioMixerGroup output; fallback to standalone AudioSources |
| `PhotoMode.PhotoCaptureManager` | `RecordPhotograph()` → photo bonus + achievement    |
| `Achievement.AchievementManager` | Species-count and rare-find achievements           |
| `Journal.JournalManager`  | Auto-entry on first species discovery                       |
| `Narration.NarrationManager` | First-discovery narration trigger                      |
| `Performance.MemoryPoolManager` | Object pooling for animal GameObjects               |
| `LOD` system              | Per-species LOD distances respected by animation controller |

All cross-system calls are guarded by `#if SWEF_*_AVAILABLE` defines so the
Wildlife system compiles cleanly even when those systems are not present.

---

## Script Reference

| File                          | Role                                                    |
|-------------------------------|---------------------------------------------------------|
| `WildlifeData.cs`             | Core enums & serializable data structures               |
| `WildlifeManager.cs`          | Singleton — streaming, encounter detection, events      |
| `AnimalGroupController.cs`    | Boid AI, state machine, terrain following               |
| `BirdFlockController.cs`      | V-formation, murmuration, thermal soaring, migration    |
| `MarineLifeController.cs`     | Whale breach, dolphin pod, fish school, shark patrol    |
| `AnimalAnimationController.cs`| Procedural animation, LOD-aware animation switching     |
| `WildlifeSpawnSystem.cs`      | Chunk-based spawning, anti-cluster, rarity weighting    |
| `WildlifeAudioController.cs`  | 3D spatial audio, biome ambience, altitude attenuation  |
| `WildlifeJournalIntegration.cs`| Codex, discovery tracking, photo integration, save     |
| `WildlifeDebugOverlay.cs`     | Debug HUD + Gizmos (Editor/DEVELOPMENT_BUILD only)      |
