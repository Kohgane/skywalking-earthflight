// SquadronManager.cs — Phase 109: Clan/Squadron System
// Central singleton MonoBehaviour — create/disband, join/leave, member management.
// Namespace: SWEF.Squadron

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SWEF.Squadron
{
    /// <summary>
    /// Phase 109 — Singleton MonoBehaviour that is the central authority for the
    /// Clan/Squadron system.  Handles squadron lifecycle, member management, rank
    /// promotions, invites, permission checking, and JSON persistence.
    ///
    /// <para>Attach to a persistent scene object — uses DontDestroyOnLoad.</para>
    /// </summary>
    public sealed class SquadronManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        /// <summary>Global singleton instance.</summary>
        public static SquadronManager Instance { get; private set; }

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Raised when a new squadron is successfully created.</summary>
        public event Action<SquadronInfo> OnSquadronCreated;

        /// <summary>Raised when the local player joins a squadron.</summary>
        public event Action<SquadronInfo> OnSquadronJoined;

        /// <summary>Raised when the local player leaves their current squadron.</summary>
        public event Action<string> OnSquadronLeft;

        /// <summary>Raised when any member joins the local player's squadron.</summary>
        public event Action<SquadronMember> OnMemberJoined;

        /// <summary>Raised when any member leaves the local player's squadron.</summary>
        public event Action<string> OnMemberLeft;

        /// <summary>Raised when a member is promoted to a higher rank.</summary>
        public event Action<SquadronMember> OnMemberPromoted;

        /// <summary>Raised when a member is demoted to a lower rank.</summary>
        public event Action<SquadronMember> OnMemberDemoted;

        /// <summary>Raised when the current squadron is disbanded.</summary>
        public event Action<string> OnSquadronDisbanded;

        // ── State ──────────────────────────────────────────────────────────────

        /// <summary>The squadron the local player currently belongs to, or null.</summary>
        public SquadronInfo CurrentSquadron { get; private set; }

        /// <summary>The local player's member record within their current squadron.</summary>
        public SquadronMember LocalMember { get; private set; }

        private readonly List<SquadronMember> _members = new List<SquadronMember>();
        private readonly List<SquadronInvite> _pendingInvites = new List<SquadronInvite>();

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Load();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API — Squadron lifecycle ────────────────────────────────────

        /// <summary>
        /// Creates a new squadron owned by the local player.
        /// </summary>
        /// <param name="name">Squadron name (3–30 chars).</param>
        /// <param name="tag">Short tag (2–6 chars).</param>
        /// <param name="description">Optional description.</param>
        /// <param name="type">Squadron type.</param>
        /// <returns>The newly created <see cref="SquadronInfo"/>, or null on failure.</returns>
        public SquadronInfo CreateSquadron(string name, string tag, string description, SquadronType type)
        {
            if (CurrentSquadron != null)
            {
                Debug.LogWarning("[SquadronManager] Cannot create: already in a squadron.");
                return null;
            }

            if (!ValidateName(name) || !ValidateTag(tag))
                return null;

            var info = new SquadronInfo
            {
                squadronId   = Guid.NewGuid().ToString(),
                name         = name,
                tag          = tag,
                description  = description ?? string.Empty,
                type         = type,
                status       = SquadronStatus.Active,
                leaderId     = GetLocalPlayerId(),
                createdAt    = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                level        = 1,
                totalXP      = 0,
                memberCount  = 1,
                maxMembers   = SquadronConfig.MaxMembers,
                isRecruiting = true
            };

            var leader = new SquadronMember
            {
                memberId              = GetLocalPlayerId(),
                displayName           = GetLocalPlayerName(),
                rank                  = SquadronRank.Leader,
                joinedAt              = info.createdAt,
                contributionXP        = 0,
                lastActive            = info.createdAt,
                totalSquadronFlights  = 0
            };

            CurrentSquadron = info;
            LocalMember = leader;
            _members.Clear();
            _members.Add(leader);

            Save();
            OnSquadronCreated?.Invoke(info);
            return info;
        }

        /// <summary>
        /// Disbands the current squadron. Only the Leader may do this.
        /// </summary>
        /// <returns>True on success.</returns>
        public bool DisbandSquadron()
        {
            if (CurrentSquadron == null || LocalMember?.rank != SquadronRank.Leader)
            {
                Debug.LogWarning("[SquadronManager] Cannot disband: not a leader or no squadron.");
                return false;
            }

            string id = CurrentSquadron.squadronId;
            CurrentSquadron.status = SquadronStatus.Disbanded;
            Save();

            string savedId = id;
            CurrentSquadron = null;
            LocalMember = null;
            _members.Clear();

            DeleteSaveFiles();
            OnSquadronDisbanded?.Invoke(savedId);
            return true;
        }

        // ── Public API — Join / leave ──────────────────────────────────────────

        /// <summary>
        /// Makes the local player join an existing squadron (open recruit).
        /// </summary>
        public bool JoinSquadron(SquadronInfo target)
        {
            if (CurrentSquadron != null)
            {
                Debug.LogWarning("[SquadronManager] Already in a squadron.");
                return false;
            }

            if (target == null || target.status != SquadronStatus.Active)
                return false;

            if (target.memberCount >= target.maxMembers)
            {
                Debug.LogWarning("[SquadronManager] Squadron is full.");
                return false;
            }

            var member = new SquadronMember
            {
                memberId             = GetLocalPlayerId(),
                displayName          = GetLocalPlayerName(),
                rank                 = SquadronRank.Recruit,
                joinedAt             = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                contributionXP       = 0,
                lastActive           = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                totalSquadronFlights = 0
            };

            CurrentSquadron = target;
            LocalMember = member;
            target.memberCount++;
            _members.Add(member);

            Save();
            OnSquadronJoined?.Invoke(target);
            OnMemberJoined?.Invoke(member);
            return true;
        }

        /// <summary>
        /// Makes the local player leave their current squadron.
        /// Leaders must transfer leadership or disband before leaving.
        /// </summary>
        public bool LeaveSquadron()
        {
            if (CurrentSquadron == null)
                return false;

            if (LocalMember?.rank == SquadronRank.Leader && _members.Count > 1)
            {
                Debug.LogWarning("[SquadronManager] Transfer leadership before leaving.");
                return false;
            }

            string squadronId = CurrentSquadron.squadronId;
            string memberId   = LocalMember?.memberId;

            CurrentSquadron.memberCount = Mathf.Max(0, CurrentSquadron.memberCount - 1);
            _members.RemoveAll(m => m.memberId == memberId);

            CurrentSquadron = null;
            LocalMember = null;

            Save();
            OnSquadronLeft?.Invoke(squadronId);
            if (memberId != null)
                OnMemberLeft?.Invoke(memberId);

            return true;
        }

        // ── Public API — Invite / kick ─────────────────────────────────────────

        /// <summary>
        /// Sends an invite to another player. Requires <see cref="SquadronPermission.InviteMembers"/>.
        /// </summary>
        public SquadronInvite SendInvite(string inviteeId, string message = "")
        {
            if (!HasPermission(SquadronPermission.InviteMembers))
            {
                Debug.LogWarning("[SquadronManager] No permission to invite.");
                return null;
            }

            if (_pendingInvites.Count >= SquadronConfig.MaxPendingInvites)
            {
                Debug.LogWarning("[SquadronManager] Max pending invites reached.");
                return null;
            }

            var invite = new SquadronInvite
            {
                inviteId   = Guid.NewGuid().ToString(),
                squadronId = CurrentSquadron.squadronId,
                inviterId  = GetLocalPlayerId(),
                inviteeId  = inviteeId,
                status     = SquadronInviteStatus.Pending,
                sentAt     = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                expiresAt  = DateTimeOffset.UtcNow.AddDays(SquadronConfig.InviteExpiryDays).ToUnixTimeSeconds(),
                message    = message ?? string.Empty
            };

            _pendingInvites.Add(invite);
            return invite;
        }

        /// <summary>
        /// Kicks a member from the squadron. Requires <see cref="SquadronPermission.KickMembers"/>.
        /// Cannot kick someone of equal or higher rank.
        /// </summary>
        public bool KickMember(string targetMemberId)
        {
            if (!HasPermission(SquadronPermission.KickMembers))
                return false;

            var target = _members.FirstOrDefault(m => m.memberId == targetMemberId);
            if (target == null || target.rank <= LocalMember.rank)
            {
                Debug.LogWarning("[SquadronManager] Cannot kick member of equal/higher rank.");
                return false;
            }

            _members.Remove(target);
            if (CurrentSquadron != null)
                CurrentSquadron.memberCount = Mathf.Max(0, CurrentSquadron.memberCount - 1);

            Save();
            OnMemberLeft?.Invoke(targetMemberId);
            return true;
        }

        // ── Public API — Rank management ───────────────────────────────────────

        /// <summary>
        /// Promotes a member by one rank step. Requires <see cref="SquadronPermission.PromoteMembers"/>.
        /// </summary>
        public bool PromoteMember(string targetMemberId)
        {
            if (!HasPermission(SquadronPermission.PromoteMembers))
                return false;

            var target = _members.FirstOrDefault(m => m.memberId == targetMemberId);
            if (target == null || target.rank == SquadronRank.Leader || target.rank <= LocalMember.rank)
                return false;

            // Enforce officer cap
            if (target.rank == SquadronRank.Veteran &&
                _members.Count(m => m.rank == SquadronRank.Officer) >= SquadronConfig.MaxOfficers)
            {
                Debug.LogWarning("[SquadronManager] Officer cap reached.");
                return false;
            }

            target.rank = (SquadronRank)((int)target.rank - 1);
            Save();
            OnMemberPromoted?.Invoke(target);
            return true;
        }

        /// <summary>
        /// Demotes a member by one rank step. Requires <see cref="SquadronPermission.PromoteMembers"/>.
        /// </summary>
        public bool DemoteMember(string targetMemberId)
        {
            if (!HasPermission(SquadronPermission.PromoteMembers))
                return false;

            var target = _members.FirstOrDefault(m => m.memberId == targetMemberId);
            if (target == null || target.rank == SquadronRank.Recruit || target.rank <= LocalMember.rank)
                return false;

            target.rank = (SquadronRank)((int)target.rank + 1);
            Save();
            OnMemberDemoted?.Invoke(target);
            return true;
        }

        // ── Public API — Permissions ───────────────────────────────────────────

        /// <summary>
        /// Returns true if the local player has the given permission based on their rank.
        /// </summary>
        public bool HasPermission(SquadronPermission permission)
        {
            if (LocalMember == null) return false;
            return PermissionMatrix.IsGranted(LocalMember.rank, permission);
        }

        // ── Public API — Discovery ─────────────────────────────────────────────

        /// <summary>
        /// Returns all members of the current squadron (read-only copy).
        /// </summary>
        public IReadOnlyList<SquadronMember> GetMembers() => _members.AsReadOnly();

        /// <summary>
        /// Returns all pending invites (read-only copy).
        /// </summary>
        public IReadOnlyList<SquadronInvite> GetPendingInvites() => _pendingInvites.AsReadOnly();

        // ── Public API — XP ────────────────────────────────────────────────────

        /// <summary>
        /// Adds XP to the squadron and recalculates level.
        /// Also increments the local member's contribution XP.
        /// </summary>
        public void AddSquadronXP(int amount)
        {
            if (CurrentSquadron == null || amount <= 0) return;

            CurrentSquadron.totalXP += amount;

            // Update level
            for (int lvl = 50; lvl >= 1; lvl--)
            {
                if (CurrentSquadron.totalXP >= SquadronConfig.LevelXPRequirements[lvl])
                {
                    CurrentSquadron.level = lvl;
                    break;
                }
            }

            if (LocalMember != null)
                LocalMember.contributionXP += amount;

            Save();
        }

        // ── Persistence ────────────────────────────────────────────────────────

        private void Save()
        {
            try
            {
                string dir = Application.persistentDataPath;

                if (CurrentSquadron != null)
                    File.WriteAllText(Path.Combine(dir, SquadronConfig.SquadronDataFile),
                        JsonUtility.ToJson(CurrentSquadron, true));

                var wrapper = new MemberListWrapper { members = _members };
                File.WriteAllText(Path.Combine(dir, SquadronConfig.MembersDataFile),
                    JsonUtility.ToJson(wrapper, true));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SquadronManager] Save error: {ex.Message}");
            }
        }

        private void Load()
        {
            try
            {
                string dir = Application.persistentDataPath;

                string squadronPath = Path.Combine(dir, SquadronConfig.SquadronDataFile);
                if (File.Exists(squadronPath))
                    CurrentSquadron = JsonUtility.FromJson<SquadronInfo>(File.ReadAllText(squadronPath));

                string membersPath = Path.Combine(dir, SquadronConfig.MembersDataFile);
                if (File.Exists(membersPath))
                {
                    var wrapper = JsonUtility.FromJson<MemberListWrapper>(File.ReadAllText(membersPath));
                    if (wrapper?.members != null)
                    {
                        _members.Clear();
                        _members.AddRange(wrapper.members);
                        LocalMember = _members.FirstOrDefault(m => m.memberId == GetLocalPlayerId());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SquadronManager] Load error: {ex.Message}");
            }
        }

        private void DeleteSaveFiles()
        {
            try
            {
                string dir = Application.persistentDataPath;
                foreach (var f in new[]
                {
                    SquadronConfig.SquadronDataFile,
                    SquadronConfig.MembersDataFile,
                    SquadronConfig.MissionsDataFile,
                    SquadronConfig.EventsDataFile,
                    SquadronConfig.BaseDataFile,
                    SquadronConfig.ChatDataFile
                })
                {
                    string path = Path.Combine(dir, f);
                    if (File.Exists(path)) File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SquadronManager] Delete error: {ex.Message}");
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static bool ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            int len = name.Trim().Length;
            return len >= SquadronConfig.NameMinLength && len <= SquadronConfig.NameMaxLength;
        }

        private static bool ValidateTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return false;
            int len = tag.Trim().Length;
            return len >= SquadronConfig.TagMinLength && len <= SquadronConfig.TagMaxLength;
        }

        /// <summary>Returns the local player's persistent ID. Override in subclass for real auth.</summary>
        protected virtual string GetLocalPlayerId() => SystemInfo.deviceUniqueIdentifier;

        /// <summary>Returns the local player's display name.</summary>
        protected virtual string GetLocalPlayerName() => "Pilot";

        // ── Serialisation helpers ──────────────────────────────────────────────

        [Serializable]
        private class MemberListWrapper
        {
            public List<SquadronMember> members;
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Permission matrix — maps rank to granted permissions
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Static helper that determines which permissions each rank possesses.
    /// </summary>
    public static class PermissionMatrix
    {
        private static readonly Dictionary<SquadronRank, HashSet<SquadronPermission>> _matrix =
            new Dictionary<SquadronRank, HashSet<SquadronPermission>>
            {
                [SquadronRank.Leader] = new HashSet<SquadronPermission>
                {
                    SquadronPermission.InviteMembers,
                    SquadronPermission.KickMembers,
                    SquadronPermission.EditBase,
                    SquadronPermission.StartMission,
                    SquadronPermission.ManageEvents,
                    SquadronPermission.EditSettings,
                    SquadronPermission.PromoteMembers
                },
                [SquadronRank.Officer] = new HashSet<SquadronPermission>
                {
                    SquadronPermission.InviteMembers,
                    SquadronPermission.KickMembers,
                    SquadronPermission.EditBase,
                    SquadronPermission.StartMission,
                    SquadronPermission.ManageEvents,
                    SquadronPermission.PromoteMembers
                },
                [SquadronRank.Veteran] = new HashSet<SquadronPermission>
                {
                    SquadronPermission.InviteMembers,
                    SquadronPermission.StartMission
                },
                [SquadronRank.Member] = new HashSet<SquadronPermission>
                {
                    SquadronPermission.StartMission
                },
                [SquadronRank.Recruit] = new HashSet<SquadronPermission>()
            };

        /// <summary>
        /// Returns true if <paramref name="rank"/> is granted <paramref name="permission"/>.
        /// </summary>
        public static bool IsGranted(SquadronRank rank, SquadronPermission permission)
            => _matrix.TryGetValue(rank, out var set) && set.Contains(permission);
    }
}
