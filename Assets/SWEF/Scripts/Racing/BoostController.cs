// BoostController.cs — SWEF Boost & Drift Mechanics System (Phase 62)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Racing
{
    /// <summary>
    /// Phase 62 — Singleton that manages all active boosts on the local player.
    ///
    /// <para>Maintains a priority-sorted queue of <see cref="BoostState"/> entries,
    /// blends them into a single <see cref="CurrentSpeedMultiplier"/> for consumption
    /// by <c>FlightController</c>, and fires events on lifecycle transitions.</para>
    ///
    /// <para>Attach to a persistent GameObject in the bootstrap scene.</para>
    /// </summary>
    public class BoostController : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static BoostController Instance { get; private set; }

        #endregion

        #region Constants

        private const int   DefaultMaxActiveBoosts    = 5;
        private const float BlendInDuration           = 0.15f;
        private const float BlendOutDuration          = 0.4f;
        private const float StackableMultiplierStep   = 0.05f;

        #endregion

        #region Inspector

        [Header("Limits")]
        [Tooltip("Maximum number of simultaneously active boosts (default 5).")]
        [SerializeField] private int maxActiveBoosts = DefaultMaxActiveBoosts;

        [Header("FOV Feedback")]
        [Tooltip("Camera whose FOV is pushed wider during boosts.")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("Base FOV when no boost is active.")]
        [SerializeField] private float baseFOV = 60f;

        [Tooltip("Maximum additional FOV added at peak boost multiplier.")]
        [SerializeField] private float maxFOVBoost = 15f;

        [Tooltip("Speed at which the FOV transitions (degrees per second).")]
        [SerializeField] private float fovLerpSpeed = 8f;

        [Header("Blend Curves")]
        [Tooltip("Ease-in curve for transitioning from 1.0 to the target multiplier.")]
        [SerializeField] private AnimationCurve blendInCurve
            = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Ease-out curve for transitioning from the target multiplier back to 1.0.")]
        [SerializeField] private AnimationCurve blendOutCurve
            = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        #endregion

        #region Events

        /// <summary>Fired when a new boost begins (passes the activating <see cref="BoostConfig"/>).</summary>
        public event Action<BoostConfig> OnBoostStart;

        /// <summary>Fired when a boost expires or is cancelled (passes the <see cref="BoostType"/>).</summary>
        public event Action<BoostType> OnBoostEnd;

        /// <summary>Fired when the stack count of a stackable boost changes.</summary>
        public event Action<BoostType, int> OnBoostStackChanged;

        #endregion

        #region Public Properties

        /// <summary>
        /// Combined speed multiplier from all active boosts.
        /// Read by <c>FlightController</c> every physics tick.
        /// Returns 1.0 when no boosts are active.
        /// </summary>
        public float CurrentSpeedMultiplier { get; private set; } = 1f;

        /// <summary>Number of currently active boost entries.</summary>
        public int ActiveBoostCount => _activeBoosts.Count;

        /// <summary>Read-only view of the active boost queue.</summary>
        public IReadOnlyList<BoostState> ActiveBoosts => _activeBoosts;

        #endregion

        #region Private State

        // Priority-sorted list (highest multiplier first).
        private readonly List<BoostState> _activeBoosts = new List<BoostState>(DefaultMaxActiveBoosts);

        private float _blendInTimer;
        private float _blendOutTimer;
        private bool  _wasBoosting;
        private float _targetMultiplier = 1f;
        private float _displayMultiplier = 1f;

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
            TickBoosts(Time.deltaTime);
            BlendMultiplier(Time.deltaTime);
            PushFOV(Time.deltaTime);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Applies a new boost to the local player.
        /// Stackable boosts increment an existing entry; non-stackable boosts replace
        /// lower-priority entries when the queue is full.
        /// </summary>
        /// <param name="config">Config describing the boost to apply.</param>
        public void ApplyBoost(BoostConfig config)
        {
            if (config == null) return;

            // Handle stackable boosts.
            if (config.stackable)
            {
                for (int i = 0; i < _activeBoosts.Count; i++)
                {
                    BoostState existing = _activeBoosts[i];
                    if (existing.config == config && existing.stacks < config.maxStacks)
                    {
                        existing.stacks++;
                        existing.remainingDuration = config.durationSeconds; // refresh duration
                        existing.multiplier        = config.speedMultiplier + (existing.stacks - 1) * StackableMultiplierStep;
                        _activeBoosts[i]           = existing;
                        OnBoostStackChanged?.Invoke(config.boostType, existing.stacks);
                        SortBoostQueue();
                        return;
                    }
                }
            }

            // Enforce queue cap — drop the lowest-priority entry if needed.
            if (_activeBoosts.Count >= maxActiveBoosts)
            {
                int lowestIdx = FindLowestPriorityIndex();
                if (config.priority <= _activeBoosts[lowestIdx].config.priority) return;
                BoostType removed = _activeBoosts[lowestIdx].type;
                _activeBoosts.RemoveAt(lowestIdx);
                OnBoostEnd?.Invoke(removed);
            }

            var newState = new BoostState(config);
            _activeBoosts.Add(newState);
            SortBoostQueue();

            _blendInTimer = 0f;
            OnBoostStart?.Invoke(config);
        }

        /// <summary>
        /// Immediately cancels all active boosts (e.g. on collision or stall).
        /// </summary>
        public void CancelAllBoosts()
        {
            foreach (var b in _activeBoosts)
                OnBoostEnd?.Invoke(b.type);
            _activeBoosts.Clear();
            _targetMultiplier = 1f;
        }

        /// <summary>
        /// Cancels all active boosts of a specific type.
        /// </summary>
        /// <param name="type">Type of boost to cancel.</param>
        public void CancelBoostsOfType(BoostType type)
        {
            for (int i = _activeBoosts.Count - 1; i >= 0; i--)
            {
                if (_activeBoosts[i].type == type)
                {
                    OnBoostEnd?.Invoke(type);
                    _activeBoosts.RemoveAt(i);
                }
            }
        }

        #endregion

        #region Private Helpers

        private void TickBoosts(float dt)
        {
            bool changed = false;
            for (int i = _activeBoosts.Count - 1; i >= 0; i--)
            {
                BoostState b = _activeBoosts[i];
                b.remainingDuration -= dt;
                if (b.remainingDuration <= 0f)
                {
                    _activeBoosts.RemoveAt(i);
                    OnBoostEnd?.Invoke(b.type);
                    changed = true;
                }
                else
                {
                    _activeBoosts[i] = b;
                }
            }
            if (changed) SortBoostQueue();

            // Recalculate target multiplier.
            _targetMultiplier = 1f;
            foreach (var b in _activeBoosts)
                _targetMultiplier = Mathf.Max(_targetMultiplier, b.multiplier);
        }

        private void BlendMultiplier(float dt)
        {
            bool isBoosting = _activeBoosts.Count > 0;

            if (isBoosting)
            {
                _blendInTimer = Mathf.Min(_blendInTimer + dt, BlendInDuration);
                float t = BlendInDuration > 0f ? _blendInTimer / BlendInDuration : 1f;
                float w = blendInCurve.Evaluate(t);
                _displayMultiplier = Mathf.Lerp(1f, _targetMultiplier, w);
                _blendOutTimer = 0f;
            }
            else
            {
                if (_wasBoosting)
                    _blendOutTimer = 0f;
                _blendOutTimer = Mathf.Min(_blendOutTimer + dt, BlendOutDuration);
                float t = BlendOutDuration > 0f ? _blendOutTimer / BlendOutDuration : 1f;
                float w = blendOutCurve.Evaluate(t);
                _displayMultiplier = Mathf.Lerp(_targetMultiplier, 1f, w);
                _blendInTimer = 0f;
            }

            CurrentSpeedMultiplier = _displayMultiplier;
            _wasBoosting = isBoosting;
        }

        private void PushFOV(float dt)
        {
            if (targetCamera == null) return;
            float boostFraction = Mathf.InverseLerp(1f, 3f, CurrentSpeedMultiplier);
            float desiredFOV    = baseFOV + maxFOVBoost * boostFraction;
            targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, desiredFOV, fovLerpSpeed * dt);
        }

        private void SortBoostQueue()
        {
            // Sort descending by multiplier so highest is first.
            _activeBoosts.Sort((a, b) => b.multiplier.CompareTo(a.multiplier));
        }

        private int FindLowestPriorityIndex()
        {
            int idx = 0;
            for (int i = 1; i < _activeBoosts.Count; i++)
            {
                if (_activeBoosts[i].config.priority < _activeBoosts[idx].config.priority)
                    idx = i;
            }
            return idx;
        }

        #endregion

        #region Editor Gizmos

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!Application.isPlaying) return;
            GUILayout.BeginArea(new Rect(10, 60, 300, 200));
            GUILayout.Label($"[BoostController] Active: {_activeBoosts.Count} | x{CurrentSpeedMultiplier:F2}");
            foreach (var b in _activeBoosts)
                GUILayout.Label($"  {b.type} | x{b.multiplier:F2} | {b.remainingDuration:F1}s | stacks:{b.stacks}");
            GUILayout.EndArea();
        }
#endif

        #endregion
    }
}
