using System;

namespace BoatTracker.Bot.Utils
{
    public static class DateTimeExtensions
    {
        public static bool HasDate(this DateTime dateTime)
        {
            return dateTime.Year != DateTime.MinValue.Year ||
                dateTime.Month != DateTime.MinValue.Month ||
                dateTime.Day != DateTime.MinValue.Day;
        }

        public static bool HasTime(this DateTime dateTime)
        {
            return dateTime.Hour != DateTime.MinValue.Hour ||
                dateTime.Minute != DateTime.MinValue.Minute ||
                dateTime.Second != DateTime.MinValue.Second;
        }
    }
}