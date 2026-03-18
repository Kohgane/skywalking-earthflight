using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Multiplayer
{
    /// <summary>
    /// Manages the visual representation of remote players in the scene.
    /// Handles distance-based LOD, name labels, jet-trail sync, player colour
    /// assignment, enter/exit fade animations, and renderer pooling.
    /// </summary>
    public class RemotePlayerRenderer : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────────
        [Header("Prefab & Materials")]
        [Tooltip("Default avatar mesh prefab for remote players.")]
        [SerializeField] private GameObject playerPrefab;

        [Tooltip("8 distinct player colour materials (indices 0–7).")]
        [SerializeField] private Material[] playerMaterials = new Material[8];

        [Header("Name Label")]
        [Tooltip("Whether to show speed and altitude beneath the player name.")]
        [SerializeField] private bool showAltSpeedInfo = true;

        [Header("LOD Thresholds (metres)")]
        [SerializeField] private float lodFullDetail  = 500f;
        [SerializeField] private float lodSimplified  = 2000f;
        [SerializeField] private float lodPoint       = 10000f;

        [Header("Pool")]
        [SerializeField] private int maxPlayers = 8;

        // ── State ────────────────────────────────────────────────────────────────
        private Camera _mainCamera;
        private readonly Dictionary<string, RemotePlayerEntry> _active
            = new Dictionary<string, RemotePlayerEntry>();
        private readonly Stack<RemotePlayerEntry> _pool = new Stack<RemotePlayerEntry>();

        // ── Inner Types ───────────────────────────────────────────────────────────

        private class RemotePlayerEntry
        {
            public GameObject root;
            public MeshRenderer meshRenderer;
            public MeshRenderer lodPointRenderer;  // simplified dot
            public CanvasGroup   canvasGroup;       // for fade animation
            public TextMesh      nameLabel;
            public JetTrail      jetTrail;
            public string        playerId;
        }

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            _mainCamera = Camera.main;
            PrewarmPool();
        }

        private void LateUpdate()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;

            foreach (var kvp in _active)
                UpdateLOD(kvp.Value);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates (or spawns) the visual for the remote player described by
        /// <paramref name="data"/>, applying position, rotation, trail state, and name.
        /// </summary>
        /// <param name="info">Player metadata (name, colour index).</param>
        /// <param name="data">Latest sync snapshot.</param>
        public void UpdatePlayer(PlayerInfo info, PlayerSyncData data)
        {
            if (data == null || info == null) return;

            if (!_active.TryGetValue(info.playerId, out RemotePlayerEntry entry))
            {
                entry = SpawnEntry(info);
                if (entry == null) return;
                _active[info.playerId] = entry;
            }

            // Apply transform
            entry.root.transform.position = data.position;
            entry.root.transform.rotation = data.rotation;

            // Name label
            if (entry.nameLabel != null)
            {
                string label = info.playerName;
                if (showAltSpeedInfo)
                    label += $"\n{data.altitude:F0}m  {data.speed:F0}m/s";
                entry.nameLabel.text = label;

                // Billboard toward camera
                if (_mainCamera != null)
                    entry.nameLabel.transform.LookAt(
                        entry.nameLabel.transform.position + _mainCamera.transform.rotation * Vector3.forward,
                        _mainCamera.transform.rotation * Vector3.up);
            }

            // Jet trail
            if (entry.jetTrail != null)
                entry.jetTrail.SetTrailState(data.trailState);
        }

        /// <summary>
        /// Fades out and returns the entry for the given player to the pool.
        /// </summary>
        /// <param name="playerId">Player identifier to remove.</param>
        public void RemovePlayer(string playerId)
        {
            if (!_active.TryGetValue(playerId, out RemotePlayerEntry entry)) return;

            _active.Remove(playerId);
            StartCoroutine(FadeAndReturn(entry, 0f, 1f));
        }

        /// <summary>Removes all active remote player renderers and returns them to the pool.</summary>
        public void RemoveAllPlayers()
        {
            var ids = new List<string>(_active.Keys);
            foreach (string id in ids)
                RemovePlayer(id);
        }

        // ── LOD ──────────────────────────────────────────────────────────────────

        private void UpdateLOD(RemotePlayerEntry entry)
        {
            if (_mainCamera == null) return;

            float dist = Vector3.Distance(_mainCamera.transform.position, entry.root.transform.position);

            bool showFull    = dist < lodFullDetail;
            bool showSimple  = dist >= lodFullDetail  && dist < lodSimplified;
            bool showPoint   = dist >= lodSimplified  && dist < lodPoint;
            bool hidden      = dist >= lodPoint;

            entry.root.SetActive(!hidden);

            if (entry.meshRenderer    != null) entry.meshRenderer.enabled    = showFull || showSimple;
            if (entry.lodPointRenderer != null) entry.lodPointRenderer.enabled = showPoint;
            if (entry.nameLabel        != null) entry.nameLabel.gameObject.SetActive(showFull || showSimple);
            if (entry.jetTrail         != null) entry.jetTrail.enabled = showFull;

            // Fade name label alpha with distance in full-detail range
            if (entry.nameLabel != null && showFull)
            {
                float alpha = 1f - Mathf.Clamp01((dist - lodFullDetail * 0.5f) / (lodFullDetail * 0.5f));
                var c = entry.nameLabel.color;
                entry.nameLabel.color = new Color(c.r, c.g, c.b, alpha);
            }
        }

        // ── Pool Management ───────────────────────────────────────────────────────

        private void PrewarmPool()
        {
            if (playerPrefab == null) return;

            for (int i = 0; i < maxPlayers; i++)
            {
                var entry = CreateEntry();
                entry.root.SetActive(false);
                _pool.Push(entry);
            }
        }

        private RemotePlayerEntry SpawnEntry(PlayerInfo info)
        {
            RemotePlayerEntry entry;
            if (_pool.Count > 0)
            {
                entry = _pool.Pop();
            }
            else
            {
                if (_active.Count >= maxPlayers)
                {
                    Debug.LogWarning("[SWEF][RemotePlayerRenderer] Max remote player limit reached.");
                    return null;
                }
                entry = CreateEntry();
            }

            entry.playerId = info.playerId;
            entry.root.SetActive(true);

            // Assign material colour
            int colorIdx = Mathf.Clamp(info.avatarIndex, 0, playerMaterials.Length - 1);
            if (entry.meshRenderer != null && colorIdx < playerMaterials.Length
                                           && playerMaterials[colorIdx] != null)
                entry.meshRenderer.sharedMaterial = playerMaterials[colorIdx];

            // Fade in
            StartCoroutine(FadeAndReturn(entry, 1f, 0f));

            return entry;
        }

        private RemotePlayerEntry CreateEntry()
        {
            if (playerPrefab == null)
            {
                // Fallback: create a minimal capsule
                var fallback = new GameObject("RemotePlayer_Fallback");
                fallback.transform.SetParent(transform);
                return new RemotePlayerEntry
                {
                    root = fallback,
                    playerId = ""
                };
            }

            var go = Instantiate(playerPrefab, transform);
            var entry = new RemotePlayerEntry
            {
                root            = go,
                meshRenderer    = go.GetComponentInChildren<MeshRenderer>(),
                jetTrail        = go.GetComponentInChildren<JetTrail>(),
                nameLabel       = go.GetComponentInChildren<TextMesh>(),
                canvasGroup     = go.GetComponentInChildren<CanvasGroup>(),
                playerId        = ""
            };

            return entry;
        }

        private IEnumerator FadeAndReturn(RemotePlayerEntry entry, float targetAlpha, float startAlpha)
        {
            const float fadeDuration = 1f;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);

                if (entry.meshRenderer != null)
                {
                    Color c = entry.meshRenderer.material.color;
                    entry.meshRenderer.material.color = new Color(c.r, c.g, c.b, alpha);
                }

                yield return null;
            }

            if (targetAlpha <= 0f)
            {
                entry.root.SetActive(false);
                _pool.Push(entry);
            }
        }
    }
}
