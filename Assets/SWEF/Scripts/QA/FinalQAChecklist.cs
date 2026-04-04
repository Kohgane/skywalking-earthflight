// FinalQAChecklist.cs — SWEF Phase 102: Final QA & Release Candidate Prep
// Defines the complete QA checklist data model and runtime verification helpers
// that cover all major SWEF systems before v1.0.0-rc1 store submission.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.QA
{
    // ── Enums ────────────────────────────────────────────────────────────────────

    /// <summary>Result of a single QA checklist item.</summary>
    public enum QAResult
    {
        /// <summary>The item has not been tested yet.</summary>
        Pending,
        /// <summary>The item passed all checks.</summary>
        Pass,
        /// <summary>The item failed and must be fixed before release.</summary>
        Fail,
        /// <summary>The item is intentionally skipped for this build/platform.</summary>
        Skip,
        /// <summary>The item is blocked by an upstream dependency.</summary>
        Blocked
    }

    /// <summary>Broad system category for grouping checklist items.</summary>
    public enum QASystem
    {
        FlightPhysics,
        Controls,
        CesiumTiles,
        GPS,
        Weather,
        DayNight,
        HUD,
        Minimap,
        Achievement,
        Journal,
        Multiplayer,
        AICoPilot,
        Audio,
        Camera,
        ATC,
        Emergency,
        BattlePass,
        SeasonalEvents,
        Performance,
        Platform
    }

    // ── Data types ───────────────────────────────────────────────────────────────

    /// <summary>
    /// A single line item in the SWEF Final QA checklist.
    /// </summary>
    [Serializable]
    public sealed class QAChecklistItem
    {
        // ── Identity ──────────────────────────────────────────────────────────

        /// <summary>Unique identifier for this checklist item (e.g. "FP-001").</summary>
        public string Id;

        /// <summary>The system this item belongs to.</summary>
        public QASystem System;

        /// <summary>Short, human-readable title of the check.</summary>
        public string Title;

        /// <summary>Step-by-step instructions for the tester.</summary>
        public string[] Steps;

        /// <summary>Measurable pass criterion.</summary>
        public string PassCriteria;

        // ── State ─────────────────────────────────────────────────────────────

        /// <summary>Current result of this item.</summary>
        public QAResult Result = QAResult.Pending;

        /// <summary>Optional tester notes (failure details, workarounds, etc.).</summary>
        public string Notes;

        /// <summary>UTC timestamp when Result was last updated.</summary>
        public DateTime LastUpdated;

        // ── Construction ──────────────────────────────────────────────────────

        /// <summary>Creates a new <see cref="QAChecklistItem"/>.</summary>
        public QAChecklistItem(string id, QASystem system, string title, string[] steps, string passCriteria)
        {
            Id           = id;
            System       = system;
            Title        = title;
            Steps        = steps;
            PassCriteria = passCriteria;
            Result       = QAResult.Pending;
            LastUpdated  = DateTime.UtcNow;
        }

        /// <summary>Marks this item with the given result and optional notes.</summary>
        public void Mark(QAResult result, string notes = "")
        {
            Result      = result;
            Notes       = notes;
            LastUpdated = DateTime.UtcNow;
        }
    }

    // ── Main checklist ───────────────────────────────────────────────────────────

    /// <summary>
    /// Complete final QA checklist for SWEF v1.0.0-rc1.
    ///
    /// <para>Contains all checklist items across every major system.
    /// Instantiate with <see cref="Build"/> to obtain the full default set.</para>
    /// </summary>
    public sealed class FinalQAChecklist
    {
        // ── Items ─────────────────────────────────────────────────────────────

        /// <summary>All checklist items in this run.</summary>
        public IReadOnlyList<QAChecklistItem> Items => _items;
        private readonly List<QAChecklistItem> _items = new List<QAChecklistItem>();

        // ── Summary ───────────────────────────────────────────────────────────

        /// <summary>Number of items with <see cref="QAResult.Pass"/>.</summary>
        public int PassCount  => CountByResult(QAResult.Pass);

        /// <summary>Number of items with <see cref="QAResult.Fail"/>.</summary>
        public int FailCount  => CountByResult(QAResult.Fail);

        /// <summary>Number of items with <see cref="QAResult.Pending"/>.</summary>
        public int PendingCount => CountByResult(QAResult.Pending);

        /// <summary>Number of items with <see cref="QAResult.Skip"/> or <see cref="QAResult.Blocked"/>.</summary>
        public int SkippedCount => CountByResult(QAResult.Skip) + CountByResult(QAResult.Blocked);

        /// <summary>
        /// <c>true</c> when all non-skipped/non-blocked items have passed.
        /// </summary>
        public bool IsReleasable => FailCount == 0 && PendingCount == 0;

        private int CountByResult(QAResult r)
        {
            int c = 0;
            foreach (var item in _items) if (item.Result == r) c++;
            return c;
        }

        // ── Lookup ────────────────────────────────────────────────────────────

        /// <summary>Returns all items belonging to the given system.</summary>
        public List<QAChecklistItem> GetBySystem(QASystem system)
        {
            var result = new List<QAChecklistItem>();
            foreach (var item in _items)
                if (item.System == system) result.Add(item);
            return result;
        }

        /// <summary>Returns the item with the given id, or <c>null</c>.</summary>
        public QAChecklistItem GetById(string id)
        {
            foreach (var item in _items)
                if (item.Id == id) return item;
            return null;
        }

        // ── Factory ───────────────────────────────────────────────────────────

        /// <summary>
        /// Builds and returns the complete default SWEF v1.0.0-rc1 QA checklist.
        /// </summary>
        public static FinalQAChecklist Build()
        {
            var cl = new FinalQAChecklist();
            cl.AddFlightPhysicsItems();
            cl.AddControlItems();
            cl.AddCesiumItems();
            cl.AddGPSItems();
            cl.AddWeatherItems();
            cl.AddDayNightItems();
            cl.AddHUDItems();
            cl.AddMinimapItems();
            cl.AddAchievementItems();
            cl.AddJournalItems();
            cl.AddMultiplayerItems();
            cl.AddAICoPilotItems();
            cl.AddAudioItems();
            cl.AddCameraItems();
            cl.AddATCItems();
            cl.AddEmergencyItems();
            cl.AddBattlePassItems();
            cl.AddSeasonalEventItems();
            cl.AddPerformanceItems();
            cl.AddPlatformItems();
            return cl;
        }

        // ── Private item builders ─────────────────────────────────────────────

        private void Add(QAChecklistItem item) => _items.Add(item);

        private void AddFlightPhysicsItems()
        {
            Add(new QAChecklistItem("FP-001", QASystem.FlightPhysics,
                "Basic takeoff physics",
                new[] { "Open World scene", "Press Play", "Apply full throttle", "Observe aircraft lift-off" },
                "Aircraft lifts off within 10 s at full throttle; no physics clipping into terrain."));

            Add(new QAChecklistItem("FP-002", QASystem.FlightPhysics,
                "Stall and recovery",
                new[] { "Climb to 1 000 m", "Cut throttle and pull nose up until stall warning", "Apply recovery inputs" },
                "Stall warning triggers before full stall; recovery inputs restore stable flight within 5 s."));

            Add(new QAChecklistItem("FP-003", QASystem.FlightPhysics,
                "Atmosphere layer transitions",
                new[] { "Climb from sea level to 100 km", "Monitor air density, speed, and drag readouts" },
                "Atmospheric effects change smoothly through Troposphere → Stratosphere → Kármán line; no physics spikes."));

            Add(new QAChecklistItem("FP-004", QASystem.FlightPhysics,
                "G-force indicator accuracy",
                new[] { "Perform tight banking turns", "Check HUD G-force indicator vs. expected values" },
                "G-force readout is within ±0.5 G of calculated value during sustained 2–4 G manoeuvres."));

            Add(new QAChecklistItem("FP-005", QASystem.FlightPhysics,
                "Speed boundaries (subsonic → supersonic → hypersonic)",
                new[] { "Accelerate past Mach 1, Mach 5, Mach 20 thresholds", "Check sonic boom and reentry FX" },
                "Sonic boom VFX/audio plays at Mach 1 ±0.05; reentry heating FX activates above Mach 5."));
        }

        private void AddControlItems()
        {
            Add(new QAChecklistItem("CT-001", QASystem.Controls,
                "Touch controls — pitch and roll",
                new[] { "Launch on mobile device or simulator", "Swipe up/down for pitch, left/right for roll" },
                "Aircraft responds within 2 frames with no input lag above 30 fps."));

            Add(new QAChecklistItem("CT-002", QASystem.Controls,
                "WASD keyboard controls",
                new[] { "Launch on PC/Editor", "Use WASD + mouse for flight" },
                "All six axes (pitch/roll/yaw/throttle/camera) respond correctly per KeybindConfig defaults."));

            Add(new QAChecklistItem("CT-003", QASystem.Controls,
                "Gamepad — Xbox and PS layout",
                new[] { "Connect Xbox controller", "Fly a circuit; repeat with PS controller" },
                "All required axes map correctly; deadzones are applied; no stick drift above threshold."));

            Add(new QAChecklistItem("CT-004", QASystem.Controls,
                "Custom keybind remapping",
                new[] { "Open Settings → Controls", "Remap throttle up to a different key", "Fly with new bind" },
                "Remapped key functions correctly; default key no longer triggers the action; persists after restart."));

            Add(new QAChecklistItem("CT-005", QASystem.Controls,
                "Tablet dual-stick touch layout",
                new[] { "Launch on iPad/Android Tablet", "Use left stick for throttle/yaw, right stick for pitch/roll" },
                "Both virtual sticks operate independently with no cross-input bleed."));
        }

        private void AddCesiumItems()
        {
            Add(new QAChecklistItem("CES-001", QASystem.CesiumTiles,
                "3D Tiles initial load",
                new[] { "Launch World scene with valid Cesium API key", "Wait for tiles to stream in at start position" },
                "Tiles within 2 km of camera load within 10 s on a 50 Mbps connection; no missing tile gaps."));

            Add(new QAChecklistItem("CES-002", QASystem.CesiumTiles,
                "Streaming at cruise altitude (10 000 m)",
                new[] { "Climb to 10 000 m AGL", "Pan camera across terrain" },
                "LOD transitions are smooth (no pop-in flashes >1 frame); tile requests stay within budget."));

            Add(new QAChecklistItem("CES-003", QASystem.CesiumTiles,
                "Tile unloading on altitude exit",
                new[] { "Climb to 200 km; observe tile memory", "Return to 1 000 m; tiles reload" },
                "GPU memory stays below platform budget; tiles reload correctly on descent."));

            Add(new QAChecklistItem("CES-004", QASystem.CesiumTiles,
                "Offline / no-network fallback",
                new[] { "Disable network", "Launch app" },
                "App shows a localised offline warning; no crash or freeze; cached tiles (if any) are shown."));
        }

        private void AddGPSItems()
        {
            Add(new QAChecklistItem("GPS-001", QASystem.GPS,
                "GPS permission prompt (iOS/Android)",
                new[] { "Fresh install on device", "Launch app" },
                "OS permission dialog appears on first launch; app gracefully handles denial."));

            Add(new QAChecklistItem("GPS-002", QASystem.GPS,
                "Start-from-location accuracy",
                new[] { "Grant GPS permission", "Note real-world location; confirm spawn position on map" },
                "Spawn position matches real location within 50 m."));

            Add(new QAChecklistItem("GPS-003", QASystem.GPS,
                "GPS-denied fallback (editor / PC)",
                new[] { "Run on PC without GPS hardware" },
                "App spawns at configured default location (e.g., 35.6N 139.7E); no exception thrown."));
        }

        private void AddWeatherItems()
        {
            Add(new QAChecklistItem("WX-001", QASystem.Weather,
                "Dynamic weather state transitions",
                new[] { "In Editor, trigger Clear → Overcast → Storm via WeatherController" },
                "All three states render visually distinct cloud/fog/rain VFX; audio changes accordingly."));

            Add(new QAChecklistItem("WX-002", QASystem.Weather,
                "Wind effect on flight model",
                new[] { "Set high crosswind via WindController", "Attempt straight flight" },
                "Aircraft drifts in wind direction proportional to wind speed; pilot must compensate."));

            Add(new QAChecklistItem("WX-003", QASystem.Weather,
                "Natural disaster — storm interception",
                new[] { "Trigger thunderstorm via DisasterManager", "Fly through storm" },
                "Lightning visual/audio effect triggers; flight model is destabilised within storm zone."));
        }

        private void AddDayNightItems()
        {
            Add(new QAChecklistItem("DN-001", QASystem.DayNight,
                "Full 24-hour cycle (time-lapse)",
                new[] { "Set time scale to 100×", "Wait for full cycle" },
                "Sun rises east, sets west; sky gradient transitions correctly through dawn/dusk/night."));

            Add(new QAChecklistItem("DN-002", QASystem.DayNight,
                "City lights at night",
                new[] { "Set time to midnight", "Fly over a city area" },
                "CityLightingController activates; lights visible from 5 km altitude."));

            Add(new QAChecklistItem("DN-003", QASystem.DayNight,
                "Star field at high altitude / night",
                new[] { "Climb to 50 km at night" },
                "Star field renders; Milky Way band visible above 30 km; no z-fighting with sky."));
        }

        private void AddHUDItems()
        {
            Add(new QAChecklistItem("HUD-001", QASystem.HUD,
                "All HUD instruments readable at runtime",
                new[] { "Start flight", "Check altimeter, speedometer, compass, VSI, G-meter" },
                "All five primary instruments update each frame; values within expected ranges."));

            Add(new QAChecklistItem("HUD-002", QASystem.HUD,
                "HUD scale — phone vs. tablet layout",
                new[] { "Launch on phone", "Launch on tablet", "Compare HUD element sizing" },
                "Tablet layout uses TabletHUDLayout with split panels; phone uses compact single-panel layout."));

            Add(new QAChecklistItem("HUD-003", QASystem.HUD,
                "Warning system — stall, overspeed, terrain",
                new[] { "Trigger each warning condition" },
                "Each warning shows distinct visual and audio alert within 0.5 s of threshold crossing."));

            Add(new QAChecklistItem("HUD-004", QASystem.HUD,
                "HUD hide / show toggle",
                new[] { "Press HUD toggle key/button" },
                "HUD hides fully (alpha = 0) then restores on second press; flight data still computed."));
        }

        private void AddMinimapItems()
        {
            Add(new QAChecklistItem("MM-001", QASystem.Minimap,
                "Minimap renders player position",
                new[] { "Open minimap panel", "Move aircraft; verify dot tracks movement" },
                "Player icon updates within 1 s of position change; heading indicator matches HUD compass."));

            Add(new QAChecklistItem("MM-002", QASystem.Minimap,
                "Waypoint display on minimap",
                new[] { "Set a navigation waypoint", "Confirm it appears on minimap" },
                "Waypoint icon visible on minimap; bearing line drawn from player to waypoint."));
        }

        private void AddAchievementItems()
        {
            Add(new QAChecklistItem("ACH-001", QASystem.Achievement,
                "Achievement unlock notification",
                new[] { "Complete 'First Flight' achievement criteria", "Wait for popup" },
                "Achievement toast appears within 2 s; displays correct title, icon, and XP reward."));

            Add(new QAChecklistItem("ACH-002", QASystem.Achievement,
                "Achievement progress persistence",
                new[] { "Make partial progress on a counted achievement", "Quit and relaunch" },
                "Progress is restored from save file; counter continues from saved value."));
        }

        private void AddJournalItems()
        {
            Add(new QAChecklistItem("JN-001", QASystem.Journal,
                "Journal entry created on milestone",
                new[] { "Complete a flight that crosses the Kármán line", "Open Journal panel" },
                "A new entry appears with correct altitude, timestamp, and location data."));

            Add(new QAChecklistItem("JN-002", QASystem.Journal,
                "Journal photo attachment",
                new[] { "Take a photo in-flight", "Open Journal; confirm photo attached" },
                "Photo thumbnail visible in journal entry; tapping/clicking opens full photo."));
        }

        private void AddMultiplayerItems()
        {
            Add(new QAChecklistItem("MP-001", QASystem.Multiplayer,
                "Lobby creation and join",
                new[] { "Host creates lobby", "Second client joins via code" },
                "Both players appear in lobby within 5 s; player count reflects correctly."));

            Add(new QAChecklistItem("MP-002", QASystem.Multiplayer,
                "Position sync (2-player flight)",
                new[] { "Both players take off", "Observe each other's aircraft" },
                "Remote aircraft position latency < 200 ms at 50 Mbps; no teleporting."));

            Add(new QAChecklistItem("MP-003", QASystem.Multiplayer,
                "Chat — profanity filter",
                new[] { "Send a message with a banned word via MultiplayerChatController" },
                "Message is replaced with asterisks before being sent to peers."));
        }

        private void AddAICoPilotItems()
        {
            Add(new QAChecklistItem("ARIA-001", QASystem.AICoPilot,
                "ARIA initialises and greets player",
                new[] { "Start new flight session" },
                "AICoPilotManager sends a greeting message within 3 s of flight start; logged in AICoPilotUIPanel."));

            Add(new QAChecklistItem("ARIA-002", QASystem.AICoPilot,
                "ARIA flight advisory on dangerous attitude",
                new[] { "Enter a steep dive (>60° nose down) and hold for 3 s" },
                "FlightAdvisor fires Advisory event with AdvisoryLevel.Warning; ARIA speaks recovery prompt."));

            Add(new QAChecklistItem("ARIA-003", QASystem.AICoPilot,
                "ARIA emergency procedure guidance",
                new[] { "Trigger engine failure via EmergencyAdvisor test method" },
                "EmergencyAdvisor activates; step-by-step procedure appears in ARIA UI panel within 1 s."));

            Add(new QAChecklistItem("ARIA-004", QASystem.AICoPilot,
                "Smart autopilot handoff",
                new[] { "Enable SmartAutopilot via ARIA command", "Fly straight-and-level for 60 s" },
                "SmartAutopilotBridge activates autopilot; ARIA confirms handoff; aircraft maintains heading ±5°."));
        }

        private void AddAudioItems()
        {
            Add(new QAChecklistItem("AU-001", QASystem.Audio,
                "Adaptive music — calm to intense transition",
                new[] { "Fly level (calm context)", "Enter a storm (intense context)" },
                "AdaptiveMusicManager transitions music layer within 4 beats; no click or pop artefact."));

            Add(new QAChecklistItem("AU-002", QASystem.Audio,
                "Audio volume controls (SFX, Music, Voice)",
                new[] { "Open Settings → Audio", "Adjust each slider to 0, then back to 100" },
                "Each slider controls only its category; muting SFX does not mute music."));

            Add(new QAChecklistItem("AU-003", QASystem.Audio,
                "Spatial audio — engine sound distance falloff",
                new[] { "In multiplayer, fly far from another player's aircraft", "Listen to engine SFX level" },
                "Engine audio fades proportionally with distance; silent beyond 2 km."));
        }

        private void AddCameraItems()
        {
            Add(new QAChecklistItem("CAM-001", QASystem.Camera,
                "Photo mode — capture and save",
                new[] { "Enter photo mode", "Apply a filter", "Capture photo" },
                "Photo saved to device gallery (or persistent data path on PC); no frame drop during save."));

            Add(new QAChecklistItem("CAM-002", QASystem.Camera,
                "Drone camera — autonomous flight path",
                new[] { "Enable DroneAutonomyController", "Set a path and let it fly" },
                "Drone follows path within 1 m tolerance; video/timelapse is captured continuously."));

            Add(new QAChecklistItem("CAM-003", QASystem.Camera,
                "Orbital camera — smooth pivot around aircraft",
                new[] { "Switch to orbital camera mode", "Drag to orbit" },
                "Camera orbits without gimbal lock; collision avoidance prevents clipping into terrain."));
        }

        private void AddATCItems()
        {
            Add(new QAChecklistItem("ATC-001", QASystem.ATC,
                "ATC radio contact on approach",
                new[] { "Fly within 30 km of a simulated airport", "Open ATC panel" },
                "ATCManager initiates contact with approach phraseology; ATCHUD updates with clearance."));

            Add(new QAChecklistItem("ATC-002", QASystem.ATC,
                "Runway assignment and ILS approach",
                new[] { "Request landing clearance from ATC", "Follow ILS guidance to landing" },
                "Runway assigned; ILS guidance indicator appears on HUD; localiser and glideslope tracks correctly."));
        }

        private void AddEmergencyItems()
        {
            Add(new QAChecklistItem("EM-001", QASystem.Emergency,
                "Engine failure simulation — single engine",
                new[] { "Trigger single engine failure via EmergencyAdvisor" },
                "Engine sound cuts; thrust asymmetry applied; ARIA emergency checklist appears."));

            Add(new QAChecklistItem("EM-002", QASystem.Emergency,
                "Emergency landing — successful ditching",
                new[] { "With total engine failure, glide to water surface", "Land within sink-rate limits" },
                "Successful ditching logged; achievement awarded; no crash-loop or freeze."));
        }

        private void AddBattlePassItems()
        {
            Add(new QAChecklistItem("BP-001", QASystem.BattlePass,
                "Battle pass tier unlock",
                new[] { "Earn enough XP to cross a tier boundary", "Open Battle Pass UI" },
                "Tier unlocks immediately; BattlePassReward.Claim() fires; reward ceremony animation plays."));

            Add(new QAChecklistItem("BP-002", QASystem.BattlePass,
                "Premium track premium reward",
                new[] { "Upgrade to premium pass", "Unlock a premium-only tier reward" },
                "Premium-only item delivered to inventory; free-track player cannot claim it."));
        }

        private void AddSeasonalEventItems()
        {
            Add(new QAChecklistItem("SE-001", QASystem.SeasonalEvents,
                "Active seasonal event visibility",
                new[] { "Confirm a live event is configured in LiveEventManager", "Launch app" },
                "Event banner visible in main menu; event challenges accessible from seasonal panel."));

            Add(new QAChecklistItem("SE-002", QASystem.SeasonalEvents,
                "Season rollover",
                new[] { "Set system clock to season end date + 1 s in test mode", "Relaunch" },
                "SeasonManager closes old season; new season data loaded; all progress correctly carried over."));
        }

        private void AddPerformanceItems()
        {
            Add(new QAChecklistItem("PERF-001", QASystem.Performance,
                "Stable 60 fps on target PC spec",
                new[] { "Run PerformanceBenchmarkConfig.WindowsPC reference scenario for 5 min" },
                "Average FPS ≥ 60; no frame spikes > 33 ms on a GTX 1060 / Ryzen 5 equivalent."));

            Add(new QAChecklistItem("PERF-002", QASystem.Performance,
                "Stable 30 fps on mobile baseline",
                new[] { "Run PerformanceBenchmarkConfig.iOS reference scenario for 3 min on iPhone 12" },
                "Average FPS ≥ 30; memory < 1.5 GB; no OS memory-kill events."));

            Add(new QAChecklistItem("PERF-003", QASystem.Performance,
                "Memory budget — no leak over 15-min session",
                new[] { "Profile memory in Unity Profiler over a 15-min flight" },
                "Managed heap growth < 50 MB over 15 min; no increasing texture or audio memory trend."));

            Add(new QAChecklistItem("PERF-004", QASystem.Performance,
                "Tile streaming budget compliance",
                new[] { "Fly at 1 000 m AGL for 10 min; monitor Cesium tile stats" },
                "Active tile count stays within PerformanceBenchmarkConfig platform tile budget."));
        }

        private void AddPlatformItems()
        {
            Add(new QAChecklistItem("PLT-001", QASystem.Platform,
                "Windows PC — build runs on clean machine",
                new[] { "Install Windows release build on a machine without Unity" },
                "App launches within 10 s; no missing DLL errors; reaches main menu."));

            Add(new QAChecklistItem("PLT-002", QASystem.Platform,
                "macOS — notarisation / Gatekeeper",
                new[] { "Install macOS build on an Apple Silicon Mac" },
                "App opens without 'unidentified developer' error (notarisation passed); reaches main menu."));

            Add(new QAChecklistItem("PLT-003", QASystem.Platform,
                "iOS — TestFlight build",
                new[] { "Submit to TestFlight; install on iPhone 14 or later" },
                "App installs and launches; no crash within first 5 min; no privacy-required-field alerts."));

            Add(new QAChecklistItem("PLT-004", QASystem.Platform,
                "Android — APK sideload on reference device",
                new[] { "Install release APK on a Pixel 7 or equivalent" },
                "App installs and launches; runtime permissions granted; reaches main menu."));

            Add(new QAChecklistItem("PLT-005", QASystem.Platform,
                "iPad — tablet layout active",
                new[] { "Install on iPad Pro 11-inch; launch app" },
                "TabletLayoutManager activates split-panel HUD; no phone-layout fallback triggered."));

            Add(new QAChecklistItem("PLT-006", QASystem.Platform,
                "Android Tablet — tablet layout active",
                new[] { "Install on a 10-inch Android tablet" },
                "TabletLayoutManager activates; touch zones sized correctly for tablet form factor."));
        }
    }
}
