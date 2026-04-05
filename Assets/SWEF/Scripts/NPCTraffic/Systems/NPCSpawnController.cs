// NPCSpawnController.cs — Phase 110: Dynamic NPC & Air Traffic Ecosystem
// Handles NPC aircraft spawn, despawn, and GameObject object-pool management.
// Namespace: SWEF.NPCTraffic

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.NPCTraffic
{
    /// <summary>
    /// Phase 110 — Manages the spawn and despawn lifecycle of NPC aircraft,
    /// including object pooling of GameObjects, category weighting, and initial
    /// data population before handing off to <see cref="NPCAircraftController"/>.
    /// </summary>
    public sealed class NPCSpawnController : MonoBehaviour
    {
        #region Inspector

        [Header("Spawn Weights")]
        [Tooltip("Relative weight of each category when randomly selecting an NPC type to spawn.")]
        [SerializeField] private float _weightCommercial  = 50f;
        [SerializeField] private float _weightPrivateJet  = 15f;
        [SerializeField] private float _weightCargo       = 15f;
        [SerializeField] private float _weightMilitary    =  5f;
        [SerializeField] private float _weightHelicopter  = 10f;
        [SerializeField] private float _weightTraining    =  5f;

        [Header("Pool")]
        [Tooltip("Number of NPCAircraftData objects to pre-allocate into the pool.")]
        [Range(0, 50)]
        [SerializeField] private int _preWarmPoolSize = 20;

        #endregion

        #region Private State

        private readonly List<(NPCAircraftCategory Cat, float Weight)> _spawnTable =
            new List<(NPCAircraftCategory, float)>();

        private readonly Queue<NPCAircraftData> _dataPool = new Queue<NPCAircraftData>();

        private float _totalWeight;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            BuildSpawnTable();
            PreWarmPool();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Attempts to spawn a single NPC near the given world-space position.
        /// </summary>
        /// <param name="nearPosition">Centre of the spawn area.</param>
        /// <param name="spawnRadiusMetres">Maximum distance from centre to spawn.</param>
        /// <returns>Populated <see cref="NPCAircraftData"/>, or <c>null</c> if spawn failed.</returns>
        public NPCAircraftData SpawnNear(Vector3 nearPosition, float spawnRadiusMetres)
        {
            NPCAircraftCategory category = PickCategory();
            NPCFlightProfile    profile  = GetProfile(category);

            // Position on the perimeter of the spawn radius so NPCs arrive rather
            // than pop into existence right next to the player
            Vector2 disk     = UnityEngine.Random.insideUnitCircle.normalized * spawnRadiusMetres;
            Vector3 spawnPos = nearPosition + new Vector3(disk.x, 0f, disk.y)
                               + Vector3.up * profile.CruiseAltitudeMetres;

            NPCAircraftData data = RentData();
            data.Id             = NPCTrafficManager.Instance != null
                                      ? NPCTrafficManager.Instance.GenerateId()
                                      : Guid.NewGuid().ToString("N")[..8];
            data.Callsign       = NPCCallsignGenerator.Generate(category);
            data.Category       = category;
            data.AircraftType   = GetDefaultAircraftType(category);
            data.WorldPosition  = spawnPos;
            data.AltitudeMetres = profile.CruiseAltitudeMetres;
            data.SpeedKnots     = profile.CruiseSpeedKnots;
            data.HeadingDeg     = UnityEngine.Random.Range(0f, 360f);
            data.BehaviorState  = NPCBehaviorState.Climbing;
            data.IsVisible      = true;
            data.SpawnTime      = Time.time;
            data.OperatorName   = data.Callsign[..Mathf.Min(3, data.Callsign.Length)];

            return data;
        }

        /// <summary>
        /// Returns a data object to the internal pool.
        /// </summary>
        /// <param name="data">Data to return.</param>
        public void ReturnData(NPCAircraftData data)
        {
            if (data == null) return;
            data.IsVisible    = false;
            data.BehaviorState = NPCBehaviorState.Parked;
            _dataPool.Enqueue(data);
        }

        #endregion

        #region Private — Category Selection

        private void BuildSpawnTable()
        {
            _spawnTable.Clear();
            _spawnTable.Add((NPCAircraftCategory.CommercialAirline, _weightCommercial));
            _spawnTable.Add((NPCAircraftCategory.PrivateJet,        _weightPrivateJet));
            _spawnTable.Add((NPCAircraftCategory.CargoPlane,        _weightCargo));
            _spawnTable.Add((NPCAircraftCategory.MilitaryAircraft,  _weightMilitary));
            _spawnTable.Add((NPCAircraftCategory.Helicopter,        _weightHelicopter));
            _spawnTable.Add((NPCAircraftCategory.TrainingAircraft,  _weightTraining));

            _totalWeight = 0f;
            foreach (var entry in _spawnTable) _totalWeight += entry.Weight;
        }

        private NPCAircraftCategory PickCategory()
        {
            float roll = UnityEngine.Random.Range(0f, _totalWeight);
            float cumulative = 0f;
            foreach (var (cat, weight) in _spawnTable)
            {
                cumulative += weight;
                if (roll <= cumulative) return cat;
            }
            return NPCAircraftCategory.CommercialAirline;
        }

        #endregion

        #region Private — Pool

        private void PreWarmPool()
        {
            for (int i = 0; i < _preWarmPoolSize; i++)
                _dataPool.Enqueue(new NPCAircraftData());
        }

        private NPCAircraftData RentData()
        {
            return _dataPool.Count > 0 ? _dataPool.Dequeue() : new NPCAircraftData();
        }

        #endregion

        #region Private — Helpers

        private static NPCFlightProfile GetProfile(NPCAircraftCategory category)
        {
            if (NPCTrafficManager.Instance != null)
                return NPCTrafficManager.Instance.GetProfile(category);

            return new NPCFlightProfile { Category = category, CruiseSpeedKnots = 200f, CruiseAltitudeMetres = 3000f };
        }

        private static string GetDefaultAircraftType(NPCAircraftCategory category) =>
            category switch
            {
                NPCAircraftCategory.CommercialAirline => "B738",
                NPCAircraftCategory.PrivateJet        => "C56X",
                NPCAircraftCategory.CargoPlane        => "B77F",
                NPCAircraftCategory.MilitaryAircraft  => "F18",
                NPCAircraftCategory.Helicopter        => "EC35",
                NPCAircraftCategory.TrainingAircraft  => "C172",
                _                                    => "UNKN"
            };

        #endregion
    }
}
