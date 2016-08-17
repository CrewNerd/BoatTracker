using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.Luis.Models;

using Newtonsoft.Json.Linq;
using NodaTime.TimeZones;

using BoatTracker.Bot.Configuration;

namespace BoatTracker.Bot.Utils
{
    public static class UserStateExtensions
    {
        public static async Task<string> DescribeReservationsAsync(this UserState userState, IList<JToken> reservations)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var reservation in reservations)
            {
                DateTime startDate = DateTime.Parse(reservation.Value<string>("startDate"));
                startDate = userState.ConvertToLocalTime(startDate);
                var duration = reservation.Value<string>("duration");

                var boatName = await BookedSchedulerCache
                    .Instance[userState.ClubId]
                    .GetResourceNameFromIdAsync(reservation.Value<long>("resourceId"));

                sb.AppendFormat("\r\n\r\n**{0} {1}** {2} *({3})*", startDate.ToLocalTime().ToString("d"), startDate.ToLocalTime().ToString("t"), boatName, duration);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Look for an acceptable match between the 'boatName' entities found by LUIS and
        /// the known set of boat names for the user's club. Consider alternate names for
        /// each boat as configured by the administrator using a custom attribute.
        /// </summary>
        /// <param name="userState">The user context</param>
        /// <param name="entities">The entities discovered by LUIS</param>
        /// <returns>The JToken for the matching boat resource, or null if no good match was found.</returns>
        public static async Task<JToken> FindBestResourceMatchAsync(this UserState userState, IList<EntityRecommendation> entities)
        {
            var entityWords = entities.Where(e => e.Type == "boatName").SelectMany(e => e.Entity.ToLower().Split(' ')).ToList();

            if (entityWords.Count == 0)
            {
                return null;
            }

            var resources = await BookedSchedulerCache.Instance[userState.ClubId].GetResourcesAsync();

            var boat = resources.FirstOrDefault((b) => PerfectMatchBoat(entityWords, b));

            if (boat != null)
            {
                return boat;
            }

            //
            // Next, check to see if a subset of the entities completely spans the boat name words.
            // This could happen if LUIS classifies "extra" words as being part of the boat name.
            //
            boat = resources.FirstOrDefault((b) => OverMatchBoat(entityWords, b));

            if (boat != null)
            {
                return boat;
            }

            //
            // Next, check for cases where the entities all match a subset of the boat name words.
            // This could happen if LUIS failed to classify some words as being part of the boat
            // name OR if the user provides only a portion of a longer name.
            //
            boat = resources.FirstOrDefault((b) => UnderMatchBoat(entityWords, b));

            if (boat != null)
            {
                return boat;
            }

            // TODO: Consider adding fuzzy matching of the individual entity words before we give
            // up entirely.

            return null;    // no match
        }

        private static bool PerfectMatchBoat(IList<string> entityWords, JToken boat)
        {
            //
            // Check for a perfect match with any of the boat names
            //
            return GetBoatNames(boat).Any(name => PerfectMatchName(entityWords, name.ToLower().Split(' ')));
        }

        private static bool PerfectMatchName(IList<string> entityWords, string[] boatNameWords)
        {
            return entityWords.Count == boatNameWords.Count() && boatNameWords.All(word => entityWords.Contains(word));
        }

        private static bool OverMatchBoat(IList<string> entityWords, JToken boat)
        {
            //
            // Check to see if a subset of the entities completely spans the boat name words.
            // This could happen if LUIS classifies "extra" words as being part of the boat name.
            //
            return GetBoatNames(boat).Any(name => OverMatchName(entityWords, name.ToLower().Split(' ')));
        }

        private static bool OverMatchName(IList<string> entityWords, string[] boatNameWords)
        {
            return entityWords.Count > boatNameWords.Count() && boatNameWords.All(word => entityWords.Contains(word));
        }

        private static bool UnderMatchBoat(IList<string> entityWords, JToken boat)
        {
            //
            // Check for cases where the entities all match a subset of the boat name words.
            // This could happen if LUIS failed to classify some words as being part of the boat
            // name OR if the user provides only a portion of a longer name.
            //
            return GetBoatNames(boat).Any(name => UnderMatchName(entityWords, name.ToLower().Split(' ')));
        }

        private static bool UnderMatchName(IList<string> entityWords, string[] boatNameWords)
        {
            return entityWords.Count < boatNameWords.Count() && entityWords.All(word => boatNameWords.Contains(word));
        }

        private static IList<string> GetBoatNames(JToken boat)
        {
            // Start with the set of alternate names (if any) for the boat
            var boatNames = (boat
                .Value<JArray>("customAttributes")
                .Where(x => x.Value<string>("label") == "Alternate names")
                .First()
                .Value<string>("value") ?? string.Empty)
                .Split(',')
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            // And add its preferred name
            boatNames.Add(boat.Value<string>("name"));

            return boatNames;
        }

        public static DateTime ConvertToLocalTime(this UserState userState, DateTime dateTime)
        {
            var mappings = TzdbDateTimeZoneSource.Default.WindowsMapping.MapZones;
            var mappedTz = mappings.FirstOrDefault(x => x.TzdbIds.Any(z => z.Equals(userState.Timezone, StringComparison.OrdinalIgnoreCase)));

            if (mappedTz == null)
            {
                return dateTime;
            }

            var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(mappedTz.WindowsId);
            return dateTime + tzInfo.GetUtcOffset(dateTime);
        }
    }
}

