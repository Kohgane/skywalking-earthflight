// MissileWarningReceiver.cs — SWEF Radar & Threat Detection System (Phase 67)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Radar
{
    /// <summary>
    /// Detects incoming missiles and fast-closing threats within a configurable
    /// spherical detection volume and raises audio / event warnings.
    /// <para>
    /// Attach to the player aircraft.  The component performs
    /// <see cref="Physics.OverlapSphere"/> checks at <see cref="warningUpdateRate"/>
    /// times per second and drives the <see cref="warningAudioSource"/> accordingly.
    /// </para>
    /// </summary>
    public class MissileWarningReceiver : MonoBehaviour
    {
        #region Inspector

        [Header("Missile Warning — Detection")]
        [Tooltip("Radius in metres within which incoming missiles are detected.")]
        [Min(1f)]
        /// <summary>Radius in metres of the missile-detection sphere.</summary>
        public float detectionRange = 3000f;

        [Tooltip("Full-sphere detection angle (360 = omnidirectional).")]
        [Range(1f, 360f)]
        /// <summary>Detection cone angle in degrees.  360 gives omnidirectional detection.</summary>
        public float detectionAngle = 360f;

        [Tooltip("Interval in seconds between each threat check (e.g. 0.2 s = 5 checks per second).")]
        [Min(0.05f)]
        /// <summary>Interval in seconds between each threat scan (0.2 s yields 5 checks per second).</summary>
        public float warningUpdateRate = 0.2f;

        [Tooltip("Physics layers that contain missile / projectile objects.")]
        /// <summary>Layer mask identifying missile or projectile colliders.</summary>
        public LayerMask missileLayers;

        [Header("Missile Warning — Audio")]
        [Tooltip("AudioSource used for missile and lock warning tones.")]
        /// <summary>AudioSource used to play warning tones.</summary>
        public AudioSource warningAudioSource;

        [Tooltip("Audio clip played when a missile is detected inbound.")]
        /// <summary>Audio clip for the MISSILE MISSILE alert tone.</summary>
        public AudioClip missileWarningTone;

        [Tooltip("Audio clip played when an enemy radar lock is detected.")]
        /// <summary>Audio clip for the radar-lock warning tone.</summary>
        public AudioClip lockWarningTone;

        #endregion

        #region Runtime State

        /// <summary>Currently detected incoming projectile transforms.</summary>
        public List<Transform> incomingThreats { get; } = new List<Transform>();

        /// <summary>Whether the player is currently under radar lock.</summary>
        public bool isUnderLock { get; private set; }

        /// <summary>Whether at least one missile is currently inbound.</summary>
        public bool isMissileIncoming => incomingThreats.Count > 0;

        /// <summary>Bearing in degrees to the nearest incoming threat (0 = ahead).</summary>
        public float incomingBearing { get; private set; }

        /// <summary>Elevation in degrees to the nearest incoming threat.</summary>
        public float incomingElevation { get; private set; }

        private Coroutine _checkRoutine;
        private bool      _wasIncomingLastCheck;

        #endregion

        #region Events

        /// <summary>
        /// Raised when a new missile is detected inbound.  The
        /// <see cref="Vector3"/> parameter is the missile's world-space position.
        /// </summary>
        public event Action<Vector3> OnMissileDetected;

        /// <summary>Raised when an enemy radar lock is detected on the player.</summary>
        public event Action OnMissileLockDetected;

        /// <summary>Raised when all previously detected threats have been cleared.</summary>
        public event Action OnThreatCleared;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _checkRoutine = StartCoroutine(ThreatCheckRoutine());
        }

        private void OnDestroy()
        {
            if (_checkRoutine != null) StopCoroutine(_checkRoutine);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Externally signals that an enemy radar has locked onto the player
        /// (e.g., called by an enemy <see cref="RadarSystem"/>).
        /// </summary>
        public void NotifyRadarLock()
        {
            if (isUnderLock) return;
            isUnderLock = true;
            PlayTone(lockWarningTone);
            OnMissileLockDetected?.Invoke();
        }

        /// <summary>Clears the radar-lock state (called when the enemy breaks lock).</summary>
        public void ClearRadarLock()
        {
            isUnderLock = false;
        }

        #endregion

        #region Private — Check Loop

        private IEnumerator ThreatCheckRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(warningUpdateRate);
                PerformThreatCheck();
            }
        }

        private void PerformThreatCheck()
        {
            incomingThreats.Clear();

            Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange, missileLayers);
            foreach (Collider hit in hits)
            {
                Vector3 toSelf    = transform.position - hit.transform.position;
                Vector3 velocity  = Vector3.zero;

                Rigidbody rb = hit.attachedRigidbody;
                if (rb != null) velocity = rb.linearVelocity;

                if (velocity.magnitude < 1f) continue;

                // Check if the projectile is heading toward the player.
                float dot = Vector3.Dot(velocity.normalized, toSelf.normalized);
                if (dot < 0.5f) continue; // Not heading toward player.

                // Check detection angle.
                if (detectionAngle < 360f)
                {
                    float angle = Vector3.Angle(transform.forward, -toSelf);
                    if (angle > detectionAngle * 0.5f) continue;
                }

                incomingThreats.Add(hit.transform);
            }

            bool currentlyIncoming = incomingThreats.Count > 0;

            if (currentlyIncoming)
            {
                UpdateDirectionToNearestThreat();

                if (!_wasIncomingLastCheck)
                {
                    PlayTone(missileWarningTone);
                    if (incomingThreats.Count > 0)
                        OnMissileDetected?.Invoke(incomingThreats[0].position);
                }
            }
            else if (_wasIncomingLastCheck)
            {
                StopTone();
                OnThreatCleared?.Invoke();
            }

            _wasIncomingLastCheck = currentlyIncoming;
        }

        private void UpdateDirectionToNearestThreat()
        {
            if (incomingThreats.Count == 0) return;

            Transform nearest = incomingThreats[0];
            float     minDist = Vector3.Distance(transform.position, nearest.position);
            foreach (Transform t in incomingThreats)
            {
                float d = Vector3.Distance(transform.position, t.position);
                if (d < minDist) { minDist = d; nearest = t; }
            }

            Vector3 toThreat = nearest.position - transform.position;
            Vector3 flatDir  = new Vector3(toThreat.x, 0f, toThreat.z).normalized;
            Vector3 flatFwd  = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;

            float bearing = Vector3.SignedAngle(flatFwd, flatDir, Vector3.up);
            if (bearing < 0f) bearing += 360f;

            incomingBearing   = bearing;
            incomingElevation = Mathf.Asin(Mathf.Clamp(toThreat.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
        }

        private void PlayTone(AudioClip clip)
        {
            if (warningAudioSource == null || clip == null) return;
            if (warningAudioSource.isPlaying && warningAudioSource.clip == clip) return;
            warningAudioSource.Stop();
            warningAudioSource.clip = clip;
            warningAudioSource.loop = true;
            warningAudioSource.Play();
        }

        private void StopTone()
        {
            if (warningAudioSource != null && warningAudioSource.isPlaying)
                warningAudioSource.Stop();
        }

        #endregion
    }
}
