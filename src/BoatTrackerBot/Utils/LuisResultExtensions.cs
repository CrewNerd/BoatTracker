using System;
using System.Linq;
using System.Text.RegularExpressions;

using BoatTracker.Bot.DataObjects;

using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;

namespace BoatTracker.Bot.Utils
{
    /// <summary>
    /// Extension methods for extracting information from LUIS results.
    /// </summary>
    public static class LuisResultExtensions
    {
        public const string EntityBoatName = "boatName";
        public const string EntityBoatClass = "boatClass";
        public const string EntityRowerName = "rowerName";
        private const string EntityStart = "DateTime::startDate";
        private const string EntityDuration = "DateTime::duration";
        private const string EntityBuiltinDate = "builtin.datetime.date";
        private const string EntityBuiltinTime = "builtin.datetime.time";
        private const string EntityBuiltinDuration = "builtin.datetime.duration";

        private static readonly string[] SingleClassNames = { "single", "singles", "1x" };
        private static readonly string[] DoubleClassNames = { "double", "doubles", "2x" };

        /// <summary>
        /// Look for entities that were originally #NN and restore them.
        /// </summary>
        /// <param name="result">The result to be repaired</param>
        /// <returns>The repaired result</returns>
        public static LuisResult FixEntities(this LuisResult result)
        {
            foreach (var entity in result.Entities)
            {
                entity.Entity = Regex.Replace(entity.Entity, "xyzzy(?<digits>[0-9]*)$", "#${digits}");
            }

            return result;
        }

        public static string BoatName(this LuisResult result)
        {
            var nameEntities = result.Entities.Where(e => e.Type == EntityBoatName).Select(e => e.Entity);
            return string.Join(" ", nameEntities);
        }

        public static bool ContainsUserNameEntity(this LuisResult result)
        {
            return result.Entities.Any(e => e.Type == EntityRowerName);
        }

        public static string UserName(this LuisResult result)
        {
            var userEntities = result.Entities.Where(e => e.Type == EntityRowerName).Select(e => e.Entity);
            return string.Join(" ", userEntities);
        }

        public static int? BoatCapacity(this LuisResult result)
        {
            var classEntities = result.Entities.Where(e => e.Type == EntityBoatClass).Select(e => e.Entity);

            var singles = SingleClassNames.Intersect(classEntities).Count() > 0;
            var doubles = DoubleClassNames.Intersect(classEntities).Count() > 0;

            if (singles && !doubles)
            {
                return 1;
            }
            else if (doubles && !singles)
            {
                return 2;
            }
            else
            {
                return null;
            }
        }

        public static DateTime? FindStartDate(this LuisResult result, UserState userState)
        {
            //
            // We special-case "now" since it most likely overrules anything else we might see.
            //
            if (result.IsStartTimeNow())
            {
                var localNow = userState.ConvertToLocalTime(DateTime.UtcNow);

                return localNow.Date;
            }

            TimeSpan maxDaysInFuture = TimeSpan.FromDays(30);   // Limit how far in the future we can recognize

            EntityRecommendation builtinDate = null;
            result.TryFindEntity(EntityBuiltinDate, out builtinDate);

            var parser = new Chronic.Parser(
                new Chronic.Options
                {
                    Context = Chronic.Pointer.Type.Future,
                    Clock = () =>
                    {
                        var utcNow = DateTime.UtcNow;
                        var tzOffset = userState.LocalOffsetForDate(utcNow);

                        return new DateTime((utcNow + tzOffset).Ticks, DateTimeKind.Unspecified);
                    }
                });

            if (builtinDate != null && builtinDate.Resolution.ContainsKey("date"))
            {
                //
                // Give DateTime a crack at parsing it first. This handles cases like MM/DD which Chronic
                // can't handle, for some reason.
                //
                DateTime date;

                if (DateTime.TryParse(builtinDate.Entity, out date))
                {
                    date = date.Date;
                    // Only accept dates in the reasonably near future.
                    if (date >= DateTime.Now.Date && date <= DateTime.Now.Date + maxDaysInFuture)
                    {
                        return date;
                    }
                }

                var span = parser.Parse(builtinDate.Entity);

                if (span != null)
                {
                    var when = span.Start ?? span.End;
                    return when.Value.Date;
                }
            }

            foreach (var startDate in result.Entities.Where(e => e.Type == EntityStart))
            {
                var span = parser.Parse(startDate.Entity);

                if (span != null)
                {
                    var when = span.Start ?? span.End;

                    // If the user gives a time without a date, it will look like today's
                    // date with the specified time. This is okay if the time is in the
                    // future. If the time is in the past, then don't let the date default
                    // to today.
                    if (when.Value.HasDate() && when.Value > userState.LocalTime())
                    {
                        return when.Value.Date;
                    }
                }
            }

            return null;
        }

        public static DateTime? FindStartTime(this LuisResult result, UserState userState)
        {
            //
            // We special-case "now" since it most likely overrules anything else we might see.
            // We snap the time to the "best" nearby time-slot (15-minute intervals).
            //
            if (result.IsStartTimeNow())
            {
                return userState.LocalTime().ToNearestTimeSlot();
            }

            EntityRecommendation builtinTime = null;
            result.TryFindEntity(EntityBuiltinTime, out builtinTime);

            var parser = new Chronic.Parser(
                new Chronic.Options
                {
                    Context = Chronic.Pointer.Type.Future,
                    Clock = () =>
                    {
                        var utcNow = DateTime.UtcNow;
                        var tzOffset = userState.LocalOffsetForDate(utcNow);

                        return new DateTime((utcNow + tzOffset).Ticks, DateTimeKind.Unspecified);
                    }
                });

            if (builtinTime != null && builtinTime.Resolution.ContainsKey("time"))
            {
                var span = parser.Parse(builtinTime.Entity);

                if (span != null)
                {
                    var when = span.Start ?? span.End;

                    if (when.Value.HasTime())
                    {
                        return when.Value;
                    }
                }
            }

            foreach (var startDate in result.Entities.Where(e => e.Type == EntityStart))
            {
                var span = parser.Parse(startDate.Entity.Replace("at ", string.Empty));

                if (span != null)
                {
                    var when = span.Start ?? span.End;

                    if (when.Value.HasTime())
                    {
                        return DateTime.MinValue + when.Value.TimeOfDay;
                    }
                }
            }

            return null;
        }

        public static bool IsStartTimeNow(this LuisResult result)
        {
            return result.Entities.Any(e => e.Resolution != null && e.Resolution.ContainsKey("time") && e.Resolution["time"] == "PRESENT_REF");
        }

        /// <summary>
        /// Find the best duration entity in the LuisResult. To support phrases like
        /// '1 hour and 30 minutes' where there might be multiple durations, we want to
        /// look for the one that seems most complete, which will most likely be the
        /// one with the longest duration.
        /// </summary>
        /// <param name="result">The LuisResult to scan for a duration</param>
        /// <returns>The 'best' duration found, converted to a TimeSpan</returns>
        public static TimeSpan? FindDuration(this LuisResult result)
        {
            TimeSpan? maxDuration = null;
            foreach (var entity in result.Entities)
            {
                if (entity.Type == EntityBuiltinDuration && entity.Resolution.ContainsKey("duration"))
                {
                    var duration = System.Xml.XmlConvert.ToTimeSpan(entity.Resolution["duration"]);

                    if (maxDuration == null || duration > maxDuration.Value)
                    {
                        maxDuration = duration;
                    }
                }
            }

            return maxDuration;
        }
    }
}