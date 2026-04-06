// LiveryGallery.cs — Phase 115: Advanced Aircraft Livery Editor
// Community gallery: browse, rate, download shared liveries.
// Namespace: SWEF.LiveryEditor

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Community livery gallery.  Maintains an in-memory catalogue of
    /// shared liveries and provides browse, search, rating, and download helpers.
    /// Online marketplace integration is gated behind <c>SWEF_MARKETPLACE_AVAILABLE</c>.
    /// </summary>
    public class LiveryGallery : MonoBehaviour
    {
        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when a livery is downloaded from the gallery.</summary>
        public event Action<LiverySaveData> OnLiveryDownloaded;

        /// <summary>Raised when a livery's rating is updated.</summary>
        public event Action<string, float> OnRatingUpdated;

        // ── Internal catalogue ────────────────────────────────────────────────────
        private readonly List<LiverySaveData> _catalogue = new List<LiverySaveData>();

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns all liveries in the gallery.</summary>
        public IReadOnlyList<LiverySaveData> GetAll() => _catalogue.AsReadOnly();

        /// <summary>
        /// Searches the gallery by name, author, or tag.
        /// </summary>
        /// <param name="query">Search term (case-insensitive).</param>
        public IReadOnlyList<LiverySaveData> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return GetAll();
            string q = query.ToLowerInvariant();
            return _catalogue
                .Where(l =>
                    (l.Metadata.Name   ?? "").ToLowerInvariant().Contains(q) ||
                    (l.Metadata.Author ?? "").ToLowerInvariant().Contains(q) ||
                    l.Metadata.Tags.Any(t => t.ToLowerInvariant().Contains(q)))
                .ToList()
                .AsReadOnly();
        }

        /// <summary>Returns gallery liveries sorted by star rating (descending).</summary>
        public IReadOnlyList<LiverySaveData> GetTopRated() =>
            _catalogue.OrderByDescending(l => l.Metadata.Rating).ToList().AsReadOnly();

        /// <summary>Returns the most recently uploaded liveries.</summary>
        public IReadOnlyList<LiverySaveData> GetNewest() =>
            _catalogue.OrderByDescending(l => l.Metadata.CreatedAtUtc).ToList().AsReadOnly();

        /// <summary>
        /// Simulates downloading a livery from the gallery (fires the event).
        /// </summary>
        public void Download(string liveryId)
        {
            var livery = _catalogue.Find(l => l.Metadata.LiveryId == liveryId);
            if (livery == null) return;
            livery.Metadata.DownloadCount++;
            OnLiveryDownloaded?.Invoke(livery);
        }

        /// <summary>Rates a gallery livery.</summary>
        /// <param name="liveryId">Livery identifier.</param>
        /// <param name="stars">Star rating (0–5).</param>
        public void Rate(string liveryId, float stars)
        {
            var livery = _catalogue.Find(l => l.Metadata.LiveryId == liveryId);
            if (livery == null) return;
            livery.Metadata.Rating = Mathf.Clamp(stars, 0f, 5f);
            OnRatingUpdated?.Invoke(liveryId, livery.Metadata.Rating);
        }

        /// <summary>Adds a livery to the local gallery catalogue (for testing or offline mode).</summary>
        public void AddToGallery(LiverySaveData livery)
        {
            if (livery != null) _catalogue.Add(livery);
        }

        /// <summary>Total number of liveries in the gallery.</summary>
        public int Count => _catalogue.Count;

#if SWEF_MARKETPLACE_AVAILABLE
        // ── Marketplace integration ───────────────────────────────────────────────

        /// <summary>
        /// Submits the given livery to the online marketplace.
        /// Requires the <c>SWEF_MARKETPLACE_AVAILABLE</c> define.
        /// </summary>
        public void SubmitToMarketplace(LiverySaveData livery)
        {
            // TODO: connect to MarketplaceManager when SWEF_MARKETPLACE_AVAILABLE
            Debug.Log($"[SWEF] LiveryGallery: submitting '{livery?.Metadata.Name}' to marketplace.");
        }
#endif
    }
}
