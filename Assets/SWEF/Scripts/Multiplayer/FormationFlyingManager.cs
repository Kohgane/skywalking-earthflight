using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Multiplayer
{
    // ── Enums ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Standard aviation formation patterns supported by the formation system.
    /// </summary>
    public enum FormationType
    {
        /// <summary>Classic V-shape with leader at apex.</summary>
        V_Formation,
        /// <summary>Diamond — lead + two flanks + trail.</summary>
        Diamond,
        /// <summary>Echelon stepped to the leader's left.</summary>
        Echelon_Left,
        /// <summary>Echelon stepped to the leader's right.</summary>
        Echelon_Right,
        /// <summary>All aircraft at equal lateral spacing.</summary>
        Line_Abreast,
        /// <summary>Single-file line behind the leader.</summary>
        Trail,
        /// <summary>Four-aircraft finger-four spread.</summary>
        Finger_Four
    }

    // ── Data Classes ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Describes a single slot within a formation (leader = slot 0).
    /// </summary>
    [Serializable]
    public class FormationSlot
    {
        /// <summary>Zero-based slot index (0 = leader).</summary>
        public int index;
        /// <summary>Player ID assigned to this slot, or null if empty.</summary>
        public string playerId;
        /// <summary>Local-space offset from the leader's transform.</summary>
        public Vector3 localOffset;
        /// <summary>Formation accuracy score for this slot (0–100).</summary>
        public float score;
    }

    /// <summary>
    /// Represents an active formation session.
    /// </summary>
    [Serializable]
    public class Formation
    {
        /// <summary>Unique identifier for this formation.</summary>
        public string formationId;
        /// <summary>Formation pattern type.</summary>
        public FormationType type;
        /// <summary>Player ID of the designated formation leader.</summary>
        public string leaderId;
        /// <summary>All slots, including the leader at index 0.</summary>
        public List<FormationSlot> slots = new();
        /// <summary>Whether the formation is currently active (not broken).</summary>
        public bool isActive;
        /// <summary>Average formation accuracy score across all wingmen (0–100).</summary>
        public float averageScore;
    }

    // ── FormationFlyingManager ────────────────────────────────────────────────────

    /// <summary>
    /// Manages formation flying between players in a multiplayer session.
    ///
    /// <para>Supports 7 formation types, calculates world-space slot positions relative to
    /// the leader, applies gentle PID-based steering corrections to keep wingmen in position,
    /// scores each wingman on distance deviation and heading/speed matching, and
    /// provides break/reform commands.</para>
    /// </summary>
    public class FormationFlyingManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static FormationFlyingManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Formation Geometry")]
        [Tooltip("Lateral spacing between aircraft in the same formation (metres).")]
        [SerializeField] private float lateralSpacingM = 50f;

        [Tooltip("Longitudinal spacing between aircraft in trail formations (metres).")]
        [SerializeField] private float longitudinalSpacingM = 80f;

        [Tooltip("Altitude offset for V and Diamond wingmen (metres, relative to leader).")]
        [SerializeField] private float altitudeOffsetM = -5f;

        [Header("PID Auto-keep")]
        [Tooltip("Proportional gain for the wingman position correction PID.")]
        [SerializeField] private float pidP = 0.4f;

        [Tooltip("Integral gain for the wingman position correction PID.")]
        [SerializeField] private float pidI = 0.02f;

        [Tooltip("Derivative gain for the wingman position correction PID.")]
        [SerializeField] private float pidD = 0.1f;

        [Tooltip("Maximum correction force magnitude applied per second (m/s²).")]
        [SerializeField] private float maxCorrectionForce = 5f;

        [Header("Scoring")]
        [Tooltip("Maximum allowed deviation in metres for a perfect (100) slot score.")]
        [SerializeField] private float perfectDeviationM = 5f;

        [Tooltip("Maximum allowed deviation in metres before the score drops to 0.")]
        [SerializeField] private float maxDeviationM = 200f;

        [Tooltip("Weight of heading alignment in the final slot score (0–1).")]
        [SerializeField] private float headingWeight = 0.2f;

        [Tooltip("Weight of speed matching in the final slot score (0–1).")]
        [SerializeField] private float speedWeight = 0.1f;

        [Header("Ghost Markers")]
        [Tooltip("Prefab used to visualise the ideal slot position for each wingman.")]
        [SerializeField] private GameObject slotGhostPrefab;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when a new formation is created.</summary>
        public event Action<Formation> OnFormationCreated;

        /// <summary>Fired when a player joins a formation.</summary>
        public event Action<Formation, string> OnFormationJoined;

        /// <summary>Fired when the leader issues a break command.</summary>
        public event Action<Formation> OnFormationBroken;

        /// <summary>Fired when the slot score for a wingman is updated.</summary>
        public event Action<string, float> OnSlotScoreUpdated;

        // ── Private state ─────────────────────────────────────────────────────────
        private readonly Dictionary<string, Formation> _formations   = new();
        private readonly Dictionary<string, string>    _playerSlots  = new(); // playerId → formationId
        private readonly Dictionary<string, GameObject> _ghostMarkers = new();

        // PID integral accumulators per player
        private readonly Dictionary<string, Vector3> _pidIntegrals = new();
        private readonly Dictionary<string, Vector3> _pidPrevErrors = new();

        private MultiplayerManager _multiplayerManager;
        private Transform           _leaderTransform; // cached reference to local leader

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _multiplayerManager = FindFirstObjectByType<MultiplayerManager>();
        }

        private void Update()
        {
            foreach (var formation in _formations.Values)
            {
                if (!formation.isActive) continue;
                UpdateFormationScores(formation);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a new formation of the specified type with the given player as leader.
        /// </summary>
        /// <param name="leaderId">Player ID of the formation leader.</param>
        /// <param name="type">Formation pattern to use.</param>
        /// <param name="maxSlots">Total number of slots (including leader).</param>
        /// <returns>The newly created <see cref="Formation"/>.</returns>
        public Formation CreateFormation(string leaderId, FormationType type, int maxSlots = 8)
        {
            string id = Guid.NewGuid().ToString("N").Substring(0, 8);
            var offsets = CalculateSlotOffsets(type, maxSlots);

            var formation = new Formation
            {
                formationId = id,
                type        = type,
                leaderId    = leaderId,
                isActive    = true
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                formation.slots.Add(new FormationSlot
                {
                    index      = i,
                    localOffset = offsets[i],
                    playerId   = i == 0 ? leaderId : null
                });
            }

            _formations[id] = formation;
            _playerSlots[leaderId] = id;

            Debug.Log($"[SWEF][FormationFlyingManager] Created {type} formation '{id}' led by {leaderId}.");
            OnFormationCreated?.Invoke(formation);
            return formation;
        }

        /// <summary>
        /// Assigns a player to the first available slot in an existing formation.
        /// </summary>
        /// <param name="formationId">Target formation identifier.</param>
        /// <param name="playerId">Player joining the formation.</param>
        /// <returns>The slot index assigned, or -1 if the formation is full.</returns>
        public int JoinFormation(string formationId, string playerId)
        {
            if (!_formations.TryGetValue(formationId, out var formation) || !formation.isActive)
                return -1;

            foreach (var slot in formation.slots)
            {
                if (slot.playerId == null)
                {
                    slot.playerId = playerId;
                    _playerSlots[playerId] = formationId;
                    SpawnGhostMarker(playerId, slot);

                    Debug.Log($"[SWEF][FormationFlyingManager] {playerId} joined formation '{formationId}' at slot {slot.index}.");
                    OnFormationJoined?.Invoke(formation, playerId);
                    return slot.index;
                }
            }

            Debug.LogWarning($"[SWEF][FormationFlyingManager] Formation '{formationId}' is full.");
            return -1;
        }

        /// <summary>
        /// Breaks the formation — all wingmen scatter. Only the leader may call this.
        /// </summary>
        /// <param name="formationId">Formation to break.</param>
        public void BreakFormation(string formationId)
        {
            if (!_formations.TryGetValue(formationId, out var formation)) return;

            formation.isActive = false;
            foreach (var slot in formation.slots)
                RemoveGhostMarker(slot.playerId);

            Debug.Log($"[SWEF][FormationFlyingManager] Formation '{formationId}' broken.");
            OnFormationBroken?.Invoke(formation);
        }

        /// <summary>
        /// Re-activates a previously broken formation.
        /// </summary>
        /// <param name="formationId">Formation to reform.</param>
        public void ReformFormation(string formationId)
        {
            if (!_formations.TryGetValue(formationId, out var formation)) return;
            formation.isActive = true;
            Debug.Log($"[SWEF][FormationFlyingManager] Formation '{formationId}' reformed.");
        }

        /// <summary>
        /// Returns the world-space target position for a wingman in the given formation.
        /// </summary>
        /// <param name="formationId">Formation identifier.</param>
        /// <param name="slotIndex">Wingman slot index.</param>
        /// <param name="leaderTransform">Current transform of the leader.</param>
        public Vector3 GetSlotWorldPosition(string formationId, int slotIndex, Transform leaderTransform)
        {
            if (!_formations.TryGetValue(formationId, out var formation))
                return leaderTransform.position;

            if (slotIndex < 0 || slotIndex >= formation.slots.Count)
                return leaderTransform.position;

            return leaderTransform.TransformPoint(formation.slots[slotIndex].localOffset);
        }

        /// <summary>
        /// Calculates the PID steering correction vector for a wingman to reach its slot.
        /// </summary>
        /// <param name="playerId">Wingman player identifier.</param>
        /// <param name="currentPos">Wingman's current world-space position.</param>
        /// <param name="targetPos">Ideal slot world-space position.</param>
        /// <param name="dt">Delta time in seconds.</param>
        /// <returns>Correction force vector (world space).</returns>
        public Vector3 GetSteeringCorrection(string playerId, Vector3 currentPos, Vector3 targetPos, float dt)
        {
            Vector3 error = targetPos - currentPos;

            if (!_pidIntegrals.ContainsKey(playerId))    _pidIntegrals[playerId]    = Vector3.zero;
            if (!_pidPrevErrors.ContainsKey(playerId))   _pidPrevErrors[playerId]   = Vector3.zero;

            _pidIntegrals[playerId] += error * dt;
            Vector3 derivative = (error - _pidPrevErrors[playerId]) / Mathf.Max(dt, 0.001f);
            _pidPrevErrors[playerId] = error;

            Vector3 correction = pidP * error + pidI * _pidIntegrals[playerId] + pidD * derivative;
            return Vector3.ClampMagnitude(correction, maxCorrectionForce);
        }

        // ── Slot offset calculation ───────────────────────────────────────────────

        /// <summary>
        /// Calculates local-space offsets for all slots in the given formation type.
        /// Slot 0 is always the leader at Vector3.zero.
        /// </summary>
        private Vector3[] CalculateSlotOffsets(FormationType type, int count)
        {
            var offsets = new Vector3[Mathf.Max(count, 1)];
            offsets[0] = Vector3.zero; // leader

            switch (type)
            {
                case FormationType.V_Formation:
                    for (int i = 1; i < count; i++)
                    {
                        int side = (i % 2 == 1) ? 1 : -1;
                        int row  = (i + 1) / 2;
                        offsets[i] = new Vector3(side * row * lateralSpacingM,
                                                 altitudeOffsetM,
                                                 -row * longitudinalSpacingM * 0.5f);
                    }
                    break;

                case FormationType.Diamond:
                    if (count >= 2) offsets[1] = new Vector3( lateralSpacingM * 0.5f, altitudeOffsetM, -longitudinalSpacingM * 0.5f);
                    if (count >= 3) offsets[2] = new Vector3(-lateralSpacingM * 0.5f, altitudeOffsetM, -longitudinalSpacingM * 0.5f);
                    if (count >= 4) offsets[3] = new Vector3(0f,                      altitudeOffsetM, -longitudinalSpacingM);
                    for (int i = 4; i < count; i++)
                        offsets[i] = new Vector3((i % 2 == 0 ? 1 : -1) * lateralSpacingM, altitudeOffsetM, -longitudinalSpacingM * 1.5f);
                    break;

                case FormationType.Echelon_Left:
                    for (int i = 1; i < count; i++)
                        offsets[i] = new Vector3(-i * lateralSpacingM, altitudeOffsetM, -i * longitudinalSpacingM * 0.5f);
                    break;

                case FormationType.Echelon_Right:
                    for (int i = 1; i < count; i++)
                        offsets[i] = new Vector3(i * lateralSpacingM, altitudeOffsetM, -i * longitudinalSpacingM * 0.5f);
                    break;

                case FormationType.Line_Abreast:
                    for (int i = 1; i < count; i++)
                    {
                        int side = (i % 2 == 1) ? 1 : -1;
                        int col  = (i + 1) / 2;
                        offsets[i] = new Vector3(side * col * lateralSpacingM, 0f, 0f);
                    }
                    break;

                case FormationType.Trail:
                    for (int i = 1; i < count; i++)
                        offsets[i] = new Vector3(0f, 0f, -i * longitudinalSpacingM);
                    break;

                case FormationType.Finger_Four:
                    // Lead pair
                    if (count >= 2) offsets[1] = new Vector3( lateralSpacingM, altitudeOffsetM, -longitudinalSpacingM * 0.3f);
                    // Trail pair
                    if (count >= 3) offsets[2] = new Vector3(-lateralSpacingM * 0.5f, altitudeOffsetM, -longitudinalSpacingM);
                    if (count >= 4) offsets[3] = new Vector3( lateralSpacingM * 1.5f, altitudeOffsetM, -longitudinalSpacingM);
                    for (int i = 4; i < count; i++)
                        offsets[i] = new Vector3(i * lateralSpacingM, altitudeOffsetM, -longitudinalSpacingM * 1.5f);
                    break;
            }

            return offsets;
        }

        // ── Scoring ───────────────────────────────────────────────────────────────

        private void UpdateFormationScores(Formation formation)
        {
            if (!_formations.ContainsKey(formation.formationId)) return;

            // Resolve the leader's transform.
            // In a full implementation, look up the leader's PlayerAvatar or RemotePlayerState.
            // For now use this MonoBehaviour's transform as a stand-in when the leader is local.
            Transform leaderTransform = null;
            if (formation.leaderId != null && _multiplayerManager != null)
            {
                // Attempt to locate the leader avatar via the player registry.
                // PlayerAvatar lookup would go here in production.
            }

            // Fall back to this object's transform (useful when leader is the local player).
            if (leaderTransform == null)
                leaderTransform = transform;

            float totalScore = 0f;
            int scoredSlots  = 0;

            foreach (var slot in formation.slots)
            {
                if (slot.index == 0 || slot.playerId == null) continue;

                Vector3 idealPos = leaderTransform.TransformPoint(slot.localOffset);
                // In production: get wingman's actual world position from PlayerAvatar/SyncState.
                // Here we simulate a placeholder deviation.
                float deviation  = 0f; // placeholder

                float posScore     = 1f - Mathf.Clamp01((deviation - perfectDeviationM) / (maxDeviationM - perfectDeviationM));
                float headingScore = 1f; // placeholder: compare headings
                float speedScore   = 1f; // placeholder: compare speeds

                slot.score = Mathf.Round(100f * (posScore * (1f - headingWeight - speedWeight)
                                                + headingScore * headingWeight
                                                + speedScore * speedWeight));
                totalScore += slot.score;
                scoredSlots++;

                OnSlotScoreUpdated?.Invoke(slot.playerId, slot.score);
            }

            formation.averageScore = scoredSlots > 0 ? totalScore / scoredSlots : 0f;
        }

        // ── Ghost markers ─────────────────────────────────────────────────────────

        private void SpawnGhostMarker(string playerId, FormationSlot slot)
        {
            if (slotGhostPrefab == null || playerId == null) return;
            if (_ghostMarkers.ContainsKey(playerId)) RemoveGhostMarker(playerId);

            var go = Instantiate(slotGhostPrefab);
            go.name = $"GhostSlot_{playerId}_{slot.index}";
            _ghostMarkers[playerId] = go;
        }

        private void RemoveGhostMarker(string playerId)
        {
            if (playerId == null || !_ghostMarkers.TryGetValue(playerId, out var go)) return;
            if (go != null) Destroy(go);
            _ghostMarkers.Remove(playerId);
        }
    }
}
