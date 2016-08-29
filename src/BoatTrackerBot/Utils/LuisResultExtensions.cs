using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BoatTracker.Bot.Utils
{
    public static class LuisResultExtensions
    {
        public const string EntityBoatName = "boatName";
        public const string EntityStart = "DateTime::startDate";
        public const string EntityDuration = "DateTime::duration";
        public const string EntityBuiltinDate = "builtin.datetime.date";
        public const string EntityBuiltinTime = "builtin.datetime.time";
        public const string EntityBuiltinDuration = "builtin.datetime.duration";

        public static bool ContainsBoatNameEntity(this LuisResult result)
        {
            return result.Entities.Any(e => e.Type == EntityBoatName);
        }

        public static string BoatName(this LuisResult result)
        {
            var nameEntities = result.Entities.Where(e => e.Type == EntityBoatName).Select(e => e.Entity);
            return string.Join(" ", nameEntities);
        }

        public static DateTime? FindStartDate(this LuisResult result)
        {
            EntityRecommendation builtinDate = null;
            result.TryFindEntity(EntityBuiltinDate, out builtinDate);

            if (builtinDate != null && builtinDate.Resolution.ContainsKey("date"))
            {
                var parser = new Chronic.Parser();
                var span = parser.Parse(builtinDate.Entity);

                if (span != null)
                {
                    var when = span.Start ?? span.End;
                    return when.Value;
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

        public static DateTime? FindStartTime(this LuisResult result)
        {
            EntityRecommendation builtinTime = null;
            result.TryFindEntity(EntityBuiltinTime, out builtinTime);

            if (builtinTime != null && builtinTime.Resolution.ContainsKey("time"))
            {
                var parser = new Chronic.Parser();
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