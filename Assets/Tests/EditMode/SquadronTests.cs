// SquadronTests.cs — NUnit EditMode tests for Phase 109: Clan/Squadron System
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using SWEF.Squadron;

[TestFixture]
public class SquadronTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // SquadronEnums
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SquadronRank_AllValuesAreDefined()
    {
        var values = (SquadronRank[])Enum.GetValues(typeof(SquadronRank));
        Assert.GreaterOrEqual(values.Length, 5);
        Assert.Contains(SquadronRank.Leader,  values);
        Assert.Contains(SquadronRank.Officer, values);
        Assert.Contains(SquadronRank.Veteran, values);
        Assert.Contains(SquadronRank.Member,  values);
        Assert.Contains(SquadronRank.Recruit, values);
    }

    [Test]
    public void SquadronType_AllValuesAreDefined()
    {
        var values = (SquadronType[])Enum.GetValues(typeof(SquadronType));
        Assert.Contains(SquadronType.Casual,      values);
        Assert.Contains(SquadronType.Competitive, values);
        Assert.Contains(SquadronType.Exploration, values);
        Assert.Contains(SquadronType.Training,    values);
        Assert.Contains(SquadronType.Military,    values);
        Assert.Contains(SquadronType.Aerobatics,  values);
    }

    [Test]
    public void SquadronStatus_AllValuesAreDefined()
    {
        var values = (SquadronStatus[])Enum.GetValues(typeof(SquadronStatus));
        Assert.Contains(SquadronStatus.Active,    values);
        Assert.Contains(SquadronStatus.Inactive,  values);
        Assert.Contains(SquadronStatus.Disbanded, values);
        Assert.Contains(SquadronStatus.Suspended, values);
    }

    [Test]
    public void SquadronMissionType_AllValuesAreDefined()
    {
        var values = (SquadronMissionType[])Enum.GetValues(typeof(SquadronMissionType));
        Assert.GreaterOrEqual(values.Length, 8);
        Assert.Contains(SquadronMissionType.FormationFlight, values);
        Assert.Contains(SquadronMissionType.AreaExploration, values);
        Assert.Contains(SquadronMissionType.SpeedRun,        values);
        Assert.Contains(SquadronMissionType.RelayRace,       values);
        Assert.Contains(SquadronMissionType.PatrolRoute,     values);
        Assert.Contains(SquadronMissionType.SearchAndRescue, values);
        Assert.Contains(SquadronMissionType.AirShow,         values);
        Assert.Contains(SquadronMissionType.ConvoyEscort,    values);
    }

    [Test]
    public void SquadronEventType_AllValuesAreDefined()
    {
        var values = (SquadronEventType[])Enum.GetValues(typeof(SquadronEventType));
        Assert.Contains(SquadronEventType.Weekly,      values);
        Assert.Contains(SquadronEventType.Monthly,     values);
        Assert.Contains(SquadronEventType.Seasonal,    values);
        Assert.Contains(SquadronEventType.Special,     values);
        Assert.Contains(SquadronEventType.Tournament,  values);
        Assert.Contains(SquadronEventType.Anniversary, values);
    }

    [Test]
    public void SquadronFacility_AllValuesAreDefined()
    {
        var values = (SquadronFacility[])Enum.GetValues(typeof(SquadronFacility));
        Assert.GreaterOrEqual(values.Length, 8);
        Assert.Contains(SquadronFacility.Hangar,         values);
        Assert.Contains(SquadronFacility.ControlTower,   values);
        Assert.Contains(SquadronFacility.BriefingRoom,   values);
        Assert.Contains(SquadronFacility.FuelDepot,      values);
        Assert.Contains(SquadronFacility.RepairBay,      values);
        Assert.Contains(SquadronFacility.RecRoom,        values);
        Assert.Contains(SquadronFacility.TrainingGround, values);
        Assert.Contains(SquadronFacility.TrophyRoom,     values);
    }

    [Test]
    public void SquadronInviteStatus_AllValuesAreDefined()
    {
        var values = (SquadronInviteStatus[])Enum.GetValues(typeof(SquadronInviteStatus));
        Assert.Contains(SquadronInviteStatus.Pending,  values);
        Assert.Contains(SquadronInviteStatus.Accepted, values);
        Assert.Contains(SquadronInviteStatus.Declined, values);
        Assert.Contains(SquadronInviteStatus.Expired,  values);
    }

    [Test]
    public void SquadronPermission_AllValuesAreDefined()
    {
        var values = (SquadronPermission[])Enum.GetValues(typeof(SquadronPermission));
        Assert.GreaterOrEqual(values.Length, 7);
        Assert.Contains(SquadronPermission.InviteMembers,  values);
        Assert.Contains(SquadronPermission.KickMembers,    values);
        Assert.Contains(SquadronPermission.EditBase,       values);
        Assert.Contains(SquadronPermission.StartMission,   values);
        Assert.Contains(SquadronPermission.ManageEvents,   values);
        Assert.Contains(SquadronPermission.EditSettings,   values);
        Assert.Contains(SquadronPermission.PromoteMembers, values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SquadronConfig
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SquadronConfig_MemberLimits_AreValid()
    {
        Assert.Greater(SquadronConfig.MaxMembers,      0);
        Assert.Greater(SquadronConfig.MaxOfficers,     0);
        Assert.Less(SquadronConfig.MaxOfficers,        SquadronConfig.MaxMembers);
        Assert.Greater(SquadronConfig.MaxPendingInvites, 0);
    }

    [Test]
    public void SquadronConfig_NameLengths_AreValid()
    {
        Assert.Greater(SquadronConfig.NameMinLength, 0);
        Assert.Greater(SquadronConfig.NameMaxLength, SquadronConfig.NameMinLength);
        Assert.Greater(SquadronConfig.TagMinLength,  0);
        Assert.Greater(SquadronConfig.TagMaxLength,  SquadronConfig.TagMinLength);
    }

    [Test]
    public void SquadronConfig_FacilityUpgradeCosts_HasCorrectCount()
    {
        Assert.AreEqual(SquadronConfig.FacilityMaxLevel, SquadronConfig.FacilityUpgradeCosts.Length);
    }

    [Test]
    public void SquadronConfig_FacilityUpgradeCosts_AreAscending()
    {
        for (int i = 1; i < SquadronConfig.FacilityUpgradeCosts.Length; i++)
            Assert.Greater(SquadronConfig.FacilityUpgradeCosts[i],
                           SquadronConfig.FacilityUpgradeCosts[i - 1],
                           $"Cost at index {i} should be greater than cost at index {i - 1}");
    }

    [Test]
    public void SquadronConfig_LevelXPRequirements_HasLevel50()
    {
        Assert.AreEqual(51, SquadronConfig.LevelXPRequirements.Length);
        Assert.AreEqual(0,  SquadronConfig.LevelXPRequirements[1]);
        Assert.Greater(SquadronConfig.LevelXPRequirements[50],
                       SquadronConfig.LevelXPRequirements[49]);
    }

    [Test]
    public void SquadronConfig_ChatSettings_AreValid()
    {
        Assert.Greater(SquadronConfig.ChatHistoryMax,      0);
        Assert.Greater(SquadronConfig.ChatMessageMaxLength, 0);
    }

    [Test]
    public void SquadronConfig_PersistencePaths_AreNotEmpty()
    {
        Assert.IsFalse(string.IsNullOrEmpty(SquadronConfig.SquadronDataFile));
        Assert.IsFalse(string.IsNullOrEmpty(SquadronConfig.MembersDataFile));
        Assert.IsFalse(string.IsNullOrEmpty(SquadronConfig.MissionsDataFile));
        Assert.IsFalse(string.IsNullOrEmpty(SquadronConfig.EventsDataFile));
        Assert.IsFalse(string.IsNullOrEmpty(SquadronConfig.BaseDataFile));
        Assert.IsFalse(string.IsNullOrEmpty(SquadronConfig.ChatDataFile));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SquadronData — data model construction
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SquadronInfo_CanBeInstantiated()
    {
        var info = new SquadronInfo
        {
            squadronId  = Guid.NewGuid().ToString(),
            name        = "Alpha Wing",
            tag         = "AW",
            type        = SquadronType.Competitive,
            status      = SquadronStatus.Active,
            level       = 1,
            totalXP     = 0,
            memberCount = 1,
            maxMembers  = SquadronConfig.MaxMembers
        };

        Assert.IsNotNull(info);
        Assert.AreEqual("Alpha Wing", info.name);
        Assert.AreEqual("AW",         info.tag);
        Assert.AreEqual(1,            info.level);
    }

    [Test]
    public void SquadronMember_CanBeInstantiated()
    {
        var member = new SquadronMember
        {
            memberId     = "player_01",
            displayName  = "Ace Pilot",
            rank         = SquadronRank.Leader,
            joinedAt     = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            contributionXP = 500
        };

        Assert.IsNotNull(member);
        Assert.AreEqual("Ace Pilot",        member.displayName);
        Assert.AreEqual(SquadronRank.Leader, member.rank);
        Assert.AreEqual(500,                member.contributionXP);
    }

    [Test]
    public void SquadronMission_ObjectiveLists_InitialiseEmpty()
    {
        var mission = new SquadronMission();
        Assert.IsNotNull(mission.objectives);
        Assert.IsNotNull(mission.participantIds);
        Assert.IsNotNull(mission.completedObjectives);
        Assert.AreEqual(0, mission.objectives.Count);
        Assert.AreEqual(0, mission.participantIds.Count);
        Assert.AreEqual(0, mission.completedObjectives.Count);
    }

    [Test]
    public void SquadronEvent_RSVPMap_InitialisesEmpty()
    {
        var ev = new SquadronEvent();
        Assert.IsNotNull(ev.rsvpMap);
        Assert.AreEqual(0, ev.rsvpMap.Count);
    }

    [Test]
    public void SquadronBase_FacilityLevels_InitialisesEmpty()
    {
        var baseData = new SquadronBase();
        Assert.IsNotNull(baseData.facilityLevels);
        Assert.IsNotNull(baseData.decorations);
        Assert.IsNotNull(baseData.unlockedAreas);
    }

    [Test]
    public void SquadronInvite_StatusDefaultsPending()
    {
        var invite = new SquadronInvite { status = SquadronInviteStatus.Pending };
        Assert.AreEqual(SquadronInviteStatus.Pending, invite.status);
    }

    [Test]
    public void SquadronLeaderboardEntry_CanBeInstantiated()
    {
        var entry = new SquadronLeaderboardEntry
        {
            squadronId   = "sq_01",
            squadronName = "Alpha Wing",
            score        = 9999,
            rank         = 1,
            category     = SquadronLeaderboardCategory.TotalXP,
            period       = SquadronLeaderboardPeriod.AllTime
        };

        Assert.AreEqual(1,     entry.rank);
        Assert.AreEqual(9999L, entry.score);
    }

    [Test]
    public void SquadronChatMessage_CanBeInstantiated()
    {
        var msg = new SquadronChatMessage
        {
            messageId  = Guid.NewGuid().ToString(),
            senderName = "Pilot",
            text       = "Hello squadron!",
            isSystem   = false,
            isPinned   = false
        };

        Assert.AreEqual("Hello squadron!", msg.text);
        Assert.IsFalse(msg.isSystem);
        Assert.IsFalse(msg.isPinned);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PermissionMatrix
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void PermissionMatrix_Leader_HasAllPermissions()
    {
        foreach (SquadronPermission perm in Enum.GetValues(typeof(SquadronPermission)))
            Assert.IsTrue(PermissionMatrix.IsGranted(SquadronRank.Leader, perm),
                $"Leader should have permission: {perm}");
    }

    [Test]
    public void PermissionMatrix_Recruit_HasNoPermissions()
    {
        foreach (SquadronPermission perm in Enum.GetValues(typeof(SquadronPermission)))
            Assert.IsFalse(PermissionMatrix.IsGranted(SquadronRank.Recruit, perm),
                $"Recruit should NOT have permission: {perm}");
    }

    [Test]
    public void PermissionMatrix_Officer_CanInviteAndKick()
    {
        Assert.IsTrue(PermissionMatrix.IsGranted(SquadronRank.Officer, SquadronPermission.InviteMembers));
        Assert.IsTrue(PermissionMatrix.IsGranted(SquadronRank.Officer, SquadronPermission.KickMembers));
    }

    [Test]
    public void PermissionMatrix_Officer_CannotEditSettings()
    {
        Assert.IsFalse(PermissionMatrix.IsGranted(SquadronRank.Officer, SquadronPermission.EditSettings));
    }

    [Test]
    public void PermissionMatrix_Veteran_CanInviteAndStartMission()
    {
        Assert.IsTrue(PermissionMatrix.IsGranted(SquadronRank.Veteran, SquadronPermission.InviteMembers));
        Assert.IsTrue(PermissionMatrix.IsGranted(SquadronRank.Veteran, SquadronPermission.StartMission));
    }

    [Test]
    public void PermissionMatrix_Veteran_CannotKick()
    {
        Assert.IsFalse(PermissionMatrix.IsGranted(SquadronRank.Veteran, SquadronPermission.KickMembers));
    }

    [Test]
    public void PermissionMatrix_Member_CanOnlyStartMission()
    {
        Assert.IsTrue(PermissionMatrix.IsGranted(SquadronRank.Member, SquadronPermission.StartMission));
        Assert.IsFalse(PermissionMatrix.IsGranted(SquadronRank.Member, SquadronPermission.KickMembers));
        Assert.IsFalse(PermissionMatrix.IsGranted(SquadronRank.Member, SquadronPermission.InviteMembers));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SquadronConfig — facility upgrade cost logic
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void FacilityUpgradeCost_Level0To1_IsFirstEntry()
    {
        Assert.AreEqual(SquadronConfig.FacilityUpgradeCosts[0],
                        SquadronConfig.FacilityUpgradeCosts[0]);
        Assert.Greater(SquadronConfig.FacilityUpgradeCosts[0], 0);
    }

    [Test]
    public void FacilityMaxLevel_Is5()
    {
        Assert.AreEqual(5, SquadronConfig.FacilityMaxLevel);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SquadronMission — objective tracking
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SquadronMission_CompleteObjective_AddsToCompletedList()
    {
        var mission = new SquadronMission
        {
            missionId  = Guid.NewGuid().ToString(),
            objectives = new List<string> { "Fly 100 km", "Maintain formation", "Land safely" }
        };

        mission.completedObjectives.Add(0);
        Assert.AreEqual(1, mission.completedObjectives.Count);
        Assert.Contains(0, mission.completedObjectives);
    }

    [Test]
    public void SquadronMission_AllObjectivesCompleted_DetectedCorrectly()
    {
        var mission = new SquadronMission
        {
            missionId  = Guid.NewGuid().ToString(),
            objectives = new List<string> { "Obj A", "Obj B" }
        };

        mission.completedObjectives.Add(0);
        mission.completedObjectives.Add(1);

        bool allDone = mission.completedObjectives.Count >= mission.objectives.Count;
        Assert.IsTrue(allDone);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SquadronEvent — RSVP logic
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SquadronEvent_RSVPAttending_StoredCorrectly()
    {
        var ev = new SquadronEvent { eventId = Guid.NewGuid().ToString() };
        ev.rsvpMap["member_01"] = SquadronRSVP.Attending;
        ev.rsvpMap["member_02"] = SquadronRSVP.Maybe;

        Assert.AreEqual(SquadronRSVP.Attending, ev.rsvpMap["member_01"]);
        Assert.AreEqual(SquadronRSVP.Maybe,     ev.rsvpMap["member_02"]);
    }

    [Test]
    public void SquadronEvent_AttendingCount_IsCorrect()
    {
        var ev = new SquadronEvent { eventId = Guid.NewGuid().ToString() };
        ev.rsvpMap["m1"] = SquadronRSVP.Attending;
        ev.rsvpMap["m2"] = SquadronRSVP.Attending;
        ev.rsvpMap["m3"] = SquadronRSVP.NotAttending;

        int count = ev.rsvpMap.Values.Count(r => r == SquadronRSVP.Attending);
        Assert.AreEqual(2, count);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SquadronBase — facility level logic
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SquadronBase_FacilityLevel_DefaultsToZero()
    {
        var baseData = new SquadronBase();
        foreach (SquadronFacility facility in Enum.GetValues(typeof(SquadronFacility)))
        {
            int level = baseData.facilityLevels.TryGetValue(facility, out int l) ? l : 0;
            Assert.AreEqual(0, level, $"{facility} should default to level 0");
        }
    }

    [Test]
    public void SquadronBase_FacilityLevel_CanBeSet()
    {
        var baseData = new SquadronBase();
        baseData.facilityLevels[SquadronFacility.Hangar] = 3;
        Assert.AreEqual(3, baseData.facilityLevels[SquadronFacility.Hangar]);
    }

    [Test]
    public void SquadronBase_Decorations_CanBeAdded()
    {
        var baseData = new SquadronBase();
        baseData.decorations.Add("flag_01");
        baseData.decorations.Add("statue_02");
        Assert.AreEqual(2, baseData.decorations.Count);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SquadronLeaderboard — sorting
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SquadronLeaderboardEntries_SortByScore_Descending()
    {
        var entries = new List<SquadronLeaderboardEntry>
        {
            new SquadronLeaderboardEntry { squadronId = "a", score = 500 },
            new SquadronLeaderboardEntry { squadronId = "b", score = 1500 },
            new SquadronLeaderboardEntry { squadronId = "c", score = 1000 }
        };

        var sorted = entries.OrderByDescending(e => e.score).ToList();
        Assert.AreEqual("b", sorted[0].squadronId);
        Assert.AreEqual("c", sorted[1].squadronId);
        Assert.AreEqual("a", sorted[2].squadronId);
    }

    [Test]
    public void SquadronLeaderboardEntries_RankAssignment_IsOneBased()
    {
        var entries = new List<SquadronLeaderboardEntry>
        {
            new SquadronLeaderboardEntry { score = 100 },
            new SquadronLeaderboardEntry { score = 200 }
        };

        var sorted = entries.OrderByDescending(e => e.score).ToList();
        for (int i = 0; i < sorted.Count; i++)
            sorted[i].rank = i + 1;

        Assert.AreEqual(1, sorted[0].rank);
        Assert.AreEqual(2, sorted[1].rank);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SquadronChatMessage
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SquadronChatMessage_SystemMessage_FlaggedCorrectly()
    {
        var msg = new SquadronChatMessage
        {
            isSystem = true,
            isPinned = false,
            text     = "Member X has joined the squadron."
        };

        Assert.IsTrue(msg.isSystem);
        Assert.IsFalse(msg.isPinned);
    }

    [Test]
    public void SquadronChatMessage_Announcement_IsPinned()
    {
        var msg = new SquadronChatMessage
        {
            isSystem = false,
            isPinned = true,
            text     = "Tonight's training at 20:00 UTC!"
        };

        Assert.IsTrue(msg.isPinned);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SquadronInfo — XP and level
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SquadronInfo_XPAddition_IsCorrect()
    {
        var info = new SquadronInfo { totalXP = 0 };
        info.totalXP += 300;
        Assert.AreEqual(300, info.totalXP);
    }

    [Test]
    public void SquadronConfig_LevelXPRequirements_Level1IsZero()
    {
        Assert.AreEqual(0, SquadronConfig.LevelXPRequirements[1]);
    }

    [Test]
    public void SquadronConfig_LevelXPRequirements_AreNonDecreasing()
    {
        for (int lvl = 2; lvl <= 50; lvl++)
            Assert.GreaterOrEqual(SquadronConfig.LevelXPRequirements[lvl],
                                  SquadronConfig.LevelXPRequirements[lvl - 1],
                                  $"XP requirement at level {lvl} should be >= level {lvl - 1}");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Invite lifecycle
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SquadronInvite_ExpiryDate_IsInFuture()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var invite = new SquadronInvite
        {
            inviteId  = Guid.NewGuid().ToString(),
            sentAt    = now,
            expiresAt = DateTimeOffset.UtcNow.AddDays(SquadronConfig.InviteExpiryDays).ToUnixTimeSeconds(),
            status    = SquadronInviteStatus.Pending
        };

        Assert.Greater(invite.expiresAt, invite.sentAt);
        Assert.Greater(invite.expiresAt, now);
    }

    [Test]
    public void SquadronInvite_StatusTransition_WorksAsExpected()
    {
        var invite = new SquadronInvite { status = SquadronInviteStatus.Pending };
        Assert.AreEqual(SquadronInviteStatus.Pending, invite.status);

        invite.status = SquadronInviteStatus.Accepted;
        Assert.AreEqual(SquadronInviteStatus.Accepted, invite.status);
    }
}
