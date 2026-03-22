using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.TimeCapsule
{
    // ── CapsuleType ───────────────────────────────────────────────────────────────

    /// <summary>Broad category that describes the purpose or intent of a time capsule.</summary>
    public enum CapsuleType
    {
        /// <summary>A personal capsule sealed for the player's own future self.</summary>
        Personal,
        /// <summary>A capsule created as a gift and shared with a specific friend.</summary>
        Gift,
        /// <summary>A community-visible capsule open for anyone to discover.</summary>
        Community,
        /// <summary>A challenge capsule that requires completing a task to open.</summary>
        Challenge,
        /// <summary>A memorial capsule dedicated to a person, place, or event.</summary>
        Memorial,
        /// <summary>A capsule set to unlock on a special recurring anniversary date.</summary>
        Anniversary
    }

    // ── CapsuleState ─────────────────────────────────────────────────────────────

    /// <summary>Lifecycle state of a time capsule from creation through to opening.</summary>
    public enum CapsuleState
    {
        /// <summary>Capsule is being assembled — contents may still be changed.</summary>
        Draft,
        /// <summary>Contents have been finalised and the capsule has been sealed.</summary>
        Sealed,
        /// <summary>Capsule has been placed (buried) at a world location.</summary>
        Buried,
        /// <summary>Capsule is buried but its unlock condition has not yet been met.</summary>
        Locked,
        /// <summary>Unlock condition has been met — the capsule is ready to be opened.</summary>
        Unlockable,
        /// <summary>The player has opened the capsule and viewed its contents.</summary>
        Opened,
        /// <summary>The capsule's expiry date has passed without being opened.</summary>
        Expired
    }

    // ── CapsuleVisibility ─────────────────────────────────────────────────────────

    /// <summary>Controls who can discover and view a buried time capsule.</summary>
    public enum CapsuleVisibility
    {
        /// <summary>Only visible to the capsule's creator.</summary>
        Private,
        /// <summary>Visible to the creator's friend list only.</summary>
        FriendsOnly,
        /// <summary>Discoverable by any player in the world.</summary>
        Public,
        /// <summary>Hidden unless the finder provides the correct location hint or clue.</summary>
        Secret
    }

    // ── ContentType ───────────────────────────────────────────────────────────────

    /// <summary>Classifies the kind of media or data stored in a <see cref="CapsuleContent"/> item.</summary>
    public enum ContentType
    {
        /// <summary>A photograph taken with <c>ScreenshotManager</c> or <c>PhotoCaptureManager</c>.</summary>
        Photo,
        /// <summary>A flight journal entry from <c>JournalManager</c>.</summary>
        JournalEntry,
        /// <summary>A recorded flight replay from <c>ReplayFileManager</c>.</summary>
        FlightReplay,
        /// <summary>A short text note written by the player.</summary>
        TextNote,
        /// <summary>A recorded voice memo audio clip.</summary>
        VoiceMemo,
        /// <summary>A music track or playlist reference from <c>MusicPlayerManager</c>.</summary>
        Music,
        /// <summary>A custom flight route from <c>RoutePlannerManager</c>.</summary>
        Route,
        /// <summary>An achievement entry from <c>AchievementManager</c>.</summary>
        Achievement,
        /// <summary>A set of GPS coordinates (latitude, longitude, altitude).</summary>
        Coordinates,
        /// <summary>Arbitrary custom data payload (JSON or binary).</summary>
        CustomData
    }

    // ── UnlockCondition ───────────────────────────────────────────────────────────

    /// <summary>Specifies what must happen before a <see cref="CapsuleState.Locked"/> capsule can be opened.</summary>
    public enum UnlockCondition
    {
        /// <summary>Unlocks automatically on or after a specified <see cref="TimeCapsuleRecord.unlockDate"/>.</summary>
        DateBased,
        /// <summary>Unlocks when the player physically revisits the burial location.</summary>
        LocationRevisit,
        /// <summary>Unlocks when a specific achievement is earned.</summary>
        AchievementUnlock,
        /// <summary>Unlocks after reaching a cumulative flight milestone (e.g. total hours, distance).</summary>
        FlightMilestone,
        /// <summary>Unlocks when a designated friend locates and opens the capsule first.</summary>
        FriendFound,
        /// <summary>Unlocks only when the correct password is supplied.</summary>
        PasswordProtected,
        /// <summary>No unlock barrier — capsule opens as soon as it is found.</summary>
        Immediate
    }

    // ── CapsuleContent ────────────────────────────────────────────────────────────

    /// <summary>
    /// Represents a single piece of media or data attached to a <see cref="TimeCapsuleRecord"/>.
    /// A capsule can hold multiple <see cref="CapsuleContent"/> items of mixed types.
    /// </summary>
    [Serializable]
    public class CapsuleContent
    {
        /// <summary>Globally unique identifier (GUID) for this content item.</summary>
        public string contentId;

        /// <summary>The kind of data this item holds.</summary>
        public ContentType contentType;

        /// <summary>User-assigned display label for this content item.</summary>
        public string title;

        /// <summary>
        /// Absolute or relative file path to the actual content asset
        /// (e.g. a photo PNG, a replay <c>.swefr</c> file, or a voice memo <c>.ogg</c>).
        /// </summary>
        public string dataPath;

        /// <summary>
        /// Optional path to a small preview image shown before the content is loaded.
        /// May be empty for non-visual types such as <see cref="ContentType.TextNote"/>.
        /// </summary>
        public string thumbnailPath;

        /// <summary>
        /// Inline text for <see cref="ContentType.TextNote"/> items, or a caption/description
        /// for photo and other media types. Maximum 1 000 characters.
        /// </summary>
        public string textContent;

        /// <summary>
        /// Arbitrary key/value pairs for storing type-specific metadata
        /// (e.g. photo EXIF data, achievement ID, track BPM).
        /// Not serialized to JSON automatically — callers must handle this field manually
        /// if persistence is required.
        /// </summary>
        [NonSerialized]
        public Dictionary<string, string> metadata;
    }

    // ── TimeCapsuleRecord ─────────────────────────────────────────────────────────

    /// <summary>
    /// Complete persistent record for a single time capsule.
    /// Serialized to <c>Application.persistentDataPath/time_capsules/{capsuleId}.json</c>
    /// by <c>TimeCapsuleManager</c>.
    /// </summary>
    [Serializable]
    public class TimeCapsuleRecord
    {
        [Header("Identity")]
        /// <summary>Globally unique identifier (GUID) for this capsule.</summary>
        public string capsuleId;

        /// <summary>Display title chosen by the creator.</summary>
        public string title;

        /// <summary>Optional longer description or dedication message.</summary>
        public string description;

        /// <summary>Broad category of this capsule.</summary>
        public CapsuleType capsuleType;

        /// <summary>Current lifecycle state.</summary>
        public CapsuleState state;

        /// <summary>Who can discover this capsule in the world.</summary>
        public CapsuleVisibility visibility;

        [Header("Location")]
        /// <summary>WGS-84 latitude in decimal degrees where the capsule is buried.</summary>
        public double latitude;

        /// <summary>WGS-84 longitude in decimal degrees where the capsule is buried.</summary>
        public double longitude;

        /// <summary>Altitude in metres above sea level at which the capsule was buried.</summary>
        public float altitudeMeters;

        /// <summary>
        /// Name of the nearest landmark at the burial site (populated from
        /// <c>LandmarkDatabase</c> at burial time). Used for display in the UI.
        /// </summary>
        public string nearestLandmarkName;

        /// <summary>
        /// Optional hint text shown to players with <see cref="CapsuleVisibility.Secret"/>
        /// capsules, describing how to find the burial site.
        /// </summary>
        public string locationHint;

        [Header("Dates")]
        /// <summary>ISO-8601 UTC string for when the capsule was first created.</summary>
        public string createdDate;

        /// <summary>ISO-8601 UTC string for when the capsule was sealed (contents finalised).</summary>
        public string sealedDate;

        /// <summary>ISO-8601 UTC string for when the capsule was buried in the world.</summary>
        public string buriedDate;

        /// <summary>
        /// ISO-8601 UTC string for the target unlock date.
        /// Relevant when <see cref="unlockCondition"/> is <see cref="UnlockCondition.DateBased"/>
        /// or <see cref="UnlockCondition.Immediate"/>.
        /// </summary>
        public string unlockDate;

        /// <summary>ISO-8601 UTC string for when the capsule was actually opened. Empty if not yet opened.</summary>
        public string openedDate;

        /// <summary>
        /// Optional ISO-8601 UTC expiry date. If set and the capsule has not been opened
        /// by this date, its state transitions to <see cref="CapsuleState.Expired"/>.
        /// </summary>
        public string expiryDate;

        [Header("Unlock")]
        /// <summary>The condition that must be satisfied before this capsule can be opened.</summary>
        public UnlockCondition unlockCondition;

        /// <summary>
        /// Supplementary data for the unlock condition, e.g. the achievement ID required for
        /// <see cref="UnlockCondition.AchievementUnlock"/>, the milestone value for
        /// <see cref="UnlockCondition.FlightMilestone"/>, or a hashed password for
        /// <see cref="UnlockCondition.PasswordProtected"/>.
        /// </summary>
        public string unlockConditionData;

        [Header("Ownership")]
        /// <summary>Platform user ID of the player who created the capsule.</summary>
        public string creatorUserId;

        /// <summary>Display name of the creator.</summary>
        public string creatorDisplayName;

        /// <summary>
        /// For <see cref="CapsuleType.Gift"/> capsules, the intended recipient's platform user ID.
        /// </summary>
        public string recipientUserId;

        /// <summary>Display name of the intended recipient (if applicable).</summary>
        public string recipientDisplayName;

        [Header("Contents")]
        /// <summary>
        /// Ordered list of content items attached to this capsule.
        /// A capsule may hold up to 10 content items.
        /// </summary>
        public List<CapsuleContent> contents = new List<CapsuleContent>();

        [Header("Interaction")]
        /// <summary>Total number of times this capsule has been discovered by any player.</summary>
        public int discoveryCount;

        /// <summary>Total number of times this capsule has been opened by any player.</summary>
        public int openCount;

        /// <summary>Platform user IDs of players who have discovered this capsule.</summary>
        public List<string> discoveredByUserIds = new List<string>();

        /// <summary>Whether the local player has marked this capsule as a favourite.</summary>
        public bool isFavorite;

        [Header("Appearance")]
        /// <summary>
        /// Optional path to a cover image shown in the capsule list UI
        /// (defaults to the first photo content item's thumbnail if not set).
        /// </summary>
        public string coverImagePath;

        /// <summary>User-defined tags (e.g. "sunset", "Antarctica", "family").</summary>
        public string[] tags = Array.Empty<string>();
    }

    // ── TimeCapsuleFilter ─────────────────────────────────────────────────────────

    /// <summary>
    /// Criteria used to filter and sort a collection of <see cref="TimeCapsuleRecord"/> objects.
    /// </summary>
    [Serializable]
    public class TimeCapsuleFilter
    {
        [Header("Type & State")]
        /// <summary>
        /// When set, only capsules of this type are returned. A value of <c>null</c>
        /// means no type filter is applied.
        /// </summary>
        public CapsuleType? typeFilter;

        /// <summary>
        /// When set, only capsules in this state are returned. A value of <c>null</c>
        /// means no state filter is applied.
        /// </summary>
        public CapsuleState? stateFilter;

        /// <summary>
        /// When set, only capsules with this visibility level are returned.
        /// A value of <c>null</c> means no visibility filter is applied.
        /// </summary>
        public CapsuleVisibility? visibilityFilter;

        [Header("Date Range")]
        /// <summary>ISO-8601 lower bound on creation date (inclusive). Empty = no lower bound.</summary>
        public string createdFrom;

        /// <summary>ISO-8601 upper bound on creation date (inclusive). Empty = no upper bound.</summary>
        public string createdTo;

        [Header("Content")]
        /// <summary>Only return capsules that contain at least one item of this type. <c>null</c> = any.</summary>
        public ContentType? contentTypeFilter;

        /// <summary>Only return capsules that contain at least one of these tags. Empty = no tag filter.</summary>
        public string[] tagsFilter = Array.Empty<string>();

        /// <summary>When true, only return capsules the local player has marked as a favourite.</summary>
        public bool favoritesOnly;

        [Header("Search")]
        /// <summary>
        /// Free-text query searched across <c>title</c>, <c>description</c>,
        /// <c>nearestLandmarkName</c>, <c>tags</c>, and <c>creatorDisplayName</c>.
        /// Empty = no text filter.
        /// </summary>
        public string searchQuery;

        [Header("Sorting")]
        /// <summary>Field to sort results by.</summary>
        public CapsuleSortBy sortBy = CapsuleSortBy.CreatedDate;

        /// <summary>When true, results are returned newest/largest first.</summary>
        public bool sortDescending = true;
    }

    // ── CapsuleSortBy ─────────────────────────────────────────────────────────────

    /// <summary>Fields by which a list of <see cref="TimeCapsuleRecord"/> objects can be sorted.</summary>
    public enum CapsuleSortBy
    {
        /// <summary>Sort by capsule creation date.</summary>
        CreatedDate,
        /// <summary>Sort by the target unlock date.</summary>
        UnlockDate,
        /// <summary>Sort alphabetically by capsule title.</summary>
        Title,
        /// <summary>Sort by number of times the capsule has been discovered.</summary>
        DiscoveryCount,
        /// <summary>Sort by capsule type.</summary>
        Type,
        /// <summary>Sort by capsule state.</summary>
        State
    }

    // ── TimeCapsuleStatistics ─────────────────────────────────────────────────────

    /// <summary>
    /// Aggregate statistics computed from all <see cref="TimeCapsuleRecord"/> objects
    /// belonging to the local player. Generated on demand by <c>TimeCapsuleManager</c>.
    /// </summary>
    [Serializable]
    public class TimeCapsuleStatistics
    {
        [Header("Totals")]
        /// <summary>Total number of capsules created by the local player.</summary>
        public int totalCreated;

        /// <summary>Total number of capsules currently in the world (state = Buried, Locked, or Unlockable).</summary>
        public int totalBuried;

        /// <summary>Total number of capsules the player has opened (their own or others').</summary>
        public int totalOpened;

        /// <summary>Total number of capsules discovered by the local player (including unopened ones).</summary>
        public int totalDiscovered;

        [Header("Type Breakdown")]
        /// <summary>Number of capsules of each <see cref="CapsuleType"/>.</summary>
        public int personalCount;
        /// <summary>Number of gift capsules created.</summary>
        public int giftCount;
        /// <summary>Number of community capsules created.</summary>
        public int communityCount;
        /// <summary>Number of challenge capsules created.</summary>
        public int challengeCount;
        /// <summary>Number of memorial capsules created.</summary>
        public int memorialCount;
        /// <summary>Number of anniversary capsules created.</summary>
        public int anniversaryCount;

        [Header("Content")]
        /// <summary>Total number of individual content items stored across all capsules.</summary>
        public int totalContentItems;

        /// <summary>Total number of photos attached across all capsules.</summary>
        public int totalPhotos;

        /// <summary>Total number of voice memos attached across all capsules.</summary>
        public int totalVoiceMemos;

        [Header("Records")]
        /// <summary>Title of the capsule that has been discovered by the most players.</summary>
        public string mostDiscoveredCapsuleTitle;

        /// <summary>How many unique players have discovered the most popular capsule.</summary>
        public int mostDiscoveredCount;

        /// <summary>
        /// ISO-8601 UTC string of the next upcoming unlock date among the player's locked capsules.
        /// Empty if no locked capsules have a date-based unlock condition.
        /// </summary>
        public string nextUnlockDate;
    }
}
