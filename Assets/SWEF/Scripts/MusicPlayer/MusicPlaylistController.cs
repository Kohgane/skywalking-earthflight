using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.MusicPlayer
{
    /// <summary>
    /// Pure C# class — no MonoBehaviour dependency.
    /// Manages playlist ordering, shuffle algorithms, repeat logic, and a manual queue.
    /// <para>
    /// Shuffle modes:
    /// <list type="bullet">
    ///   <item><see cref="ShuffleMode.Off"/> — sequential playback.</item>
    ///   <item><see cref="ShuffleMode.On"/>  — Fisher-Yates randomisation.</item>
    ///   <item><see cref="ShuffleMode.Smart"/> — energy-sorted to match current flight intensity.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class MusicPlaylistController
    {
        // ── Constants ─────────────────────────────────────────────────────────────
        private const int MaxHistory = 50;

        // ── State ─────────────────────────────────────────────────────────────────
        private readonly List<string>   _history       = new List<string>();
        private readonly Queue<string>  _manualQueue   = new Queue<string>();
        private          List<string>   _shuffledOrder = new List<string>();
        private          int            _historyIndex  = -1; // cursor for back-navigation
        private          int            _shuffleIndex  = 0;

        /// <summary>Current flight-energy level (0–1); used by Smart shuffle.</summary>
        public float CurrentFlightEnergy { get; set; } = 0.5f;

        /// <summary>Active shuffle mode.</summary>
        public ShuffleMode CurrentShuffleMode { get; set; } = ShuffleMode.Off;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the track ID that should play after the current one.
        /// Drains the manual queue first, then follows shuffle/repeat rules.
        /// </summary>
        /// <param name="state">Current player state.</param>
        /// <param name="playlist">Active playlist.</param>
        /// <param name="tracks">Hydrated <see cref="MusicTrack"/> list matching <paramref name="playlist"/>.</param>
        /// <returns>Next track ID, or empty string if none.</returns>
        public string GetNextTrack(MusicPlayerState state, MusicPlaylist playlist, List<MusicTrack> tracks)
        {
            if (playlist == null || tracks == null || tracks.Count == 0)
                return string.Empty;

            // Manual queue takes priority
            if (_manualQueue.Count > 0)
            {
                string queued = _manualQueue.Dequeue();
                PushHistory(queued);
                return queued;
            }

            string nextId;
            switch (state.shuffleMode)
            {
                case ShuffleMode.On:
                    nextId = GetNextShuffle(playlist, tracks);
                    break;
                case ShuffleMode.Smart:
                    nextId = GetNextSmartShuffle(playlist, tracks);
                    break;
                default:
                    nextId = GetNextSequential(state.currentTrackId, playlist, state.repeatMode);
                    break;
            }

            if (!string.IsNullOrEmpty(nextId))
                PushHistory(nextId);

            return nextId;
        }

        /// <summary>
        /// Returns the previous track ID via the history stack.
        /// Falls back to the previous sequential track when history is empty.
        /// </summary>
        public string GetPreviousTrack(MusicPlayerState state, MusicPlaylist playlist, List<MusicTrack> tracks)
        {
            if (playlist == null || tracks == null || tracks.Count == 0)
                return string.Empty;

            // Pop from history if available (skip current track entry)
            if (_history.Count > 1)
            {
                // _history.Last() is the current track; go one further back
                int targetIndex = _history.Count - 2;
                string prev = _history[targetIndex];
                _history.RemoveAt(_history.Count - 1); // remove current
                return prev;
            }

            return GetPreviousSequential(state.currentTrackId, playlist);
        }

        /// <summary>Adds a track to the front-of-line manual queue.</summary>
        public void EnqueueTrack(string trackId)
        {
            if (!string.IsNullOrEmpty(trackId))
                _manualQueue.Enqueue(trackId);
        }

        /// <summary>
        /// Removes and returns the next track from the manual queue.
        /// Returns empty string when the queue is empty.
        /// </summary>
        public string DequeueNext()
        {
            return _manualQueue.Count > 0 ? _manualQueue.Dequeue() : string.Empty;
        }

        /// <summary>
        /// Builds a smart "Up Next" list of track IDs sorted by energy similarity
        /// to <paramref name="referenceEnergy"/> and by mood similarity.
        /// </summary>
        /// <param name="playlist">Source playlist.</param>
        /// <param name="tracks">Hydrated track list.</param>
        /// <param name="referenceEnergy">Target energy (0–1) to match against.</param>
        /// <param name="count">Maximum number of results to return.</param>
        /// <returns>Ordered list of recommended track IDs.</returns>
        public List<string> GetSmartQueue(MusicPlaylist playlist, List<MusicTrack> tracks,
                                          float referenceEnergy, int count = 5)
        {
            var sorted = new List<MusicTrack>(tracks);
            sorted.Sort((a, b) =>
                Mathf.Abs(a.energy - referenceEnergy)
                    .CompareTo(Mathf.Abs(b.energy - referenceEnergy)));

            var result = new List<string>();
            foreach (MusicTrack t in sorted)
            {
                if (result.Count >= count) break;
                result.Add(t.trackId);
            }
            return result;
        }

        /// <summary>Resets shuffle order so the next shuffle call re-randomises.</summary>
        public void Reset()
        {
            _shuffledOrder.Clear();
            _shuffleIndex = 0;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private string GetNextSequential(string currentTrackId, MusicPlaylist playlist, RepeatMode repeat)
        {
            int idx = playlist.trackIds.IndexOf(currentTrackId);
            if (idx < 0) return playlist.trackIds.Count > 0 ? playlist.trackIds[0] : string.Empty;

            int next = idx + 1;
            if (next >= playlist.trackIds.Count)
            {
                if (repeat == RepeatMode.All) return playlist.trackIds[0];
                return string.Empty; // playlist ended
            }
            return playlist.trackIds[next];
        }

        private string GetPreviousSequential(string currentTrackId, MusicPlaylist playlist)
        {
            int idx = playlist.trackIds.IndexOf(currentTrackId);
            if (idx <= 0) return playlist.trackIds.Count > 0 ? playlist.trackIds[0] : string.Empty;
            return playlist.trackIds[idx - 1];
        }

        private string GetNextShuffle(MusicPlaylist playlist, List<MusicTrack> tracks)
        {
            // Rebuild shuffled order when exhausted
            if (_shuffledOrder.Count == 0 || _shuffleIndex >= _shuffledOrder.Count)
                RebuildShuffledOrder(playlist);

            if (_shuffledOrder.Count == 0) return string.Empty;

            string id = _shuffledOrder[_shuffleIndex];
            _shuffleIndex++;
            return id;
        }

        private string GetNextSmartShuffle(MusicPlaylist playlist, List<MusicTrack> tracks)
        {
            if (tracks.Count == 0) return string.Empty;

            // Sort by energy proximity to current flight energy
            var sorted = new List<MusicTrack>(tracks);
            float target = CurrentFlightEnergy;
            sorted.Sort((a, b) =>
                Mathf.Abs(a.energy - target).CompareTo(Mathf.Abs(b.energy - target)));

            // Return the closest match not already at the top of history
            string currentId = _history.Count > 0 ? _history[_history.Count - 1] : string.Empty;
            foreach (MusicTrack t in sorted)
                if (t.trackId != currentId) return t.trackId;

            return sorted.Count > 0 ? sorted[0].trackId : string.Empty;
        }

        /// <summary>Fisher-Yates shuffle on a copy of the playlist track IDs.</summary>
        private void RebuildShuffledOrder(MusicPlaylist playlist)
        {
            _shuffledOrder = new List<string>(playlist.trackIds);
            _shuffleIndex  = 0;

            // Fisher-Yates in-place shuffle
            for (int i = _shuffledOrder.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                string tmp           = _shuffledOrder[i];
                _shuffledOrder[i]    = _shuffledOrder[j];
                _shuffledOrder[j]    = tmp;
            }
        }

        private void PushHistory(string trackId)
        {
            _history.Add(trackId);
            if (_history.Count > MaxHistory)
                _history.RemoveAt(0);
        }
    }
}
