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
    /// </summary>
    public class PlayerAvatar : MonoBehaviour
    {
        [Header("Interpolation")]
        [SerializeField] private float interpolationSpeed = 10f;

        [Header("References")]
        [SerializeField] private TextMesh nameLabel;       // nullable — optional 3D name
        [SerializeField] private MeshRenderer meshRenderer;

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

        /// <summary>
        /// Enables or disables the mesh renderer and name label so the avatar
        /// can be hidden without being destroyed (e.g. while loading).
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
