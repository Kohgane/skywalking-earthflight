// TeamLiveryManager.cs — Phase 115: Advanced Aircraft Livery Editor
// Team/squadron shared liveries: team color scheme, auto-apply to team members.
// Namespace: SWEF.LiveryEditor

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Manages shared liveries for a team or squadron.
    /// Stores a team colour scheme and provides auto-application helpers
    /// so that all team members can quickly adopt a consistent livery.
    /// </summary>
    public class TeamLiveryManager : MonoBehaviour
    {
        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when the team livery is updated.</summary>
        public event Action<LiverySaveData> OnTeamLiveryUpdated;

        /// <summary>Raised when the team livery is applied to a member.</summary>
        public event Action<string, LiverySaveData> OnTeamLiveryApplied;

        // ── Internal state ────────────────────────────────────────────────────────
        private LiverySaveData _teamLivery;
        private Color _teamPrimaryColor   = Color.blue;
        private Color _teamSecondaryColor = Color.white;
        private readonly List<string> _memberIds = new List<string>();

        // ── Public properties ─────────────────────────────────────────────────────
        /// <summary>Currently active team livery.</summary>
        public LiverySaveData TeamLivery => _teamLivery;

        /// <summary>Primary team colour.</summary>
        public Color TeamPrimaryColor => _teamPrimaryColor;

        /// <summary>Secondary team colour.</summary>
        public Color TeamSecondaryColor => _teamSecondaryColor;

        /// <summary>Read-only list of team member IDs.</summary>
        public IReadOnlyList<string> MemberIds => _memberIds.AsReadOnly();

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Sets the active team livery.</summary>
        public void SetTeamLivery(LiverySaveData livery)
        {
            _teamLivery = livery ?? throw new ArgumentNullException(nameof(livery));
            OnTeamLiveryUpdated?.Invoke(_teamLivery);
        }

        /// <summary>Updates the team colour scheme.</summary>
        public void SetTeamColors(Color primary, Color secondary)
        {
            _teamPrimaryColor   = primary;
            _teamSecondaryColor = secondary;
        }

        /// <summary>Applies the current team livery to a specific member aircraft.</summary>
        public void ApplyToMember(string memberId)
        {
            if (_teamLivery == null)
            {
                Debug.LogWarning("[SWEF] TeamLiveryManager: no team livery set.");
                return;
            }
            OnTeamLiveryApplied?.Invoke(memberId, _teamLivery);
        }

        /// <summary>Applies the team livery to all registered members.</summary>
        public void ApplyToAllMembers()
        {
            foreach (var id in _memberIds) ApplyToMember(id);
        }

        /// <summary>Registers a team member by their aircraft ID.</summary>
        public void AddMember(string memberId)
        {
            if (!_memberIds.Contains(memberId)) _memberIds.Add(memberId);
        }

        /// <summary>Removes a team member from the roster.</summary>
        public bool RemoveMember(string memberId) => _memberIds.Remove(memberId);
    }
}
