using System;
using System.Linq;

using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;

using BoatTracker.Bot.DataObjects;

namespace BoatTracker.Bot.Utils
{
    /// <summary>
    /// Extension methods for extracting information from LUIS results.
    /// </summary>
    public static class LuisResultExtensions
    {
        public const string EntityBoatName = "boatName";
        private const string EntityStart = "DateTime::startDate";
        private const string EntityDuration = "DateTime::duration";
        private const string EntityBuiltinDate = "builtin.datetime.date";
        private const string EntityBuiltinTime = "builtin.datetime.time";
        private const string EntityBuiltinDuration = "builtin.datetime.duration";

        public static bool ContainsBoatNameEntity(this LuisResult result)
        {
            return result.Entities.Any(e => e.Type == EntityBoatName);
        }

        public static string BoatName(this LuisResult result)
        {
            var nameEntities = result.Entities.Where(e => e.Type == EntityBoatName).Select(e => e.Entity);
            return string.Join(" ", nameEntities);
        }

        public static DateTime? FindStartDate(this LuisResult result, UserState userState)
        {
            TimeSpan MaxDaysInFuture = TimeSpan.FromDays(30);   // Limit how far in the future we can recognize

            EntityRecommendation builtinDate = null;
            result.TryFindEntity(EntityBuiltinDate, out builtinDate);

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
                    if (date >= DateTime.Now.Date && date <= DateTime.Now.Date + MaxDaysInFuture)
                    {
                        return date;
                    }
                }

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

                var span = parser.Parse(builtinDate.Entity);

                if (span != null)
                {
                    var when = span.Start ?? span.End;
                    return when.Value.Date;
                }
            }

            foreach (var startDate in result.Entities.Where(e => e.Type == EntityStart))
            {
                var parser = new Chronic.Parser();
                var span = parser.Parse(startDate.Entity);

                if (span != null)
                {
                    var when = span.Start ?? span.End;

                    if (when.Value.HasDate())
                    {
                        return when.Value.Date;
                    }
                }
            }

            return null;
        }

        public static DateTime? FindStartTime(this LuisResult result, UserState userState)
        {
            EntityRecommendation builtinTime = null;
            result.TryFindEntity(EntityBuiltinTime, out builtinTime);

            if (builtinTime != null && builtinTime.Resolution.ContainsKey("time"))
            {
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
                var parser = new Chronic.Parser();
                var span = parser.Parse(startDate.Entity.Replace("at ", ""));

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

        public static TimeSpan? FindDuration(this LuisResult result)
        {
            EntityRecommendation builtinDuration = null;
            result.TryFindEntity(EntityBuiltinDuration, out builtinDuration);

            if (builtinDuration != null && builtinDuration.Resolution.ContainsKey("duration"))
            {
                return System.Xml.XmlConvert.ToTimeSpan(builtinDuration.Resolution["duration"]);
            }

            return null;
        }
    }
}