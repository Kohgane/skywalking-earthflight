// RadarSystem.cs — SWEF Radar & Threat Detection System (Phase 67)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Radar
{
    /// <summary>
    /// Singleton MonoBehaviour that drives the entire radar pipeline:
    /// periodic <see cref="Physics.OverlapSphere"/> scans, contact creation /
    /// update / removal, IFF classification via <see cref="IFFTransponder"/>,
    /// signal-strength calculation, and target locking.
    /// <para>
    /// Attach to a persistent scene object.  All other radar components
    /// (ThreatDetector, RadarDisplay, MissileWarningReceiver) subscribe to
    /// this system's events.
    /// </para>
    /// </summary>
    public sealed class RadarSystem : MonoBehaviour
    {
        #region Singleton

        /// <summary>Shared singleton instance.</summary>
        public static RadarSystem Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Radar — Mode & Range")]
        [Tooltip("Initial operating mode of the radar.")]
        /// <summary>Current operating mode of the radar.</summary>
        public RadarMode currentMode = RadarMode.Active;

        [Tooltip("Maximum detection range in metres.")]
        [Min(1f)]
        /// <summary>Maximum detection range in metres.</summary>
        public float radarRange = RadarConfig.DefaultRadarRange;

        [Tooltip("Seconds between full scan cycles.")]
        [Min(0.05f)]
        /// <summary>Seconds between full scan cycles.</summary>
        public float scanInterval = RadarConfig.ScanInterval;

        [Header("Radar — Scan Arc")]
        [Tooltip("Horizontal scan arc in degrees (360 = full circle).")]
        [Range(1f, 360f)]
        /// <summary>Horizontal scan arc in degrees.</summary>
        public float scanArcHorizontal = 360f;

        [Tooltip("Vertical scan arc in degrees (120 covers −60° to +60°).")]
        [Range(1f, 180f)]
        /// <summary>Vertical scan arc in degrees.</summary>
        public float scanArcVertical = 120f;

        [Header("Radar — Contacts")]
        [Tooltip("Maximum number of simultaneously tracked contacts.")]
        [Min(1)]
        /// <summary>Maximum number of simultaneously tracked contacts.</summary>
        public int maxContacts = RadarConfig.MaxContacts;

        [Tooltip("Seconds after the last update before a contact is expired.")]
        [Min(0.1f)]
        /// <summary>Seconds after the last update before a stale contact is removed.</summary>
        public float contactTimeout = RadarConfig.ContactTimeout;

        [Tooltip("Distance at which signal strength begins to degrade (metres).")]
        [Min(1f)]
        /// <summary>Distance in metres at which signal strength starts to fall off.</summary>
        public float signalFalloffDistance = RadarConfig.DefaultRadarRange * RadarConfig.SignalFalloffStart;

        [Header("Radar — Layers")]
        [Tooltip("Physics layers that the radar OverlapSphere test can detect.")]
        /// <summary>Physics layers that the radar OverlapSphere query can detect.</summary>
        public LayerMask detectableLayers = ~0;

        [Header("Radar — Player Reference")]
        [Tooltip("Transform used as the radar origin.  Auto-resolved on Start if null.")]
        /// <summary>Transform used as the radar origin and bearing reference.</summary>
        [SerializeField] private Transform _radarOrigin;

        #endregion

        #region Runtime State

        /// <summary>All currently tracked contacts.</summary>
        public List<RadarContact> contacts { get; } = new List<RadarContact>();

        /// <summary>Currently locked/tracked contact.  <c>null</c> when no lock is active.</summary>
        public RadarContact lockedContact { get; private set; }

        private readonly Dictionary<string, RadarContact> _contactMap =
            new Dictionary<string, RadarContact>();

        private Coroutine _scanRoutine;
        private int _contactCounter;

        #endregion

        #region Events

        /// <summary>Raised when a new contact is added to the tracked list.</summary>
        public event Action<RadarContact> OnContactDetected;

        /// <summary>Raised when a tracked contact is removed (timed-out or destroyed).</summary>
        public event Action<RadarContact> OnContactLost;

        /// <summary>Raised when a contact is locked.</summary>
        public event Action<RadarContact> OnTargetLocked;

        /// <summary>Raised when the current lock is released.</summary>
        public event Action OnTargetUnlocked;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (_radarOrigin == null)
                _radarOrigin = transform;

            if (currentMode != RadarMode.Off)
                _scanRoutine = StartCoroutine(ScanRoutine());
        }

        private void Update()
        {
            CleanupStaleContacts();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Public API — Mode

        /// <summary>
        /// Changes the radar operating mode.
        /// <para>
        /// Switching to <see cref="RadarMode.Off"/> halts the scan coroutine and
        /// clears all contacts.  Any other mode (re-)starts scanning.
        /// </para>
        /// </summary>
        /// <param name="mode">New radar mode.</param>
        public void SetMode(RadarMode mode)
        {
            currentMode = mode;

            if (mode == RadarMode.Off)
            {
                if (_scanRoutine != null) { StopCoroutine(_scanRoutine); _scanRoutine = null; }
                ClearAllContacts();
            }
            else
            {
                if (_scanRoutine == null)
                    _scanRoutine = StartCoroutine(ScanRoutine());
            }
        }

        #endregion

        #region Public API — Target Lock

        /// <summary>Locks the radar onto the specified contact.</summary>
        /// <param name="contact">Contact to lock.  Must be in the active contacts list.</param>
        public void LockTarget(RadarContact contact)
        {
            if (contact == null) return;

            if (lockedContact != null)
                lockedContact.isLocked = false;

            lockedContact = contact;
            lockedContact.isLocked = true;
            OnTargetLocked?.Invoke(lockedContact);
        }

        /// <summary>Releases the current target lock.</summary>
        public void UnlockTarget()
        {
            if (lockedContact == null) return;
            lockedContact.isLocked = false;
            lockedContact = null;
            OnTargetUnlocked?.Invoke();
        }

        /// <summary>
        /// Cycles the lock through all tracked contacts, sorted nearest first.
        /// Wraps around to the first contact after the last.
        /// </summary>
        public void CycleTargets()
        {
            if (contacts.Count == 0) { UnlockTarget(); return; }

            List<RadarContact> sorted = new List<RadarContact>(contacts);
            sorted.Sort((a, b) => a.distance.CompareTo(b.distance));

            if (lockedContact == null)
            {
                LockTarget(sorted[0]);
                return;
            }

            int idx = sorted.IndexOf(lockedContact);
            int next = (idx + 1) % sorted.Count;
            LockTarget(sorted[next]);
        }

        #endregion

        #region Public API — Queries

        /// <summary>Returns the nearest hostile contact, or <c>null</c> if none exists.</summary>
        public RadarContact GetNearestHostile()
        {
            RadarContact nearest = null;
            float minDist = float.MaxValue;

            foreach (RadarContact c in contacts)
            {
                if (c.classification == ContactClassification.Hostile && c.distance < minDist)
                {
                    minDist = c.distance;
                    nearest = c;
                }
            }
            return nearest;
        }

        /// <summary>Returns the nearest contact regardless of classification, or <c>null</c> if none.</summary>
        public RadarContact GetNearestContact()
        {
            RadarContact nearest = null;
            float minDist = float.MaxValue;

            foreach (RadarContact c in contacts)
            {
                if (c.distance < minDist)
                {
                    minDist = c.distance;
                    nearest = c;
                }
            }
            return nearest;
        }

        /// <summary>
        /// Returns all tracked contacts that match the specified
        /// <see cref="ContactClassification"/>.
        /// </summary>
        /// <param name="classification">Classification to filter by.</param>
        /// <returns>List of matching contacts (may be empty).</returns>
        public List<RadarContact> GetContactsByClassification(ContactClassification classification)
        {
            var result = new List<RadarContact>();
            foreach (RadarContact c in contacts)
            {
                if (c.classification == classification)
                    result.Add(c);
            }
            return result;
        }

        #endregion

        #region Scan Routine

        private IEnumerator ScanRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(scanInterval);

                if (currentMode == RadarMode.Off || currentMode == RadarMode.Passive)
                    continue;

                PerformScan();
            }
        }

        private void PerformScan()
        {
            if (_radarOrigin == null) return;

            Vector3 origin = _radarOrigin.position;
            Collider[] hits = Physics.OverlapSphere(origin, radarRange, detectableLayers);

            foreach (Collider hit in hits)
            {
                if (hit.transform == _radarOrigin) continue;

                Vector3 toTarget = hit.transform.position - origin;
                float dist = toTarget.magnitude;

                // Elevation check
                float elevDeg = Mathf.Asin(Mathf.Clamp(toTarget.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
                if (Mathf.Abs(elevDeg) > scanArcVertical * 0.5f) continue;

                // Bearing check (horizontal arc)
                Vector3 flatDir = new Vector3(toTarget.x, 0f, toTarget.z).normalized;
                Vector3 flatFwd = new Vector3(_radarOrigin.forward.x, 0f, _radarOrigin.forward.z).normalized;
                float bearingAngle = Vector3.SignedAngle(flatFwd, flatDir, Vector3.up);
                if (bearingAngle < 0f) bearingAngle += 360f;

                if (scanArcHorizontal < 360f)
                {
                    float halfArc = scanArcHorizontal * 0.5f;
                    float diff = Mathf.DeltaAngle(0f, bearingAngle);
                    if (Mathf.Abs(diff) > halfArc) continue;
                }

                IFFTransponder transponder = hit.GetComponent<IFFTransponder>();
                float sigMod = transponder != null ? transponder.signatureModifier : 1f;
                float signal = CalculateSignalStrength(dist, sigMod);

                if (signal <= 0f) continue;

                string id = transponder != null && !string.IsNullOrEmpty(transponder.transponderCode)
                    ? transponder.transponderCode
                    : hit.GetInstanceID().ToString();

                if (_contactMap.TryGetValue(id, out RadarContact existing))
                {
                    UpdateContact(existing, hit.transform, dist, bearingAngle, elevDeg, signal, transponder);
                }
                else
                {
                    if (contacts.Count >= maxContacts) continue;
                    RadarContact newContact = CreateContact(id, hit.transform, dist, bearingAngle, elevDeg, signal, transponder);
                    contacts.Add(newContact);
                    _contactMap[id] = newContact;
                    OnContactDetected?.Invoke(newContact);
                }
            }
        }

        private RadarContact CreateContact(
            string id, Transform t, float dist, float bearing, float elev,
            float signal, IFFTransponder transponder)
        {
            var c = new RadarContact
            {
                contactId         = id,
                trackedTransform  = t,
                classification    = transponder != null ? transponder.EffectiveIdentity : ContactClassification.Unknown,
                threat            = transponder != null ? transponder.baseThreatLevel : ThreatLevel.None,
                size              = transponder != null ? transponder.radarSignature : BlipSize.Medium,
                position          = t.position,
                velocity          = Vector3.zero,
                distance          = dist,
                bearing           = bearing,
                elevation         = elev,
                signalStrength    = signal,
                firstDetectedTime = Time.time,
                lastUpdateTime    = Time.time,
                isLocked          = false,
                displayName       = transponder != null && !string.IsNullOrEmpty(transponder.displayName)
                                        ? transponder.displayName
                                        : $"Unknown-{++_contactCounter:D2}",
                contactIcon       = transponder != null ? transponder.radarIcon : null
            };
            return c;
        }

        private void UpdateContact(
            RadarContact c, Transform t, float dist, float bearing, float elev,
            float signal, IFFTransponder transponder)
        {
            float dt = Time.time - c.lastUpdateTime;
            if (dt > 0f)
                c.velocity = (t.position - c.position) / dt;

            c.trackedTransform = t;
            c.position         = t.position;
            c.distance         = dist;
            c.bearing          = bearing;
            c.elevation        = elev;
            c.signalStrength   = signal;
            c.lastUpdateTime   = Time.time;

            if (transponder != null)
            {
                c.classification = transponder.EffectiveIdentity;
                c.size           = transponder.radarSignature;
                if (!string.IsNullOrEmpty(transponder.displayName))
                    c.displayName = transponder.displayName;
                if (transponder.radarIcon != null)
                    c.contactIcon = transponder.radarIcon;
            }
        }

        private float CalculateSignalStrength(float dist, float signatureMod)
        {
            if (dist >= radarRange) return 0f;

            float strength;
            if (dist <= signalFalloffDistance)
            {
                strength = 1f;
            }
            else
            {
                float t = (dist - signalFalloffDistance) / (radarRange - signalFalloffDistance);
                strength = Mathf.Clamp01(1f - t);
            }
            return Mathf.Clamp01(strength * signatureMod);
        }

        #endregion

        #region Contact Cleanup

        private void CleanupStaleContacts()
        {
            for (int i = contacts.Count - 1; i >= 0; i--)
            {
                RadarContact c = contacts[i];
                bool timedOut  = (Time.time - c.lastUpdateTime) > contactTimeout;
                bool destroyed = c.trackedTransform == null;

                if (timedOut || destroyed)
                {
                    if (c == lockedContact) UnlockTarget();
                    contacts.RemoveAt(i);
                    _contactMap.Remove(c.contactId);
                    OnContactLost?.Invoke(c);
                }
            }
        }

        private void ClearAllContacts()
        {
            foreach (RadarContact c in contacts)
            {
                if (c == lockedContact) UnlockTarget();
                OnContactLost?.Invoke(c);
            }
            contacts.Clear();
            _contactMap.Clear();
        }

        #endregion
    }
}
