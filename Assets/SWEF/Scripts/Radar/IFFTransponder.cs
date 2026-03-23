// IFFTransponder.cs — SWEF Radar & Threat Detection System (Phase 67)
using UnityEngine;

namespace SWEF.Radar
{
    /// <summary>
    /// Component attached to any object that should be detectable by
    /// <see cref="RadarSystem"/>.
    /// <para>
    /// During each scan cycle the radar reads the transponder to classify the
    /// contact, retrieve its display name and radar-cross-section size, and
    /// apply the signature modifier to the signal-strength calculation.
    /// </para>
    /// </summary>
    public class IFFTransponder : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("IFF Identity")]
        [Tooltip("IFF classification broadcast by this transponder when active.")]
        /// <summary>IFF classification broadcast by this transponder when active.</summary>
        public ContactClassification identity = ContactClassification.Unknown;

        [Tooltip("Unique transponder code used to de-duplicate contacts.")]
        /// <summary>Unique transponder code used to de-duplicate contacts across scan cycles.</summary>
        public string transponderCode;

        [Tooltip("Label displayed on the radar scope (e.g., 'Eagle-2', 'Transport-7').")]
        /// <summary>Label displayed on the radar scope for this contact.</summary>
        public string displayName;

        [Header("Radar Signature")]
        [Tooltip("Radar cross-section category — affects blip size on the display.")]
        /// <summary>Radar cross-section category that determines blip size on the display.</summary>
        public BlipSize radarSignature = BlipSize.Medium;

        [Tooltip("Icon shown on the radar display for this contact.  Leave null for the default blip.")]
        /// <summary>Sprite shown on the radar display for this contact.</summary>
        public Sprite radarIcon;

        [Header("Transponder State")]
        [Tooltip("When false the transponder is silent; the contact will appear as Unknown.")]
        /// <summary>
        /// When <c>false</c> the transponder is silent and the radar will classify
        /// the contact as <see cref="ContactClassification.Unknown"/>.
        /// </summary>
        public bool isTransponderActive = true;

        [Tooltip("Multiplier applied to radar signal strength.  Values below 1 model stealth; " +
                 "values above 1 model large or reflective targets.")]
        [Range(0f, 5f)]
        /// <summary>
        /// Multiplier applied to radar signal strength during detection calculations.
        /// Values below 1 model stealth characteristics; values above 1 model large
        /// or highly reflective targets.
        /// </summary>
        public float signatureModifier = 1f;

        [Header("Threat Profile")]
        [Tooltip("Inherent threat level regardless of behaviour — used as a baseline by ThreatDetector.")]
        /// <summary>Inherent threat level of this object used as a baseline by <see cref="ThreatDetector"/>.</summary>
        public ThreatLevel baseThreatLevel = ThreatLevel.None;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the effective <see cref="ContactClassification"/> observed by the
        /// radar — <see cref="ContactClassification.Unknown"/> when the transponder is
        /// inactive.
        /// </summary>
        public ContactClassification EffectiveIdentity =>
            isTransponderActive ? identity : ContactClassification.Unknown;
    }
}
