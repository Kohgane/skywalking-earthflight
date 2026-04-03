// RateLimiter.cs — SWEF Anti-Cheat & Security Hardening (Phase 92)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Security
{
    /// <summary>
    /// Generic sliding-window rate limiter.
    ///
    /// <para>Each unique <c>actionKey</c> maintains an independent window.
    /// Call <see cref="IsAllowed"/> before processing any rate-sensitive action;
    /// it returns <c>false</c> when the caller has exceeded the quota.</para>
    ///
    /// <para>Repeat offenders are tracked; when a player exceeds the rate limit
    /// more than <see cref="OffenderThreshold"/> times their backoff multiplier
    /// doubles up to <see cref="MaxBackoffMultiplier"/>.</para>
    /// </summary>
    public class RateLimiter
    {
        // ── Constants ─────────────────────────────────────────────────────────

        /// <summary>Number of consecutive violations that trigger backoff escalation.</summary>
        public const int OffenderThreshold = 3;

        /// <summary>Maximum backoff multiplier for repeat offenders.</summary>
        public const float MaxBackoffMultiplier = 8f;

        // ── Private state ─────────────────────────────────────────────────────

        // Maps actionKey → ordered list of timestamps (each event within the window)
        private readonly Dictionary<string, Queue<float>> _windows =
            new Dictionary<string, Queue<float>>();

        // Maps actionKey → consecutive violation count
        private readonly Dictionary<string, int> _violationCounts =
            new Dictionary<string, int>();

        // Maps actionKey → backoff multiplier
        private readonly Dictionary<string, float> _backoffMultipliers =
            new Dictionary<string, float>();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Checks whether the specified <paramref name="actionKey"/> is allowed
        /// given the configured quota.
        /// </summary>
        /// <param name="actionKey">Unique key identifying the player + action type, e.g. <c>"player123_chat"</c>.</param>
        /// <param name="maxPerWindow">Maximum number of allowed events per window.</param>
        /// <param name="windowSeconds">Sliding window duration in seconds.</param>
        /// <returns><c>true</c> if the action is within the allowed rate; <c>false</c> if rate-limited.</returns>
        public bool IsAllowed(string actionKey, int maxPerWindow, float windowSeconds)
        {
            if (string.IsNullOrEmpty(actionKey)) return true;

            float now = Time.realtimeSinceStartup;

            if (!_windows.TryGetValue(actionKey, out var window))
            {
                window = new Queue<float>();
                _windows[actionKey] = window;
            }

            // Apply backoff: extend the effective window for repeat offenders
            float effectiveWindow = windowSeconds * GetBackoffMultiplier(actionKey);

            // Evict events outside the window
            while (window.Count > 0 && now - window.Peek() > effectiveWindow)
                window.Dequeue();

            if (window.Count >= maxPerWindow)
            {
                // Rate limit exceeded — increment violation counter
                _violationCounts.TryGetValue(actionKey, out int count);
                count++;
                _violationCounts[actionKey] = count;

                if (count >= OffenderThreshold)
                    EscalateBackoff(actionKey);

                return false;
            }

            window.Enqueue(now);

            // Reset violation streak on successful request
            if (_violationCounts.ContainsKey(actionKey))
                _violationCounts[actionKey] = 0;

            return true;
        }

        /// <summary>
        /// Returns the number of additional requests permitted for
        /// <paramref name="actionKey"/> in the current window.
        /// </summary>
        /// <param name="actionKey">Action key to query.</param>
        /// <param name="maxPerWindow">Configured quota.</param>
        /// <param name="windowSeconds">Window duration in seconds.</param>
        /// <returns>Remaining quota (≥ 0).</returns>
        public int GetRemainingQuota(string actionKey, int maxPerWindow, float windowSeconds)
        {
            if (!_windows.TryGetValue(actionKey, out var window))
                return maxPerWindow;

            float now             = Time.realtimeSinceStartup;
            float effectiveWindow = windowSeconds * GetBackoffMultiplier(actionKey);

            int active = 0;
            foreach (float t in window)
                if (now - t <= effectiveWindow) active++;

            return Mathf.Max(0, maxPerWindow - active);
        }

        /// <summary>Clears all rate-limit state for a given <paramref name="actionKey"/>.</summary>
        /// <param name="actionKey">Key to reset.</param>
        public void Reset(string actionKey)
        {
            _windows.Remove(actionKey);
            _violationCounts.Remove(actionKey);
            _backoffMultipliers.Remove(actionKey);
        }

        /// <summary>Clears all rate-limit state for all keys.</summary>
        public void ResetAll()
        {
            _windows.Clear();
            _violationCounts.Clear();
            _backoffMultipliers.Clear();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private float GetBackoffMultiplier(string actionKey)
        {
            _backoffMultipliers.TryGetValue(actionKey, out float m);
            return Mathf.Max(1f, m);
        }

        private void EscalateBackoff(string actionKey)
        {
            float current = GetBackoffMultiplier(actionKey);
            _backoffMultipliers[actionKey] = Mathf.Min(current * 2f, MaxBackoffMultiplier);
        }
    }
}
