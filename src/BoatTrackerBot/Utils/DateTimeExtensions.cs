using System;

namespace BoatTracker.Bot.Utils
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Determines if the DateTime has a non-default date component.
        /// </summary>
        /// <param name="dateTime">The DateTime object to test.</param>
        /// <returns>True if the date component has been set.</returns>
        public static bool HasDate(this DateTime dateTime)
        {
            return dateTime.Year != DateTime.MinValue.Year ||
                dateTime.Month != DateTime.MinValue.Month ||
                dateTime.Day != DateTime.MinValue.Day;
        }

        /// <summary>
        /// Determines if the DateTime has a non-default time component.
        /// </summary>
        /// <param name="dateTime">The DateTime object to test.</param>
        /// <returns>True if the time component has been set.</returns>
        public static bool HasTime(this DateTime dateTime)
        {
            return dateTime.Hour != DateTime.MinValue.Hour ||
                dateTime.Minute != DateTime.MinValue.Minute ||
                dateTime.Second != DateTime.MinValue.Second;
        }
    }
}