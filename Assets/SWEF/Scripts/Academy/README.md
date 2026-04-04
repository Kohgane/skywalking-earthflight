# SWEF Flight Academy & Certification System — Phase 104

The **Flight Academy** module provides a complete pilot training and certification pathway
inside SkyWalking EarthFlight. Players progress from raw beginners all the way to simulated
Airline Transport Pilots through structured curricula, theory quizzes, practical in-flight
exercises, and formal examinations.

---

## Directory layout

```
Academy/
├── LicenseTier.cs               — License tier enum (None → Student → PPL → CPL → ATP)
├── CertificateData.cs           — Digital certificate / badge data model
├── FlightLesson.cs              — Individual lesson definition (theory + practical)
├── FlightCurriculum.cs          — Ordered lesson collection + ScriptableObject asset
├── TheoryModule.cs              — Multiple-choice quiz with automatic scoring
├── PracticalExercise.cs         — In-flight objective-based exercise definition
├── FlightExam.cs                — Weighted theory+practical exam + result builder
├── AcademyConfig.cs             — Global academy settings ScriptableObject
├── AcademyProgressTracker.cs    — Per-player progress (enrolment, scores, certificates)
├── TrainingSessionManager.cs    — Active session lifecycle & phase state-machine
├── CertificationManager.cs      — Certificate issuance & gallery queries
├── FlightAcademyManager.cs      — Main singleton MonoBehaviour (lifecycle orchestrator)
├── AcademyUIController.cs       — Academy UI panels (menu, lessons, gallery, debrief)
└── SWEF.Academy.asmdef          — Assembly definition
```

---

## License tiers

| Tier | Value | Description |
|------|-------|-------------|
| `None` | 0 | No certification (default) |
| `StudentPilot` | 1 | Basic flight fundamentals |
| `PrivatePilot` | 2 | Solo & recreational flying (PPL) |
| `CommercialPilot` | 3 | Advanced cross-country, IFR (CPL) |
| `AirlineTransportPilot` | 4 | Highest civil aviation certificate (ATP) |

---

## Quick-start setup

1. Create an `AcademyConfig` asset: **Assets → Create → SWEF/Academy/Config**
2. Create one or more `FlightCurriculum` assets: **Assets → Create → SWEF/Academy/Curriculum**
3. Populate each curriculum with `FlightLesson` objects (theory quiz + practical exercise).
4. Assign the curricula and matching `FlightExam` objects to the config asset.
5. Add a `FlightAcademyManager` MonoBehaviour to a persistent scene object and assign the config.
6. Attach `AcademyUIController` to the Academy canvas and wire up the panel/prefab references.

---

## Integration points

| System | How Academy integrates |
|--------|------------------------|
| **Mission System** | Practical exercises can mirror mission objectives |
| **Achievement System** | `OnCertificateIssued` event triggers achievement checks |
| **Tutorial System** | First lesson in the Student Pilot curriculum supplements the in-game tutorial |
| **AI Co-Pilot (ARIA)** | `FlightAdvisor` hints can be surfaced during practical exercises |
| **Save System** | Progress is persisted to `academy_progress.json` in `persistentDataPath` |

---

## Testing

Edit-mode NUnit tests are in `Assets/Tests/EditMode/AcademyTests.cs`.
Run them via **Window → General → Test Runner → EditMode**.

---

*Closes #127 — Phase 104: Flight Academy & Certification System*
