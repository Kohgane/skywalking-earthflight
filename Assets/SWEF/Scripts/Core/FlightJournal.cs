using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Core
{
    /// <summary>
    /// Records each flight session and persists the entries via <see cref="SaveManager"/>.
    /// Tracks start location, max altitude, duration, and distance travelled.
    /// Auto-starts recording on <c>Start()</c> and stops on pause or destroy.
    /// Newest entries appear first; the list is capped at <see cref="maxEntries"/>.
    /// </summary>
    public class FlightJournal : MonoBehaviour
    {
        // ── Config ───────────────────────────────────────────────────────────
        [Header("Config")]
        [Tooltip("Maximum number of journal entries to retain (oldest are pruned).")]
        [SerializeField] private int maxEntries = 100;

        // ── Internal state ───────────────────────────────────────────────────
        private SaveManager _save;
        private bool        _recording;
        private string      _activeId;

        private float  _sessionStartTime;
        private float  _maxAltitudeKm;
        private float  _distanceKm;
        private Vector3 _lastPosition;
        private bool   _hasLastPosition;

        private const float MetersToKilometers = 0.001f;

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Start()
        {
            _save = FindFirstObjectByType<SaveManager>();
            if (_save == null)
                Debug.LogWarning("[SWEF] FlightJournal: SaveManager not found — entries will not be persisted.");

            StartRecording();
        }

        private void Update()
        {
            if (!_recording) return;

            // Track altitude
            float altKm = (float)(SWEFSession.Alt / 1000.0);
            if (altKm > _maxAltitudeKm)
                _maxAltitudeKm = altKm;

            // Track distance using world-space position of this transform
            if (_hasLastPosition)
            {
                float delta = Vector3.Distance(transform.position, _lastPosition) * MetersToKilometers;
                _distanceKm += delta;
            }
            _lastPosition    = transform.position;
            _hasLastPosition = true;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _recording)
                StopRecording();
        }

        private void OnDestroy()
        {
            if (_recording)
                StopRecording();
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Begins a new flight-journal recording session.</summary>
        public void StartRecording()
        {
            if (_recording)
            {
                Debug.LogWarning("[SWEF] FlightJournal: already recording.");
                return;
            }

            _activeId        = Guid.NewGuid().ToString();
            _sessionStartTime = Time.unscaledTime;
            _maxAltitudeKm   = 0f;
            _distanceKm      = 0f;
            _hasLastPosition = false;
            _recording       = true;

            Debug.Log($"[SWEF] FlightJournal: recording started (id={_activeId}).");
        }

        /// <summary>Ends the current recording session and saves the entry.</summary>
        public void StopRecording()
        {
            if (!_recording) return;

            _recording = false;

            float duration = Time.unscaledTime - _sessionStartTime;

            var entry = new JournalEntry
            {
                id            = _activeId,
                startLocation = SWEFSession.HasFix
                    ? $"{SWEFSession.Lat:F4}, {SWEFSession.Lon:F4}"
                    : "Unknown",
                startLatitude  = SWEFSession.Lat,
                startLongitude = SWEFSession.Lon,
                maxAltitudeKm  = _maxAltitudeKm,
                durationSec    = duration,
                distanceKm     = _distanceKm,
                notes          = "",
                recordedAt     = DateTime.UtcNow.ToString("o")
            };

            if (_save != null)
            {
                // Newest first
                _save.Data.journal.Insert(0, entry);

                // Prune old entries
                if (_save.Data.journal.Count > maxEntries)
                    _save.Data.journal.RemoveRange(maxEntries, _save.Data.journal.Count - maxEntries);

                // Update cumulative stats
                _save.Data.totalFlights++;
                _save.Data.totalFlightTimeSec   += duration;
                _save.Data.totalDistanceKm      += _distanceKm;
                if (_maxAltitudeKm > _save.Data.allTimeMaxAltitudeKm)
                    _save.Data.allTimeMaxAltitudeKm = _maxAltitudeKm;

                _save.Save();
            }

            Debug.Log($"[SWEF] FlightJournal: recording stopped — duration={duration:F0}s, maxAlt={_maxAltitudeKm:F2}km, dist={_distanceKm:F2}km.");
        }

        /// <summary>Deletes the journal entry with the given <paramref name="id"/>.</summary>
        public void DeleteEntry(string id)
        {
            if (_save == null) return;
            int removed = _save.Data.journal.RemoveAll(e => e.id == id);
            if (removed > 0)
            {
                _save.Save();
                Debug.Log($"[SWEF] FlightJournal: deleted entry {id}.");
            }
        }

        /// <summary>Stores free-text notes for an existing journal entry.</summary>
        public void SetNotes(string id, string notes)
        {
            if (_save == null) return;
            var entry = _save.Data.journal.Find(e => e.id == id);
            if (entry != null)
            {
                entry.notes = notes ?? "";
                _save.Save();
            }
        }

        /// <summary>Returns all journal entries (newest first).</summary>
        public List<JournalEntry> GetAllEntries() =>
            _save != null ? _save.Data.journal : new List<JournalEntry>();
    }
}
