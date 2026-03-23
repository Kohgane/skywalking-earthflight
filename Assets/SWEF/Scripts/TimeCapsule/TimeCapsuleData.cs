using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.TimeCapsule
{
    // ── TimeCapsuleType ───────────────────────────────────────────────────────────

    /// <summary>Broad category describing the nature of a time capsule.</summary>
    public enum TimeCapsuleType
    {
        /// <summary>A snapshot of a memorable in-flight moment.</summary>
        FlightMoment,
        /// <summary>A capsule marking a personal or game milestone.</summary>
        Milestone,
        /// <summary>A capsule created upon discovering a hidden gem or landmark.</summary>
        Discovery,
        /// <summary>A free-form personal note or reflection.</summary>
        PersonalNote,
        /// <summary>A capsule intended to be shared with or discovered by other players.</summary>
        SharedMemory
    }

    // ── TimeCapsuleStatus ─────────────────────────────────────────────────────────

    /// <summary>Lifecycle status of a <see cref="TimeCapsule"/>.</summary>
    public enum TimeCapsuleStatus
    {
        /// <summary>Capsule has been sealed and is waiting to be opened after its delay.</summary>
        Sealed,
        /// <summary>Capsule has been opened by the player.</summary>
        Opened,
        /// <summary>Capsule's open date has passed without being opened.</summary>
        Expired,
        /// <summary>Capsule has been shared with other players.</summary>
        Shared
    }

    // ── CapsuleLocation ───────────────────────────────────────────────────────────

    /// <summary>Geographic location where a time capsule was created.</summary>
    [Serializable]
    public class CapsuleLocation
    {
        /// <summary>WGS-84 latitude in decimal degrees.</summary>
        public float latitude;

        /// <summary>WGS-84 longitude in decimal degrees.</summary>
        public float longitude;

        /// <summary>Altitude in metres above sea level.</summary>
        public float altitude;

        /// <summary>Human-readable name of this location (e.g. "Over the Alps").</summary>
        public string locationName;
    }

    // ── CapsuleWeatherSnapshot ────────────────────────────────────────────────────

    /// <summary>Weather conditions captured at the moment a time capsule was created.</summary>
    [Serializable]
    public class CapsuleWeatherSnapshot
    {
        /// <summary>Descriptive weather condition (e.g. "Clear", "Overcast", "Stormy").</summary>
        public string weatherCondition;

        /// <summary>Ambient temperature in degrees Celsius.</summary>
        public float temperature;

        /// <summary>Wind speed in metres per second.</summary>
        public float windSpeed;

        /// <summary>Horizontal visibility in kilometres.</summary>
        public float visibility;

        /// <summary>Cloud cover fraction from 0 (clear) to 1 (fully overcast).</summary>
        public float cloudCover;
    }

    // ── CapsuleFlightSnapshot ─────────────────────────────────────────────────────

    /// <summary>Flight state captured at the moment a time capsule was created.</summary>
    [Serializable]
    public class CapsuleFlightSnapshot
    {
        /// <summary>Identifier of the aircraft in use.</summary>
        public string aircraftId;

        /// <summary>Current airspeed in km/h.</summary>
        public float speed;

        /// <summary>Magnetic heading in degrees (0–360).</summary>
        public float heading;

        /// <summary>Total elapsed flight time in seconds at capture.</summary>
        public float flightDuration;

        /// <summary>Total distance traveled during the flight in metres.</summary>
        public float distanceTraveled;
    }

    // ── TimeCapsule ───────────────────────────────────────────────────────────────

    /// <summary>
    /// A complete time capsule record — a snapshot of a memorable flight moment
    /// that can be revisited in the future.
    /// </summary>
    [Serializable]
    public class TimeCapsule
    {
        [Header("Identity")]
        /// <summary>Globally unique identifier (GUID) for this capsule.</summary>
        public string capsuleId;

        /// <summary>Short display title chosen by the player.</summary>
        public string title;

        /// <summary>Optional longer description or reflection message.</summary>
        public string description;

        /// <summary>Broad category of this capsule.</summary>
        public TimeCapsuleType type;

        /// <summary>Current lifecycle status.</summary>
        public TimeCapsuleStatus status;

        [Header("Dates")]
        /// <summary>ISO-8601 UTC string for when the capsule was created and sealed.</summary>
        public string createdAt;

        /// <summary>
        /// ISO-8601 UTC string for the earliest date on which this capsule may be opened.
        /// The capsule is ready when <c>DateTime.UtcNow &gt;= openAfter</c>.
        /// </summary>
        public string openAfter;

        [Header("Context")]
        /// <summary>Geographic location where the capsule was created.</summary>
        public CapsuleLocation location;

        /// <summary>Weather conditions at the time of creation.</summary>
        public CapsuleWeatherSnapshot weather;

        /// <summary>Flight state at the time of creation.</summary>
        public CapsuleFlightSnapshot flight;

        [Header("Content")]
        /// <summary>Absolute path to an optional screenshot associated with this capsule.</summary>
        public string screenshotPath;

        /// <summary>User-defined tags for filtering and search (e.g. "sunset", "Alps").</summary>
        public List<string> tags = new List<string>();

        /// <summary>Free-form personal note written by the player.</summary>
        public string personalNote;

        /// <summary>Whether this capsule was created automatically by <see cref="TimeCapsuleAutoCapture"/>.</summary>
        public bool isAutoGenerated;

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns <c>true</c> when the current UTC time is on or after <see cref="openAfter"/>,
        /// meaning this capsule is eligible to be opened.
        /// </summary>
        public bool IsReadyToOpen()
        {
            if (string.IsNullOrEmpty(openAfter)) return true;
            if (!DateTime.TryParse(openAfter, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime dt))
                return true;
            return DateTime.UtcNow >= dt;
        }

        /// <summary>
        /// Returns a human-readable string describing how long ago this capsule was created,
        /// e.g. "just now", "3 hours ago", "2 days ago", "1 month ago".
        /// </summary>
        public string FormattedAge()
        {
            if (string.IsNullOrEmpty(createdAt)) return string.Empty;
            if (!DateTime.TryParse(createdAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime created))
                return string.Empty;

            TimeSpan age = DateTime.UtcNow - created;
            if (age.TotalSeconds < 60) return "just now";
            if (age.TotalMinutes < 60) return $"{(int)age.TotalMinutes} minutes ago";
            if (age.TotalHours < 24)   return $"{(int)age.TotalHours} hours ago";
            if (age.TotalDays < 30)    return $"{(int)age.TotalDays} days ago";
            if (age.TotalDays < 365)   return $"{(int)(age.TotalDays / 30)} months ago";
            return $"{(int)(age.TotalDays / 365)} years ago";
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Legacy Phase 50 types kept below for backward compatibility.
    // ─────────────────────────────────────────────────────────────────────────────

    // ── CapsuleType (Phase 50 legacy) ─────────────────────────────────────────────

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
