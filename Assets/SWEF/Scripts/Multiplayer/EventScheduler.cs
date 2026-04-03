using System;
using System.Collections.Generic;

namespace SWEF.Multiplayer
{
    /// <summary>
    /// Static utility that determines which cross-session events should be active
    /// based on the current UTC time, day of week, and season.
    /// Supports recurring event templates so the schedule regenerates automatically.
    /// </summary>
    public static class EventScheduler
    {
        #region Season Helpers
        /// <summary>
        /// Returns the meteorological season for a given date (Northern Hemisphere).
        /// </summary>
        /// <param name="date">Date to evaluate.</param>
        /// <returns>Season name: Spring, Summer, Autumn, or Winter.</returns>
        public static string GetSeason(DateTime date)
        {
            int month = date.Month;
            if (month >= 3 && month <= 5) return "Spring";
            if (month >= 6 && month <= 8) return "Summer";
            if (month >= 9 && month <= 11) return "Autumn";
            return "Winter";
        }

        /// <summary>
        /// Returns true if the given date falls on a weekend (Saturday or Sunday UTC).
        /// </summary>
        public static bool IsWeekend(DateTime date) =>
            date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
        #endregion

        #region Template Catalogue
        /// <summary>
        /// Returns all event templates whose recurrence rule matches the supplied time.
        /// Each template is returned as a fresh <see cref="CrossSessionEventData"/> instance.
        /// </summary>
        /// <param name="now">The reference time (UTC).</param>
        /// <returns>List of events that should currently be active.</returns>
        public static List<CrossSessionEventData> GetActiveEventTemplates(DateTime now)
        {
            var result = new List<CrossSessionEventData>();

            // Daily rotation — always active
            result.Add(BuildDailySpeedRun(now));

            // Weekend-only formation challenge
            if (IsWeekend(now))
                result.Add(BuildWeekendFormationChallenge(now));

            // Weekly exploration rally — active on Fridays through Sundays
            if (now.DayOfWeek == DayOfWeek.Friday ||
                now.DayOfWeek == DayOfWeek.Saturday ||
                now.DayOfWeek == DayOfWeek.Sunday)
                result.Add(BuildWeeklyExplorationRally(now));

            // Seasonal festival — active during first week of each season start month
            if (now.Day <= 7 && (now.Month == 3 || now.Month == 6 ||
                                  now.Month == 9 || now.Month == 12))
                result.Add(BuildSeasonalFestival(now));

            return result;
        }
        #endregion

        #region Template Builders
        private static CrossSessionEventData BuildDailySpeedRun(DateTime now)
        {
            DateTime dayStart = now.Date;
            DateTime dayEnd = dayStart.AddDays(1).AddTicks(-1);
            return new CrossSessionEventData
            {
                eventId = $"daily_speedrun_{now:yyyyMMdd}",
                eventType = CrossSessionEventType.SpeedRun,
                title = "Daily Speed Run Challenge",
                description = "Beat the daily best time over the featured course. Top 3 earn bonus XP.",
                startTime = dayStart.ToString("o"),
                endTime = dayEnd.ToString("o"),
                locationLatitude = 35.6762,
                locationLongitude = 139.6503,
                radius = 50f,
                rewards = "{\"xp\":500,\"badge\":\"speed_demon\"}",
                participantCount = 0,
                isActive = true
            };
        }

        private static CrossSessionEventData BuildWeekendFormationChallenge(DateTime now)
        {
            // Anchor to the most recent Friday (days-since-Friday formula, handles all DayOfWeek values)
            int daysSinceFriday = ((int)now.DayOfWeek - (int)DayOfWeek.Friday + 7) % 7;
            DateTime friday = now.Date.AddDays(-daysSinceFriday);
            DateTime start = friday.AddHours(18);
            DateTime end = friday.AddDays(2).AddHours(23).AddMinutes(59);
            return new CrossSessionEventData
            {
                eventId = $"weekend_formation_{friday:yyyyMMdd}",
                eventType = CrossSessionEventType.FormationChallenge,
                title = "Weekend Formation Challenge",
                description = "Fly in tight formation with 3+ friends for 10 minutes to earn the Formation Master badge.",
                startTime = start.ToString("o"),
                endTime = end.ToString("o"),
                locationLatitude = 48.8566,
                locationLongitude = 2.3522,
                radius = 100f,
                rewards = "{\"xp\":750,\"badge\":\"formation_master\"}",
                participantCount = 0,
                isActive = true
            };
        }

        private static CrossSessionEventData BuildWeeklyExplorationRally(DateTime now)
        {
            // Anchor to the most recent Friday
            int daysSinceFriday = ((int)now.DayOfWeek - (int)DayOfWeek.Friday + 7) % 7;
            DateTime friday = now.Date.AddDays(-daysSinceFriday);
            DateTime start = friday;
            DateTime end = friday.AddDays(3).AddHours(23).AddMinutes(59);
            return new CrossSessionEventData
            {
                eventId = $"weekly_rally_{friday:yyyyMMdd}",
                eventType = CrossSessionEventType.ExplorationRally,
                title = "Weekly Exploration Rally",
                description = "Discover and visit 5 featured shared waypoints before Sunday midnight.",
                startTime = start.ToString("o"),
                endTime = end.ToString("o"),
                locationLatitude = 0,
                locationLongitude = 0,
                radius = 20000f,
                rewards = "{\"xp\":1000,\"badge\":\"explorer\"}",
                participantCount = 0,
                isActive = true
            };
        }

        private static CrossSessionEventData BuildSeasonalFestival(DateTime now)
        {
            string season = GetSeason(now);
            DateTime monthStart = new DateTime(now.Year, now.Month, 1);
            DateTime festivalEnd = monthStart.AddDays(7);
            return new CrossSessionEventData
            {
                eventId = $"seasonal_festival_{now:yyyyMM}",
                eventType = CrossSessionEventType.SeasonalFestival,
                title = $"{season} Sky Festival",
                description = $"Celebrate the start of {season}! Complete special {season.ToLower()} challenges and earn unique cosmetics.",
                startTime = monthStart.ToString("o"),
                endTime = festivalEnd.ToString("o"),
                locationLatitude = 51.5074,
                locationLongitude = -0.1278,
                radius = 500f,
                rewards = $"{{\"xp\":2000,\"badge\":\"{season.ToLower()}_pilot\",\"cosmetic\":\"season_{season.ToLower()}_livery\"}}",
                participantCount = 0,
                isActive = true
            };
        }
        #endregion
    }
}
