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

        /// <summary>
        /// Snap a given DateTime to the "best" 15-minute interval.
        /// </summary>
        /// <param name="now">The unaligned time</param>
        /// <returns>The time moved to the best nearby 15-minute interval.</returns>
        public static DateTime ToNearestTimeSlot(this DateTime now)
        {
            //
            // Select a starting slot for the reservation. If we're withing 5 minutes of the next slot, we'll
            // be allowed to check in early, so choose that. Otherwise, choose the slot that's already in progress.
            //
            int startMinute;
            int startHour = now.Hour;
            if (now.Minute >= 0 && now.Minute <= 10)
            {
                startMinute = 0;
            }
            else if (now.Minute >= 11 && now.Minute <= 25)
            {
                startMinute = 15;
            }
            else if (now.Minute >= 26 && now.Minute <= 40)
            {
                startMinute = 30;
            }
            else if (now.Minute >= 41 && now.Minute <= 55)
            {
                startMinute = 45;
            }
            else
            {
                startMinute = 0;
                startHour = now.Hour + 1;
            }

            var startTime = new DateTime(
                now.Year,
                now.Month,
                now.Day,
                startHour,
                startMinute,
                0,
                DateTimeKind.Unspecified);

            return startTime;
        }
    }
}