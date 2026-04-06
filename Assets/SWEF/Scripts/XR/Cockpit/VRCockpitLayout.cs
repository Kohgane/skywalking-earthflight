// VRCockpitLayout.cs — Phase 112: VR/XR Flight Experience
// Configurable cockpit layouts per aircraft type.
// Namespace: SWEF.XR

using System;
using UnityEngine;

namespace SWEF.XR
{
    /// <summary>Aircraft type category for cockpit layout selection.</summary>
    public enum CockpitAircraftType
    {
        /// <summary>Single-engine general aviation aircraft.</summary>
        GeneralAviation,
        /// <summary>Commercial airliner (737/A320 style).</summary>
        CommercialAirliner,
        /// <summary>Military fighter jet.</summary>
        FighterJet,
        /// <summary>Helicopter.</summary>
        Helicopter,
        /// <summary>Spacecraft / futuristic.</summary>
        Spacecraft
    }

    /// <summary>
    /// ScriptableObject that defines the 3D positions and orientations of
    /// all cockpit instruments and controls for a given aircraft type.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/XR/Cockpit Layout", fileName = "CockpitLayout")]
    public class VRCockpitLayout : ScriptableObject
    {
        [Header("Aircraft")]
        /// <summary>Aircraft type this layout applies to.</summary>
        public CockpitAircraftType aircraftType = CockpitAircraftType.GeneralAviation;

        [Header("Instrument Panel")]
        /// <summary>Local position of the instrument panel relative to the cockpit root.</summary>
        public Vector3  instrumentPanelPosition = new Vector3(0f,   0.2f,  0.6f);
        /// <summary>Local rotation of the instrument panel.</summary>
        public Vector3  instrumentPanelRotation = new Vector3(15f,  0f,    0f);

        [Header("Controls")]
        /// <summary>Local position of the throttle quadrant.</summary>
        public Vector3  throttlePosition = new Vector3(0.3f,  0.0f,  0.35f);
        /// <summary>Local position of the yoke/stick.</summary>
        public Vector3  yokePosition     = new Vector3(0f,    0.05f, 0.4f);
        /// <summary>Local position of the rudder pedals.</summary>
        public Vector3  ruddersPosition  = new Vector3(0f,   -0.25f, 0.7f);

        [Header("Seats")]
        /// <summary>Local position of the pilot seat (entry teleport target).</summary>
        public Vector3  pilotSeatPosition = new Vector3(0f,   0f,    0f);
    }
}
