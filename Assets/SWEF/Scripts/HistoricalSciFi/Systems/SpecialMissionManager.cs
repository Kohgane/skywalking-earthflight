// SpecialMissionManager.cs — SWEF Phase 106: Historical & Sci-Fi Flight Mode
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SWEF.HistoricalSciFi
{
    /// <summary>
    /// Phase 106 — Manager that owns the catalogue of special historical and Sci-Fi
    /// missions, tracks per-mission progress, and handles mission start/complete/fail
    /// lifecycle transitions.
    ///
    /// <para>Missions are available only when the correct aircraft and environment are
    /// active.  Mission state changes fire events that other SWEF systems can subscribe to.</para>
    /// </summary>
    public sealed class SpecialMissionManager
    {
        #region Singleton

        private static SpecialMissionManager _instance;

        /// <summary>Global singleton instance. Initialised on first access.</summary>
        public static SpecialMissionManager Instance =>
            _instance ?? (_instance = new SpecialMissionManager());

        #endregion

        #region Internal State

        private readonly Dictionary<string, SpecialMissionData> _missions =
            new Dictionary<string, SpecialMissionData>(StringComparer.Ordinal);

        private SpecialMissionData _activeMission;

        #endregion

        #region Events

        /// <summary>Raised when a mission is started.</summary>
        public event Action<SpecialMissionData> OnMissionStarted;

        /// <summary>Raised when a mission is completed successfully.</summary>
        public event Action<SpecialMissionData> OnMissionCompleted;

        /// <summary>Raised when a mission fails.</summary>
        public event Action<SpecialMissionData> OnMissionFailed;

        /// <summary>Raised when an objective within the active mission is completed.</summary>
        public event Action<SpecialMissionData, MissionObjective> OnObjectiveCompleted;

        #endregion

        #region Constructor

        private SpecialMissionManager()
        {
            PopulateBuiltInMissions();
        }

        #endregion

        #region Public API — Catalogue

        /// <summary>Returns all registered missions.</summary>
        public IReadOnlyCollection<SpecialMissionData> All => _missions.Values;

        /// <summary>Returns the currently active (in-progress) mission, or <c>null</c>.</summary>
        public SpecialMissionData ActiveMission => _activeMission;

        /// <summary>Looks up a mission by ID.  Returns <c>null</c> if not found.</summary>
        public SpecialMissionData GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _missions.TryGetValue(id, out var m);
            return m;
        }

        /// <summary>Returns all missions for the given <paramref name="category"/>.</summary>
        public IEnumerable<SpecialMissionData> GetByCategory(MissionCategory category) =>
            _missions.Values.Where(m => m.category == category);

        /// <summary>
        /// Returns all missions that are available (not locked, not already completed)
        /// given the currently selected <paramref name="aircraftId"/> and
        /// <paramref name="environmentId"/>.
        /// </summary>
        public IEnumerable<SpecialMissionData> GetAvailable(string aircraftId, string environmentId) =>
            _missions.Values.Where(m =>
                m.status == MissionStatus.Available &&
                m.requiredAircraftId == aircraftId &&
                m.requiredEnvironmentId == environmentId);

        /// <summary>
        /// Registers a custom mission.  Overwrites any existing mission with the same ID.
        /// </summary>
        public void Register(SpecialMissionData mission)
        {
            if (mission == null)                 throw new ArgumentNullException(nameof(mission));
            if (string.IsNullOrEmpty(mission.id))
                throw new ArgumentNullException(nameof(mission), "Mission ID must not be null or empty.");

            _missions[mission.id] = mission;
        }

        #endregion

        #region Public API — Lifecycle

        /// <summary>
        /// Starts the mission with the given ID.
        /// </summary>
        /// <param name="missionId">ID of the mission to start.</param>
        /// <returns>The mission that was started.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when another mission is already in progress, or when the mission is
        /// not in <see cref="MissionStatus.Available"/> state.
        /// </exception>
        public SpecialMissionData StartMission(string missionId)
        {
            if (_activeMission != null)
                throw new InvalidOperationException($"Mission '{_activeMission.id}' is already in progress.");

            if (!_missions.TryGetValue(missionId, out var mission))
                throw new ArgumentException($"Unknown mission ID: '{missionId}'", nameof(missionId));

            if (mission.status != MissionStatus.Available)
                throw new InvalidOperationException($"Mission '{missionId}' cannot be started (status: {mission.status}).");

            mission.status = MissionStatus.InProgress;
            _activeMission = mission;
            OnMissionStarted?.Invoke(mission);
            return mission;
        }

        /// <summary>
        /// Marks the objective at the given <paramref name="objectiveIndex"/> as complete
        /// within the active mission.  Auto-completes the mission when all objectives are done.
        /// </summary>
        /// <param name="objectiveIndex">Zero-based index of the objective to complete.</param>
        /// <exception cref="InvalidOperationException">Thrown when no mission is active.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is out of range.</exception>
        public void CompleteObjective(int objectiveIndex)
        {
            if (_activeMission == null)
                throw new InvalidOperationException("No mission is currently active.");

            var objectives = _activeMission.objectives;
            if (objectiveIndex < 0 || objectiveIndex >= objectives.Count)
                throw new ArgumentOutOfRangeException(nameof(objectiveIndex));

            var obj = objectives[objectiveIndex];
            if (obj.isCompleted) return; // already done

            obj.isCompleted = true;
            OnObjectiveCompleted?.Invoke(_activeMission, obj);

            if (_activeMission.AllObjectivesComplete())
                CompleteMission();
        }

        /// <summary>
        /// Manually marks the active mission as successfully completed.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when no mission is active.</exception>
        public void CompleteMission()
        {
            if (_activeMission == null)
                throw new InvalidOperationException("No mission is currently active.");

            var completed = _activeMission;
            completed.status = MissionStatus.Completed;
            _activeMission = null;
            OnMissionCompleted?.Invoke(completed);
        }

        /// <summary>
        /// Marks the active mission as failed and resets it to
        /// <see cref="MissionStatus.Available"/> so it can be retried.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when no mission is active.</exception>
        public void FailMission()
        {
            if (_activeMission == null)
                throw new InvalidOperationException("No mission is currently active.");

            var failed = _activeMission;
            failed.status = MissionStatus.Available; // allow retry
            _activeMission = null;
            OnMissionFailed?.Invoke(failed);
        }

        /// <summary>
        /// Unlocks all missions that require the given <paramref name="aircraftId"/>,
        /// changing their status from <see cref="MissionStatus.Locked"/> to
        /// <see cref="MissionStatus.Available"/>.
        /// </summary>
        /// <returns>Number of missions unlocked.</returns>
        public int UnlockMissionsForAircraft(string aircraftId)
        {
            int count = 0;
            foreach (var m in _missions.Values)
            {
                if (m.requiredAircraftId == aircraftId && m.status == MissionStatus.Locked)
                {
                    m.status = MissionStatus.Available;
                    count++;
                }
            }
            return count;
        }

        #endregion

        #region Built-In Missions

        private void PopulateBuiltInMissions()
        {
            var missions = new[]
            {
                // ── Historical: First Flight ───────────────────────────────────
                SpecialMissionData.Create(
                    id:                    "first_flight",
                    title:                 "First Flight",
                    description:           "Relive the historic moment at Kitty Hawk. Pilot the Wright Flyer into the air and sustain flight for at least 12 seconds.",
                    category:              MissionCategory.Historical,
                    requiredAircraftId:    HistoricalAircraftRegistry.IdWrightFlyer,
                    requiredEnvironmentId: SciFiEnvironmentController.IdEarth,
                    objectives:            new List<MissionObjective>
                    {
                        MissionObjective.Create("Leave the ground"),
                        MissionObjective.Create("Sustain flight for 12 seconds"),
                        MissionObjective.Create("Land safely"),
                    },
                    reward: new MissionReward
                    {
                        bonusPoints          = 500,
                        unlockedAircraftId   = HistoricalAircraftRegistry.IdSpiritOfStLouis,
                        rewardDescription    = "Unlocks the Spirit of St. Louis for future flights.",
                    }),

                // ── Historical: Cross the Atlantic ────────────────────────────
                SpecialMissionData.Create(
                    id:                    "cross_the_atlantic",
                    title:                 "Cross the Atlantic",
                    description:           "Recreate Lindbergh's legendary solo transatlantic crossing. Fly from New York to Paris without stopping.",
                    category:              MissionCategory.Historical,
                    requiredAircraftId:    HistoricalAircraftRegistry.IdSpiritOfStLouis,
                    requiredEnvironmentId: SciFiEnvironmentController.IdEarth,
                    objectives:            new List<MissionObjective>
                    {
                        MissionObjective.Create("Depart New York"),
                        MissionObjective.Create("Cross the mid-Atlantic waypoint"),
                        MissionObjective.Create("Arrive Paris (Le Bourget)"),
                    },
                    reward: new MissionReward
                    {
                        bonusPoints          = 1_000,
                        unlockedAircraftId   = HistoricalAircraftRegistry.IdSpitfire,
                        rewardDescription    = "Unlocks the Supermarine Spitfire.",
                    }),

                // ── Historical: Mach 3 Recon ──────────────────────────────────
                SpecialMissionData.Create(
                    id:                    "mach3_recon",
                    title:                 "Mach 3 Recon",
                    description:           "Fly the SR-71 Blackbird at Mach 3 above 24,000 m during a Cold War reconnaissance sortie.",
                    category:              MissionCategory.Historical,
                    requiredAircraftId:    HistoricalAircraftRegistry.IdSR71Blackbird,
                    requiredEnvironmentId: SciFiEnvironmentController.IdEarth,
                    objectives:            new List<MissionObjective>
                    {
                        MissionObjective.Create("Reach Mach 3 (≈ 3,700 km/h)"),
                        MissionObjective.Create("Climb above 24,000 m"),
                        MissionObjective.Create("Complete the recon pass"),
                        MissionObjective.Create("Return to base"),
                    },
                    reward: new MissionReward
                    {
                        bonusPoints          = 2_000,
                        unlockedAircraftId   = HistoricalAircraftRegistry.IdConcorde,
                        rewardDescription    = "Unlocks Concorde.",
                    }),

                // ── Historical: Orbital Re-entry ──────────────────────────────
                SpecialMissionData.Create(
                    id:                    "orbital_reentry",
                    title:                 "Orbital Re-entry",
                    description:           "Pilot the Space Shuttle through atmospheric re-entry and guide it to a runway landing.",
                    category:              MissionCategory.Historical,
                    requiredAircraftId:    HistoricalAircraftRegistry.IdSpaceShuttle,
                    requiredEnvironmentId: SciFiEnvironmentController.IdSpace,
                    objectives:            new List<MissionObjective>
                    {
                        MissionObjective.Create("De-orbit burn"),
                        MissionObjective.Create("Survive peak heating corridor"),
                        MissionObjective.Create("Acquire runway alignment"),
                        MissionObjective.Create("Land on runway"),
                    },
                    reward: new MissionReward
                    {
                        bonusPoints          = 5_000,
                        unlockedAircraftId   = null,
                        rewardDescription    = "Space Shuttle Commander badge awarded.",
                    }),

                // ── Sci-Fi: Lunar Survey ──────────────────────────────────────
                SpecialMissionData.Create(
                    id:                    "lunar_survey",
                    title:                 "Lunar Survey",
                    description:           "Survey three geological sites on the lunar surface, collecting samples and returning to base.",
                    category:              MissionCategory.SciFi,
                    requiredAircraftId:    HistoricalAircraftRegistry.IdSpaceShuttle,
                    requiredEnvironmentId: SciFiEnvironmentController.IdMoon,
                    objectives:            new List<MissionObjective>
                    {
                        MissionObjective.Create("Reach Site Alpha"),
                        MissionObjective.Create("Reach Site Beta"),
                        MissionObjective.Create("Reach Site Gamma"),
                        MissionObjective.Create("Return to Lunar Base"),
                    },
                    reward: new MissionReward
                    {
                        bonusPoints          = 3_000,
                        unlockedAircraftId   = null,
                        rewardDescription    = "Lunar Explorer badge awarded.",
                    }),

                // ── Sci-Fi: Mars Colony Supply Run ────────────────────────────
                SpecialMissionData.Create(
                    id:                    "mars_colony_supply_run",
                    title:                 "Mars Colony Supply Run",
                    description:           "Deliver critical supplies to the Mars colony outpost, navigating dust storms and thin atmosphere.",
                    category:              MissionCategory.SciFi,
                    requiredAircraftId:    HistoricalAircraftRegistry.IdSpaceShuttle,
                    requiredEnvironmentId: SciFiEnvironmentController.IdMars,
                    objectives:            new List<MissionObjective>
                    {
                        MissionObjective.Create("Aerobrake into Mars orbit"),
                        MissionObjective.Create("Avoid dust storm sector"),
                        MissionObjective.Create("Land at Colony Base Ares-1"),
                    },
                    reward: new MissionReward
                    {
                        bonusPoints          = 4_000,
                        unlockedAircraftId   = null,
                        rewardDescription    = "Mars Pioneer badge awarded.",
                    }),
            };

            foreach (var m in missions)
                Register(m);
        }

        #endregion
    }
}
