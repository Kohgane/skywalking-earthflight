// GhostReplayManager.cs — Phase 120: Precision Landing Challenge System
// Ghost system: download top player's approach, fly alongside as ghost overlay.
// Namespace: SWEF.LandingChallenge

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Manages ghost replay overlays.
    /// Loads a reference approach replay and drives a ghost aircraft transform
    /// in sync with the player's current approach for comparison.
    /// </summary>
    public class GhostReplayManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Ghost Visuals")]
        [SerializeField] private Transform ghostAircraftRoot;
        [SerializeField] private float     ghostAlpha = 0.4f;

        // ── State ─────────────────────────────────────────────────────────────

        private List<ReplayFrame>  _ghostFrames;
        private int                _frameIndex;
        private float              _timer;
        private bool               _isRunning;
        private float              _frameInterval = 0.1f;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Raised when the ghost replay finishes.</summary>
        public event Action OnGhostComplete;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Whether a ghost is currently playing.</summary>
        public bool IsRunning => _isRunning;

        /// <summary>Player name whose replay is loaded as ghost, or empty.</summary>
        public string GhostPlayerName { get; private set; }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Load a set of replay frames as the ghost approach.</summary>
        public void LoadGhost(List<ReplayFrame> frames, string playerName, float captureRateFPS = 10f)
        {
            _ghostFrames   = frames;
            GhostPlayerName = playerName;
            _frameInterval = 1f / Mathf.Max(1f, captureRateFPS);
        }

        /// <summary>Start playing the ghost overlay from the beginning.</summary>
        public void StartGhost()
        {
            if (_ghostFrames == null || _ghostFrames.Count == 0) return;
            _frameIndex = 0;
            _timer      = 0f;
            _isRunning  = true;
            if (ghostAircraftRoot != null) ghostAircraftRoot.gameObject.SetActive(true);
        }

        /// <summary>Stop the ghost overlay.</summary>
        public void StopGhost()
        {
            _isRunning = false;
            if (ghostAircraftRoot != null) ghostAircraftRoot.gameObject.SetActive(false);
        }

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Update()
        {
            if (!_isRunning || _ghostFrames == null) return;

            _timer += Time.deltaTime;
            if (_timer >= _frameInterval)
            {
                _timer = 0f;
                if (_frameIndex < _ghostFrames.Count)
                {
                    var frame = _ghostFrames[_frameIndex];
                    if (ghostAircraftRoot != null)
                    {
                        ghostAircraftRoot.position = frame.Position;
                        ghostAircraftRoot.rotation = frame.Rotation;
                    }
                    _frameIndex++;
                }
                else
                {
                    _isRunning = false;
                    if (ghostAircraftRoot != null) ghostAircraftRoot.gameObject.SetActive(false);
                    OnGhostComplete?.Invoke();
                }
            }
        }
    }
}
