using System.Globalization;

namespace EventService.Application.Utilities
{
    public static class DateTimeUtility
    {
        /// <summary>
        /// Converts local date to Coordinated Universal Time (UTC) date
        /// </summary>
        /// <param name="localDate"></param>
        /// <param name="timeZone"></param>
        /// <returns></returns>
        public static DateTime ToUtcDate(this DateTime localDate, string timeZone)
        {
            // Find the corresponding Windows time zone by the IANA time zone name
            TimeZoneInfo timeZoneInfo = TimeZoneConverter.TZConvert.GetTimeZoneInfo(timeZone);
            // TODO: revisit this specify kind thing
            localDate = DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified);
            // Convert the local time to UTC by using the time zone info
            return TimeZoneInfo.ConvertTimeToUtc(localDate, timeZoneInfo);
        }

        /// <summary>
        /// Converts Coordinated Universal Time (UTC) date to local date
        /// </summary>
        /// <param name="utcDate"></param>
        /// <param name="timeZone"></param>
        /// <returns></returns>
        public static DateTime ToLocalDate(this DateTime utcDate, string timeZone)
        {
            // Find the corresponding Windows time zone by the IANA time zone name
            TimeZoneInfo timeZoneInfo = TimeZoneConverter.TZConvert.GetTimeZoneInfo(timeZone);
            // TODO: revisit this specify kind thing
            utcDate = DateTime.SpecifyKind(utcDate, DateTimeKind.Utc);
            // Convert the local time to UTC by using the time zone info
            return TimeZoneInfo.ConvertTimeFromUtc(utcDate, timeZoneInfo);
        }
    }
}
