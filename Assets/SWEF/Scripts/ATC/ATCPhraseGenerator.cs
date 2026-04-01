using System.Collections.Generic;
using System.Text;
using UnityEngine;

#if SWEF_LOCALIZATION_AVAILABLE
using SWEF.Localization;
#endif

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 78 — Generates realistic ATC phraseology for clearances, readbacks,
    /// and ATIS broadcasts.
    ///
    /// <para>Uses the NATO phonetic alphabet for callsigns and integrates with
    /// <c>SWEF.Localization.LocalizationManager</c> when
    /// <c>SWEF_LOCALIZATION_AVAILABLE</c> is defined.</para>
    ///
    /// <para>Set <see cref="ATCSettings.realisticPhraseology"/> to <c>false</c>
    /// for simplified, casual-player language.</para>
    /// </summary>
    public class ATCPhraseGenerator : MonoBehaviour
    {
        #region NATO Phonetic Alphabet

        private static readonly Dictionary<char, string> NatoAlphabet = new Dictionary<char, string>
        {
            {'A',"Alpha"},   {'B',"Bravo"},   {'C',"Charlie"}, {'D',"Delta"},
            {'E',"Echo"},    {'F',"Foxtrot"},  {'G',"Golf"},    {'H',"Hotel"},
            {'I',"India"},   {'J',"Juliet"},   {'K',"Kilo"},    {'L',"Lima"},
            {'M',"Mike"},    {'N',"November"}, {'O',"Oscar"},   {'P',"Papa"},
            {'Q',"Quebec"},  {'R',"Romeo"},    {'S',"Sierra"},  {'T',"Tango"},
            {'U',"Uniform"}, {'V',"Victor"},   {'W',"Whiskey"}, {'X',"X-ray"},
            {'Y',"Yankee"},  {'Z',"Zulu"},
            {'0',"Zero"},    {'1',"One"},      {'2',"Two"},     {'3',"Three"},
            {'4',"Four"},    {'5',"Five"},     {'6',"Six"},     {'7',"Seven"},
            {'8',"Eight"},   {'9',"Niner"}
        };

        #endregion

        #region Inspector

        [Tooltip("Player callsign used when generating phrases. Defaults to 'SWEF 1'.")]
        [SerializeField] private string playerCallsign = "SWEF 1";

        [Tooltip("Station name broadcast in ATIS messages.")]
        [SerializeField] private string stationName = "Tower";

        #endregion

        #region Public API

        /// <summary>
        /// Generates an ATC clearance phrase for the given instruction.
        /// </summary>
        /// <param name="instruction">The clearance instruction to describe.</param>
        /// <returns>Localised ATC phrase string.</returns>
        public string GenerateClearance(ATCInstruction instruction)
        {
            bool realistic = ATCManager.Instance != null &&
                             ATCManager.Instance.Settings.realisticPhraseology;

            var sb = new StringBuilder();
            sb.Append(SpellCallsign(playerCallsign));
            sb.Append(", ");
            sb.Append(GetClearancePhrase(instruction, realistic));
            return sb.ToString();
        }

        /// <summary>
        /// Generates a pilot readback for the given instruction.
        /// </summary>
        /// <param name="instruction">The clearance instruction to read back.</param>
        /// <returns>Pilot readback phrase string.</returns>
        public string GenerateReadback(ATCInstruction instruction)
        {
            bool realistic = ATCManager.Instance != null &&
                             ATCManager.Instance.Settings.realisticPhraseology;

            var sb = new StringBuilder();
            sb.Append(GetReadbackPhrase(instruction, realistic));
            sb.Append(", ");
            sb.Append(SpellCallsign(playerCallsign));
            return sb.ToString();
        }

        /// <summary>
        /// Generates an ATIS (Automatic Terminal Information Service) broadcast string.
        /// </summary>
        /// <param name="airport">Airport ICAO code.</param>
        /// <param name="weather">Brief weather description.</param>
        /// <returns>ATIS broadcast text.</returns>
        public string GenerateATIS(string airport, string weather)
        {
            string info = NatoAlphabet.ContainsKey('A') ?
                NatoAlphabet[(char)('A' + (UnityEngine.Random.Range(0, 26)))] : "Alpha";

            return $"{airport} information {info}. {weather}. Advise on initial contact, you have information {info}.";
        }

        /// <summary>Converts a callsign string to NATO phonetic spelling.</summary>
        /// <param name="callsign">Raw callsign, e.g. \"SWR 123\".</param>
        /// <returns>Phonetically spelled callsign string.</returns>
        public string SpellCallsign(string callsign)
        {
            if (string.IsNullOrEmpty(callsign)) return callsign;
            var sb = new StringBuilder();
            foreach (char c in callsign.ToUpperInvariant())
            {
                if (c == ' ') { sb.Append(' '); continue; }
                if (NatoAlphabet.TryGetValue(c, out string word))
                    sb.Append(word).Append(' ');
                else
                    sb.Append(c).Append(' ');
            }
            return sb.ToString().TrimEnd();
        }

        #endregion

        #region Phrase Building

        private string GetClearancePhrase(ATCInstruction instr, bool realistic)
        {
            switch (instr.clearanceType)
            {
                case Clearance.Taxi:
                    return realistic
                        ? $"taxi to holding point runway {instr.assignedRunway} via taxiways"
                        : $"taxi to runway {instr.assignedRunway}";

                case Clearance.Takeoff:
                    return realistic
                        ? $"runway {instr.assignedRunway}, cleared for takeoff, wind calm"
                        : $"cleared takeoff runway {instr.assignedRunway}";

                case Clearance.Landing:
                    return realistic
                        ? $"runway {instr.assignedRunway}, cleared to land"
                        : $"cleared to land runway {instr.assignedRunway}";

                case Clearance.Approach:
                    return realistic
                        ? $"cleared ILS approach runway {instr.assignedRunway}"
                        : $"cleared approach runway {instr.assignedRunway}";

                case Clearance.Altitude:
                    return realistic
                        ? $"climb and maintain flight level {(int)(instr.assignedAltitude / 100f):D3}"
                        : $"climb to {instr.assignedAltitude:F0} feet";

                case Clearance.Speed:
                    return realistic
                        ? $"reduce speed to {instr.assignedSpeed:F0} knots"
                        : $"slow to {instr.assignedSpeed:F0} knots";

                case Clearance.Heading:
                    return realistic
                        ? $"fly heading {instr.assignedHeading:000}"
                        : $"turn to heading {instr.assignedHeading:000}";

                case Clearance.Hold:
                    return realistic
                        ? $"hold as published, expect further clearance in 10 minutes"
                        : "enter holding pattern";

                case Clearance.GoAround:
                    return realistic
                        ? "go around, fly runway heading, climb to 3,000 feet"
                        : "go around";

                default:
                    return "standby";
            }
        }

        private string GetReadbackPhrase(ATCInstruction instr, bool realistic)
        {
            switch (instr.clearanceType)
            {
                case Clearance.Taxi:       return realistic ? $"taxi to runway {instr.assignedRunway}" : $"runway {instr.assignedRunway}";
                case Clearance.Takeoff:    return realistic ? $"cleared for takeoff runway {instr.assignedRunway}" : "takeoff clearance";
                case Clearance.Landing:    return realistic ? $"cleared to land runway {instr.assignedRunway}" : "cleared to land";
                case Clearance.Approach:   return realistic ? $"cleared ILS runway {instr.assignedRunway}" : "cleared approach";
                case Clearance.Altitude:   return realistic ? $"climb to {instr.assignedAltitude:F0}" : $"{instr.assignedAltitude:F0} feet";
                case Clearance.Speed:      return realistic ? $"speed {instr.assignedSpeed:F0}" : $"{instr.assignedSpeed:F0} knots";
                case Clearance.Heading:    return realistic ? $"heading {instr.assignedHeading:000}" : $"{instr.assignedHeading:000}";
                case Clearance.Hold:       return "wilco, holding";
                case Clearance.GoAround:   return "going around";
                default:                   return "roger";
            }
        }

        #endregion
    }
}
