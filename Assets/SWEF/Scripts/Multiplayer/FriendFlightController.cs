using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Multiplayer
{
    /// <summary>
    /// MonoBehaviour that handles the "flying together" experience for friends.
    /// Detects formation flight, awards bonus XP, renders friend HUD markers,
    /// and provides "Follow Me" autopilot mode.
    /// </summary>
    public class FriendFlightController : MonoBehaviour
    {
        #region Constants
        private const float FormationRadiusMetres = 500f;
        private const float FormationCheckInterval = 3f;
        private const int FormationXpPerInterval = 10;
        #endregion

        #region Events
        /// <summary>Fired when two or more friends are within formation radius.</summary>
        public event Action<List<string>> OnFormationFormed;
        /// <summary>Fired when the formation breaks (friends move too far apart).</summary>
        public event Action OnFormationBroken;
        /// <summary>Fired when "Follow Me" mode is activated targeting a friend.</summary>
        public event Action<string> OnFollowStarted;
        #endregion

        #region Inspector
        [Header("Formation Settings")]
        [SerializeField, Tooltip("Distance in metres within which players are considered in formation.")]
        private float formationRadius = FormationRadiusMetres;

        [SerializeField, Tooltip("How often (seconds) the formation proximity check runs.")]
        private float formationCheckInterval = FormationCheckInterval;

        [SerializeField, Tooltip("XP awarded per formation check interval to each member.")]
        private int formationXpPerInterval = FormationXpPerInterval;

        [Header("HUD Markers")]
        [SerializeField, Tooltip("Prefab used to display a friend's position on the HUD.")]
        private GameObject friendHudMarkerPrefab;

        [Header("Follow Me")]
        [SerializeField, Tooltip("Speed at which the autopilot follows the target friend (m/s).")]
        private float followSpeed = 80f;
        #endregion

        #region Public Properties
        /// <summary>Whether the local player is currently in a formation.</summary>
        public bool IsInFormation { get; private set; }

        /// <summary>Player ID currently being followed in "Follow Me" mode, or null.</summary>
        public string FollowTargetId { get; private set; }
        #endregion

        #region Private State
        private readonly List<string> _currentFormationMembers = new List<string>();
        private readonly Dictionary<string, GameObject> _hudMarkers = new Dictionary<string, GameObject>();
        private Coroutine _formationCheckCoroutine;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            _formationCheckCoroutine = StartCoroutine(FormationCheckLoop());
        }

        private void OnDisable()
        {
            if (_formationCheckCoroutine != null)
                StopCoroutine(_formationCheckCoroutine);
            ClearHudMarkers();
        }
        #endregion

        #region Formation Detection
        private IEnumerator FormationCheckLoop()
        {
            var wait = new WaitForSeconds(formationCheckInterval);
            while (true)
            {
                CheckFormation();
                yield return wait;
            }
        }

        private void CheckFormation()
        {
            if (PlayerProfileManager.Instance == null) return;
            var localProfile = PlayerProfileManager.Instance.GetLocalProfile();
            if (localProfile == null) return;

            if (FriendSystemController.Instance == null) return;
            var onlineFriends = FriendSystemController.Instance.GetOnlineFriends();

            var inRange = new List<string>();
            foreach (var friend in onlineFriends)
            {
                if (friend.profile == null) continue;
                double dist = ApproxDistanceMetres(
                    localProfile.currentLatitude, localProfile.currentLongitude,
                    friend.profile.currentLatitude, friend.profile.currentLongitude);
                if (dist <= formationRadius)
                    inRange.Add(friend.friendId);
            }

            bool wasInFormation = IsInFormation;
            IsInFormation = inRange.Count > 0;

            if (IsInFormation)
            {
                // Award XP for each friend in formation
                foreach (string friendId in inRange)
                    AwardFormationXP(friendId);

                if (!wasInFormation)
                {
                    _currentFormationMembers.Clear();
                    _currentFormationMembers.AddRange(inRange);
                    MultiplayerAnalytics.RecordFormationFormed(inRange.Count);
                    MultiplayerBridge.OnFormationFormed(_currentFormationMembers);
                    OnFormationFormed?.Invoke(new List<string>(inRange));
                }
            }
            else if (wasInFormation)
            {
                _currentFormationMembers.Clear();
                MultiplayerBridge.OnFormationBroken();
                OnFormationBroken?.Invoke();
            }

            UpdateHudMarkers(onlineFriends);
        }

        private void AwardFormationXP(string friendId)
        {
#if SWEF_PROGRESSION_AVAILABLE
            if (SWEF.Progression.ProgressionManager.Instance != null)
                SWEF.Progression.ProgressionManager.Instance.AddXP(formationXpPerInterval);
#endif
        }
        #endregion

        #region HUD Markers
        private void UpdateHudMarkers(List<FriendData> friends)
        {
            if (friendHudMarkerPrefab == null) return;

            var activeFriendIds = new HashSet<string>();
            foreach (var friend in friends)
            {
                if (friend.profile == null) continue;
                activeFriendIds.Add(friend.friendId);

                if (!_hudMarkers.ContainsKey(friend.friendId))
                {
                    var marker = Instantiate(friendHudMarkerPrefab, transform);
                    _hudMarkers[friend.friendId] = marker;
                }
                // Position update would be driven by the actual world-space conversion
                // in a live network stack; here we note the marker exists.
            }

            // Remove markers for friends no longer online
            var toRemove = new List<string>();
            foreach (var kv in _hudMarkers)
                if (!activeFriendIds.Contains(kv.Key))
                    toRemove.Add(kv.Key);
            foreach (var id in toRemove)
            {
                if (_hudMarkers[id] != null)
                    Destroy(_hudMarkers[id]);
                _hudMarkers.Remove(id);
            }
        }

        private void ClearHudMarkers()
        {
            foreach (var kv in _hudMarkers)
                if (kv.Value != null)
                    Destroy(kv.Value);
            _hudMarkers.Clear();
        }
        #endregion

        #region Follow Me Mode
        /// <summary>
        /// Activates "Follow Me" mode, directing the autopilot to trail a specific friend.
        /// </summary>
        /// <param name="friendId">The friend's player ID to follow.</param>
        public void StartFollowMode(string friendId)
        {
            if (string.IsNullOrEmpty(friendId))
            {
                Debug.LogWarning("[SWEF] Multiplayer: StartFollowMode called with null/empty friendId.");
                return;
            }
            if (!FriendSystemController.Instance.IsFriend(friendId))
            {
                Debug.LogWarning($"[SWEF] Multiplayer: StartFollowMode — {friendId} is not a friend.");
                return;
            }

            FollowTargetId = friendId;
            OnFollowStarted?.Invoke(friendId);
            Debug.Log($"[SWEF] Multiplayer: Follow Me activated — targeting {friendId}");
        }

        /// <summary>Deactivates "Follow Me" mode.</summary>
        public void StopFollowMode()
        {
            FollowTargetId = null;
        }
        #endregion

        #region Distance Helper
        /// <summary>Approximate surface distance in metres using equirectangular projection.</summary>
        private static double ApproxDistanceMetres(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000.0;
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;
            double avgLat = (lat1 + lat2) / 2.0 * Math.PI / 180.0;
            double x = dLon * Math.Cos(avgLat);
            return R * Math.Sqrt(x * x + dLat * dLat);
        }
        #endregion
    }
}
