using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Wildlife
{
    /// <summary>
    /// Phase 53 — Connects wildlife encounters to the Journal and collection systems.
    ///
    /// <para>Maintains the Wildlife Codex (catalogue of all discoverable species),
    /// tracks first-discovery events, integrates with PhotoMode for photo bonuses,
    /// generates journal entries, awards biome-completion achievements, and
    /// saves/loads discovery data.</para>
    ///
    /// <para>Integration points:
    /// <list type="bullet">
    ///   <item><c>SWEF.Journal.JournalManager</c> — auto-creates journal entries.</item>
    ///   <item><c>SWEF.Achievement.AchievementManager</c> — unlocks collection achievements.</item>
    ///   <item><c>SWEF.Narration.NarrationManager</c> — triggers first-discovery narration.</item>
    ///   <item><c>SWEF.PhotoMode.PhotoCaptureManager</c> — photo bonus on species capture.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class WildlifeJournalIntegration : MonoBehaviour
    {
        #region Constants

        private const string SaveKey              = "SWEF_WildlifeDiscoveries";
        private const string PhotoBonusAchievement= "wildlife_photographer";
        private const int    PhotoBonusThreshold  = 10;  // distinct species photographed

        #endregion

        #region Inspector

        [Header("Codex Configuration")]
        [Tooltip("Complete list of species available for discovery.")]
        [SerializeField] private List<AnimalSpecies> allSpecies = new List<AnimalSpecies>();

        [Header("References")]
        [Tooltip("WildlifeManager to subscribe to encounter events. Resolved at runtime if null.")]
        [SerializeField] private WildlifeManager wildlifeManager;

        #endregion

        #region Events

        /// <summary>Fired the first time a species is encountered.</summary>
        public event Action<AnimalSpecies> OnSpeciesDiscovered;

        /// <summary>Fired when a biome's species collection is complete.</summary>
        public event Action<BiomeHabitat> OnBiomeCompleted;

        #endregion

        #region Public Properties

        /// <summary>Total number of species the player has discovered.</summary>
        public int DiscoveredCount => _discovered.Count;

        /// <summary>Total number of species in the codex.</summary>
        public int TotalSpecies => allSpecies.Count;

        /// <summary>Completion percentage across all species (0–1).</summary>
        public float CompletionRatio =>
            TotalSpecies == 0 ? 0f : (float)DiscoveredCount / TotalSpecies;

        #endregion

        #region Private State

        private readonly HashSet<string>            _discovered       = new HashSet<string>();
        private readonly HashSet<string>            _photographed     = new HashSet<string>();
        private readonly List<WildlifeEncounter>    _encounterHistory = new List<WildlifeEncounter>();

        // Cross-system references (guarded by conditional compilation)
        private Component _journalManager;
        private Component _achievementManager;
        private Component _narrationManager;
        private Component _photoCaptureManager;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ResolveReferences();
            LoadDiscoveries();
        }

        private void Start()
        {
            if (wildlifeManager == null)
                wildlifeManager = FindFirstObjectByType<WildlifeManager>();

            if (wildlifeManager != null)
            {
                wildlifeManager.OnWildlifeEncounter += HandleEncounter;
                wildlifeManager.OnRareAnimalFound   += HandleRareAnimal;
            }
        }

        private void OnDestroy()
        {
            if (wildlifeManager != null)
            {
                wildlifeManager.OnWildlifeEncounter -= HandleEncounter;
                wildlifeManager.OnRareAnimalFound   -= HandleRareAnimal;
            }
        }

        #endregion

        #region Public API

        /// <summary>Returns true if the species with the given name has been discovered.</summary>
        public bool IsDiscovered(string speciesName) => _discovered.Contains(speciesName);

        /// <summary>Returns true if the species has been photographed.</summary>
        public bool IsPhotographed(string speciesName) => _photographed.Contains(speciesName);

        /// <summary>Marks a species as photographed (called by PhotoMode integration).</summary>
        public void RecordPhotograph(string speciesName)
        {
            if (string.IsNullOrEmpty(speciesName)) return;
            _photographed.Add(speciesName);
            SaveDiscoveries();

            if (_photographed.Count >= PhotoBonusThreshold)
                AwardAchievement(PhotoBonusAchievement);
        }

        /// <summary>Calculates the discovery completion percentage for a given biome (0–1).</summary>
        public float GetBiomeCompletion(BiomeHabitat biome)
        {
            int total = 0, found = 0;
            foreach (var s in allSpecies)
            {
                if (!s.habitats.Contains(biome)) continue;
                total++;
                if (_discovered.Contains(s.speciesName)) found++;
            }
            return total == 0 ? 0f : (float)found / total;
        }

        /// <summary>Returns rarity badge text for a given rarity tier.</summary>
        public static string GetRarityBadge(AnimalRarity rarity)
        {
            switch (rarity)
            {
                case AnimalRarity.Common:    return "★";
                case AnimalRarity.Uncommon:  return "★★";
                case AnimalRarity.Rare:      return "★★★";
                case AnimalRarity.Legendary: return "★★★★";
                default:                     return "★";
            }
        }

        /// <summary>Returns the full encounter history.</summary>
        public IReadOnlyList<WildlifeEncounter> GetEncounterHistory() => _encounterHistory;

        #endregion

        #region Event Handlers

        private void HandleEncounter(WildlifeEncounter encounter)
        {
            _encounterHistory.Add(encounter);
            bool firstTime = _discovered.Add(encounter.speciesName);

            if (!firstTime) return;

            AnimalSpecies species = FindSpecies(encounter.speciesName);
            if (species != null)
            {
                OnSpeciesDiscovered?.Invoke(species);
                CreateJournalEntry(species, encounter);
                TriggerNarration(species);
                CheckBiomeCompletion(species);
                AwardAchievement("wildlife_species_" + _discovered.Count);
            }

            SaveDiscoveries();
        }

        private void HandleRareAnimal(AnimalSpecies species)
        {
            if (species == null) return;
            AwardAchievement("wildlife_rare_found");
        }

        #endregion

        #region Journal Entry

        private void CreateJournalEntry(AnimalSpecies species, WildlifeEncounter encounter)
        {
#if SWEF_JOURNAL_AVAILABLE
            var jm = _journalManager as SWEF.Journal.JournalManager;
            if (jm == null) return;
            string text = $"Discovered {species.speciesName} " +
                          $"({GetRarityBadge(species.rarity)}) " +
                          $"at altitude {encounter.position.y:F0}m. " +
                          $"{species.description}";
            jm.AddAutoEntry(text);
#endif
        }

        private void TriggerNarration(AnimalSpecies species)
        {
#if SWEF_NARRATION_AVAILABLE
            var nm = _narrationManager as SWEF.Narration.NarrationManager;
            nm?.TriggerNarration("wildlife_" + species.speciesName.ToLower().Replace(" ", "_"));
#endif
        }

        #endregion

        #region Achievement

        private void AwardAchievement(string key)
        {
#if SWEF_ACHIEVEMENT_AVAILABLE
            var am = _achievementManager as SWEF.Achievement.AchievementManager;
            am?.RecordProgress(key, 1);
#endif
        }

        #endregion

        #region Biome Completion

        private void CheckBiomeCompletion(AnimalSpecies species)
        {
            foreach (var biome in species.habitats)
            {
                if (Mathf.Approximately(GetBiomeCompletion(biome), 1f))
                    OnBiomeCompleted?.Invoke(biome);
            }
        }

        #endregion

        #region Persistence

        private void SaveDiscoveries()
        {
            string data = string.Join(",", _discovered) + "|" + string.Join(",", _photographed);
            PlayerPrefs.SetString(SaveKey, data);
            PlayerPrefs.Save();
        }

        private void LoadDiscoveries()
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return;
            string raw = PlayerPrefs.GetString(SaveKey);
            if (string.IsNullOrEmpty(raw)) return;

            string[] parts = raw.Split('|');
            if (parts.Length >= 1 && !string.IsNullOrEmpty(parts[0]))
                foreach (string s in parts[0].Split(','))
                    if (!string.IsNullOrEmpty(s)) _discovered.Add(s);

            if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[1]))
                foreach (string s in parts[1].Split(','))
                    if (!string.IsNullOrEmpty(s)) _photographed.Add(s);
        }

        #endregion

        #region Helpers

        private AnimalSpecies FindSpecies(string name)
        {
            foreach (var s in allSpecies)
                if (s.speciesName == name) return s;
            return null;
        }

        private void ResolveReferences()
        {
#if SWEF_JOURNAL_AVAILABLE
            _journalManager = FindFirstObjectByType<SWEF.Journal.JournalManager>();
#endif
#if SWEF_ACHIEVEMENT_AVAILABLE
            _achievementManager = FindFirstObjectByType<SWEF.Achievement.AchievementManager>();
#endif
#if SWEF_NARRATION_AVAILABLE
            _narrationManager = FindFirstObjectByType<SWEF.Narration.NarrationManager>();
#endif
#if SWEF_PHOTOMODE_AVAILABLE
            _photoCaptureManager = FindFirstObjectByType<SWEF.PhotoMode.PhotoCaptureManager>();
#endif
        }

        #endregion
    }
}
