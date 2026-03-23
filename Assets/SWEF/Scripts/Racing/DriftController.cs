// DriftController.cs — SWEF Boost & Drift Mechanics System (Phase 62)
using System;
using UnityEngine;

namespace SWEF.Racing
{
    /// <summary>
    /// Phase 62 — Singleton that manages the 4-level drift charge system.
    ///
    /// <para>Drift is initiated by a simultaneous brake + turn input. Holding the drift
    /// accumulates charge time which advances through four levels (Blue → Orange →
    /// Purple → UltraPurple). Releasing grants a boost proportional to the achieved
    /// level. The controller modifies yaw/roll via callbacks consumed by
    /// <c>FlightController</c>, reduces grip for a slide feel, and drives spark
    /// particle colour transitions.</para>
    ///
    /// <para>Attach to the same persistent GameObject as <see cref="BoostController"/>.</para>
    /// </summary>
    public class DriftController : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static DriftController Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Configuration")]
        [Tooltip("Drift behaviour config (charge thresholds, turn rate, grip reduction).")]
        [SerializeField] private DriftConfig driftConfig;

        [Header("Input Thresholds")]
        [Tooltip("Minimum absolute turn input axis value required to initiate or sustain a drift.")]
        [SerializeField] private float minTurnInputThreshold = 0.3f;

        [Tooltip("Minimum brake input value required to initiate a drift.")]
        [SerializeField] private float minBrakeInputThreshold = 0.2f;

        [Header("Mini-Turbo Chain")]
        [Tooltip("Whether quick successive drift releases trigger a chain mini-turbo bonus.")]
        [SerializeField] private bool enableMiniTurboChain = true;

        #endregion

        #region Events

        /// <summary>Fired when a drift is initiated (passes the locked direction).</summary>
        public event Action<DriftDirection> OnDriftStart;

        /// <summary>Fired each time the drift charge advances to a new level.</summary>
        public event Action<DriftLevel> OnDriftLevelUp;

        /// <summary>
        /// Fired when the player releases the drift — passes the achieved level
        /// and the <see cref="BoostConfig"/> that will be applied.
        /// </summary>
        public event Action<DriftLevel, BoostConfig> OnDriftRelease;

        /// <summary>Fired when the drift is cancelled (e.g. collision), passes the level at cancellation.</summary>
        public event Action<DriftLevel> OnDriftCancel;

        #endregion

        #region Public Properties

        /// <summary>Current snapshot of the drift state.</summary>
        public DriftState State => _state;

        /// <summary>Whether a drift is currently active.</summary>
        public bool IsDrifting => _state.active;

        /// <summary>
        /// Current turn-rate multiplier for <c>FlightController</c>.
        /// Returns 1.0 when not drifting; applies <see cref="DriftConfig.turnRateMultiplier"/> otherwise.
        /// </summary>
        public float TurnRateMultiplier =>
            (_state.active && driftConfig != null) ? driftConfig.turnRateMultiplier : 1f;

        /// <summary>
        /// Grip reduction fraction (0–1) to apply to flight physics.
        /// Non-zero only during an active drift.
        /// </summary>
        public float GripReduction =>
            (_state.active && driftConfig != null) ? driftConfig.gripReductionPercent : 0f;

        /// <summary>
        /// Current spark colour that VFX systems should use for drift particle emission.
        /// </summary>
        public Color CurrentSparkColor { get; private set; } = Color.blue;

        #endregion

        #region Private State

        private DriftState _state;
        private float      _lastReleaseTime = -999f;
        private int        _chainCount;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (_state.active)
                TickDriftCharge(Time.deltaTime);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Called by the input layer each frame with the current brake and turn values.
        /// Handles drift initiation and release detection.
        /// </summary>
        /// <param name="brakeInput">0–1 normalised brake axis value.</param>
        /// <param name="turnInput">–1 (left) to +1 (right) turn axis value.</param>
        public void ProcessInput(float brakeInput, float turnInput)
        {
            bool wantsDrift = brakeInput >= minBrakeInputThreshold
                              && Mathf.Abs(turnInput) >= minTurnInputThreshold;

            if (!_state.active && wantsDrift)
            {
                InitiateDrift(turnInput > 0f ? DriftDirection.Right : DriftDirection.Left);
            }
            else if (_state.active && !wantsDrift)
            {
                ReleaseDrift();
            }
        }

        /// <summary>
        /// Immediately cancels the current drift without granting a boost reward.
        /// Called externally on collisions or other interruptions.
        /// </summary>
        public void CancelDrift()
        {
            if (!_state.active) return;
            DriftLevel levelAtCancel = _state.currentLevel;
            _state.Reset();
            OnDriftCancel?.Invoke(levelAtCancel);
        }

        #endregion

        #region Private Helpers

        private void InitiateDrift(DriftDirection dir)
        {
            _state.Reset();
            _state.active    = true;
            _state.direction = dir;
            CurrentSparkColor = GetSparkColor(DriftLevel.None);
            OnDriftStart?.Invoke(dir);
        }

        private void TickDriftCharge(float dt)
        {
            if (driftConfig == null) return;

            _state.chargeTime += dt;
            DriftLevel newLevel = ComputeLevel(_state.chargeTime);

            if (newLevel != _state.currentLevel)
            {
                _state.currentLevel = newLevel;
                CurrentSparkColor   = GetSparkColor(newLevel);
                OnDriftLevelUp?.Invoke(newLevel);
            }

            // Normalise spark intensity within the current level band.
            _state.sparkIntensity = ComputeSparkIntensity(_state.chargeTime, newLevel);
        }

        private void ReleaseDrift()
        {
            DriftLevel level  = _state.currentLevel;
            BoostConfig reward = GetBoostRewardForLevel(level);

            _state.Reset();

            if (reward != null && BoostController.Instance != null)
            {
                // Chain mini-turbo bonus: apply chain multiplier or extra stacks.
                if (enableMiniTurboChain)
                {
                    float timeSinceLast = Time.time - _lastReleaseTime;
                    if (driftConfig != null && timeSinceLast <= driftConfig.miniTurboChainWindow)
                        _chainCount++;
                    else
                        _chainCount = 0;
                }
                BoostController.Instance.ApplyBoost(reward);
            }

            _lastReleaseTime = Time.time;
            OnDriftRelease?.Invoke(level, reward);
        }

        private DriftLevel ComputeLevel(float chargeTime)
        {
            if (driftConfig == null || driftConfig.chargeThresholds == null
                || driftConfig.chargeThresholds.Length < 4)
                return DriftLevel.None;

            if (chargeTime >= driftConfig.chargeThresholds[3]) return DriftLevel.UltraPurple;
            if (chargeTime >= driftConfig.chargeThresholds[2]) return DriftLevel.Purple;
            if (chargeTime >= driftConfig.chargeThresholds[1]) return DriftLevel.Orange;
            if (chargeTime >= driftConfig.chargeThresholds[0]) return DriftLevel.Blue;
            return DriftLevel.None;
        }

        private float ComputeSparkIntensity(float chargeTime, DriftLevel level)
        {
            if (driftConfig == null) return 0f;
            float[] t = driftConfig.chargeThresholds;
            switch (level)
            {
                case DriftLevel.None:
                    return t[0] > 0f ? chargeTime / t[0] : 0f;
                case DriftLevel.Blue:
                    return Mathf.InverseLerp(t[0], t[1], chargeTime);
                case DriftLevel.Orange:
                    return Mathf.InverseLerp(t[1], t[2], chargeTime);
                case DriftLevel.Purple:
                    return Mathf.InverseLerp(t[2], t[3], chargeTime);
                default:
                    return 1f;
            }
        }

        private Color GetSparkColor(DriftLevel level)
        {
            if (driftConfig == null || driftConfig.sparkColors == null
                || driftConfig.sparkColors.Length < 4)
                return Color.blue;
            switch (level)
            {
                case DriftLevel.Blue:        return driftConfig.sparkColors[0];
                case DriftLevel.Orange:      return driftConfig.sparkColors[1];
                case DriftLevel.Purple:      return driftConfig.sparkColors[2];
                case DriftLevel.UltraPurple: return driftConfig.sparkColors[3];
                default:                     return driftConfig.sparkColors[0];
            }
        }

        private BoostConfig GetBoostRewardForLevel(DriftLevel level)
        {
            if (driftConfig == null || driftConfig.boostRewardPerLevel == null) return null;
            int idx = (int)level - 1; // DriftLevel.None = 0, Blue = 1 …
            if (idx < 0 || idx >= driftConfig.boostRewardPerLevel.Length) return null;
            return driftConfig.boostRewardPerLevel[idx];
        }

        #endregion

        #region Editor Gizmos

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!Application.isPlaying) return;
            GUILayout.BeginArea(new Rect(10, 270, 300, 100));
            GUILayout.Label($"[DriftController] Active:{_state.active} | Dir:{_state.direction}");
            GUILayout.Label($"  Level:{_state.currentLevel} | Charge:{_state.chargeTime:F2}s | Intensity:{_state.sparkIntensity:F2}");
            GUILayout.EndArea();
        }
#endif

        #endregion
    }
}
