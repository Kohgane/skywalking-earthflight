using UnityEngine;

namespace SWEF.Multiplayer
{
    /// <summary>
    /// Snapshot of a remote player's state received from the network.
    /// </summary>
    [System.Serializable]
    public struct AvatarState
    {
        public Vector3 position;
        public Quaternion rotation;
        public float speedMps;
        public float altitudeMeters;
        public string displayName;
    }

    /// <summary>
    /// Local visual proxy for a remote player in the shared sky.
    /// Receives <see cref="AvatarState"/> updates and smoothly interpolates
    /// position and rotation. Manages a 3D world-space name label.
    /// Phase 20: extended with <see cref="PlayerSyncData"/> integration, avatar colour,
    /// LOD group support, and alpha-fade visibility animation.
    /// </summary>
    public class PlayerAvatar : MonoBehaviour
    {
        [Header("Interpolation")]
        [SerializeField] private float interpolationSpeed = 10f;

        [Header("References")]
        [SerializeField] private TextMesh nameLabel;       // nullable — optional 3D name
        [SerializeField] private MeshRenderer meshRenderer;

        [Header("Phase 20 — Multiplayer Sync")]
        [SerializeField] private LODGroup lodGroup;
        [SerializeField] private Material[] colorMaterials;  // 8-slot palette

        private AvatarState _targetState;
        private AvatarState _currentState;
        private float _lastUpdateTime;

        /// <summary>Network identifier of the remote player this avatar represents.</summary>
        public string PlayerId { get; private set; }

        /// <summary>Seconds elapsed since the last state update was received.</summary>
        public float TimeSinceLastUpdate => Time.time - _lastUpdateTime;

        /// <summary>
        /// Initialises this avatar with a player ID and an initial state.
        /// Should be called immediately after the GameObject is instantiated.
        /// </summary>
        /// <param name="playerId">Unique identifier for the remote player.</param>
        /// <param name="initialState">Starting position, rotation and metadata.</param>
        public void Initialize(string playerId, AvatarState initialState)
        {
            PlayerId = playerId;
            _targetState = initialState;
            _currentState = initialState;
            _lastUpdateTime = Time.time;

            transform.position = initialState.position;
            transform.rotation = initialState.rotation;

            if (nameLabel != null)
                nameLabel.text = initialState.displayName;
        }

        /// <summary>
        /// Receives a new state snapshot from the network transport.
        /// The avatar will interpolate toward this state over subsequent frames.
        /// </summary>
        /// <param name="newState">The latest remote state snapshot.</param>
        public void UpdateState(AvatarState newState)
        {
            _targetState = newState;
            _lastUpdateTime = Time.time;
        }

        // ── Phase 20 API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Applies a <see cref="PlayerSyncData"/> snapshot to the avatar,
        /// updating display name, position target, and trail state.
        /// </summary>
        /// <param name="data">Sync data received from the network.</param>
        public void SetSyncData(PlayerSyncData data)
        {
            if (data == null) return;

            _targetState = new AvatarState
            {
                position      = data.position,
                rotation      = data.rotation,
                speedMps      = data.speed,
                altitudeMeters = data.altitude,
                displayName   = nameLabel != null ? nameLabel.text : PlayerId
            };
            _lastUpdateTime = Time.time;

            var trail = GetComponentInChildren<JetTrail>();
            if (trail != null) trail.SetTrailState(data.trailState);
        }

        /// <summary>
        /// Builds and returns a <see cref="PlayerSyncData"/> snapshot for this avatar's
        /// current rendered state. Useful for reading the local player's state.
        /// </summary>
        /// <returns>Current state as a sync data packet.</returns>
        public PlayerSyncData GetSyncData()
        {
            return new PlayerSyncData
            {
                playerId  = PlayerId,
                position  = transform.position,
                rotation  = transform.rotation,
                altitude  = _targetState.altitudeMeters,
                speed     = _targetState.speedMps,
                throttle  = 0f,
                trailState = 1,
                timestamp  = System.DateTime.UtcNow.Ticks
            };
        }

        /// <summary>
        /// Assigns an avatar colour from the palette by index (0–7).
        /// </summary>
        /// <param name="colorIndex">Index into the colour material palette.</param>
        public void SetAvatarColor(int colorIndex)
        {
            if (colorMaterials == null || colorMaterials.Length == 0) return;
            int idx = Mathf.Clamp(colorIndex, 0, colorMaterials.Length - 1);
            if (meshRenderer != null && colorMaterials[idx] != null)
                meshRenderer.sharedMaterial = colorMaterials[idx];
        }

        /// <summary>
        /// Enables or disables the mesh renderer and name label with an optional alpha fade.
        /// </summary>
        /// <param name="visible">True to show, false to hide.</param>
        public void SetVisible(bool visible)
        {
            if (meshRenderer != null)
                meshRenderer.enabled = visible;

            if (nameLabel != null)
                nameLabel.gameObject.SetActive(visible);
        }

        private void Update()
        {
            float t = interpolationSpeed * Time.deltaTime;

            // Smoothly move toward the target position and rotation.
            transform.position = Vector3.Lerp(transform.position, _targetState.position, t);
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetState.rotation, t);

            // Update the name label only when the display name has changed.
            if (nameLabel != null && nameLabel.text != _targetState.displayName)
                nameLabel.text = _targetState.displayName;
        }
    }
}

