// CommentatorController.cs — SWEF Phase 107: Live Streaming & Spectator Mode
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Spectator
{
    /// <summary>
    /// Phase 107 — Provides commentator/caster-specific tooling on top of the
    /// core spectator system.
    ///
    /// <para>Features:</para>
    /// <list type="bullet">
    ///   <item>Quick-switch camera presets (up to 9 bookmarked positions).</item>
    ///   <item>Picture-in-picture inset support (secondary camera tracking).</item>
    ///   <item>Highlight replay trigger — marks moments for instant replay.</item>
    ///   <item>Participant overlay showing pilot names, speeds, and positions.</item>
    ///   <item>Event marker API for key flight moments.</item>
    /// </list>
    /// </summary>
    public sealed class CommentatorController : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static CommentatorController Instance { get; private set; }

        // ── Inspector ──────────────────────────────────────────────────────────
        [SerializeField] private Camera pipCamera;
        [Tooltip("Maximum number of camera position presets.")]
        [SerializeField] private int maxPresets = 9;

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Raised when a highlight moment is marked. Argument is the timestamp (game time).</summary>
        public event Action<float> OnHighlightMarked;

        /// <summary>Raised when a flight event marker is added.</summary>
        public event Action<FlightEventMarker> OnEventMarked;

        // ── Public state ───────────────────────────────────────────────────────

        /// <summary>Returns <c>true</c> when picture-in-picture is active.</summary>
        public bool IsPiPActive { get; private set; }

        /// <summary>Registered camera position presets indexed 0–8.</summary>
        public IReadOnlyList<CameraPreset> Presets => _presets.AsReadOnly();

        // ── Internal state ─────────────────────────────────────────────────────
        private readonly List<CameraPreset> _presets = new List<CameraPreset>();
        private readonly List<FlightEventMarker> _markers = new List<FlightEventMarker>();
        private readonly List<float> _highlightTimestamps = new List<float>();

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Camera presets ─────────────────────────────────────────────────────

        /// <summary>
        /// Saves the current camera transform as preset <paramref name="slot"/> (0-based).
        /// </summary>
        public void SavePreset(int slot, Transform cam)
        {
            if (slot < 0 || slot >= maxPresets || cam == null) return;

            var preset = new CameraPreset
            {
                slot     = slot,
                position = cam.position,
                rotation = cam.rotation,
            };

            while (_presets.Count <= slot) _presets.Add(null);
            _presets[slot] = preset;
            Debug.Log($"[CommentatorController] Preset {slot} saved at {cam.position}.");
        }

        /// <summary>
        /// Moves <paramref name="cam"/> to the saved preset at <paramref name="slot"/>.
        /// Returns <c>false</c> if the slot is empty.
        /// </summary>
        public bool RecallPreset(int slot, Transform cam)
        {
            if (slot < 0 || slot >= _presets.Count || _presets[slot] == null || cam == null)
                return false;

            cam.position = _presets[slot].position;
            cam.rotation = _presets[slot].rotation;
            return true;
        }

        // ── Picture-in-picture ─────────────────────────────────────────────────

        /// <summary>
        /// Enables picture-in-picture mode, rendering <paramref name="insetTarget"/>
        /// in the PiP inset camera.
        /// </summary>
        public void EnablePiP(Transform insetTarget)
        {
            if (pipCamera == null) return;
            pipCamera.gameObject.SetActive(true);
            IsPiPActive = true;

            if (insetTarget != null)
                pipCamera.transform.SetParent(insetTarget, false);

            Debug.Log("[CommentatorController] PiP enabled.");
        }

        /// <summary>Disables picture-in-picture mode.</summary>
        public void DisablePiP()
        {
            if (pipCamera != null)
            {
                pipCamera.transform.SetParent(null, false);
                pipCamera.gameObject.SetActive(false);
            }
            IsPiPActive = false;
            Debug.Log("[CommentatorController] PiP disabled.");
        }

        // ── Highlight replay ───────────────────────────────────────────────────

        /// <summary>Marks the current game time as a highlight moment for instant replay.</summary>
        public void MarkHighlight()
        {
            float t = Time.time;
            _highlightTimestamps.Add(t);
            OnHighlightMarked?.Invoke(t);
            Debug.Log($"[CommentatorController] Highlight marked at t={t:F2}s.");
        }

        /// <summary>Returns all highlight timestamps recorded this session.</summary>
        public IReadOnlyList<float> GetHighlights() => _highlightTimestamps.AsReadOnly();

        // ── Event markers ──────────────────────────────────────────────────────

        /// <summary>
        /// Adds an event marker for a <paramref name="eventType"/> associated with an aircraft.
        /// </summary>
        public void AddEventMarker(FlightEventType eventType, string aircraftId, string description = "")
        {
            var marker = new FlightEventMarker
            {
                eventType   = eventType,
                aircraftId  = aircraftId,
                description = description,
                timestamp   = Time.time,
            };
            _markers.Add(marker);
            OnEventMarked?.Invoke(marker);
        }

        /// <summary>Returns all flight event markers recorded this session.</summary>
        public IReadOnlyList<FlightEventMarker> GetEventMarkers() => _markers.AsReadOnly();
    }

    // ── Data types ─────────────────────────────────────────────────────────────

    /// <summary>A saved spectator camera position/rotation preset.</summary>
    [Serializable]
    public sealed class CameraPreset
    {
        /// <summary>Slot index (0-based).</summary>
        public int slot;
        /// <summary>World-space position.</summary>
        public Vector3 position;
        /// <summary>World-space rotation.</summary>
        public Quaternion rotation;
    }

    /// <summary>Records a notable in-flight event for overlay display and replay.</summary>
    [Serializable]
    public sealed class FlightEventMarker
    {
        /// <summary>Type of the flight event.</summary>
        public FlightEventType eventType;
        /// <summary>ID of the aircraft involved.</summary>
        public string aircraftId;
        /// <summary>Human-readable description shown in the commentator overlay.</summary>
        public string description;
        /// <summary>Game time (seconds) when the event occurred.</summary>
        public float timestamp;
    }
}
