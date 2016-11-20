using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.Luis.Models;

using Newtonsoft.Json.Linq;
using NodaTime.TimeZones;

using BoatTracker.Bot.Configuration;
using BoatTracker.Bot.DataObjects;
using BoatTracker.BookedScheduler;

namespace BoatTracker.Bot.Utils
{
    public static class UserStateExtensions
    {
        public static ClubInfo ClubInfo(this UserState userState)
        {
            return EnvironmentDefinition.Instance.MapClubIdToClubInfo[userState.ClubId];
        }

        #region Reservations

        public static async Task<string> DescribeReservationsAsync(
            this UserState userState,
            IList<JToken> reservations,
            bool showOwner = false,
            bool showDate = true,
            bool showIndex = false,
            bool useMarkdown = true)
        {
            StringBuilder sb = new StringBuilder();

            int i = 1;
            foreach (var reservation in reservations)
            {
                sb.Append("\n\n");
                sb.Append(await userState.DescribeReservationAsync(reservation, i++, showOwner, showDate, showIndex, useMarkdown));
            }

            if (showIndex)
            {
                if (useMarkdown)
                {
                    sb.AppendFormat("\n\n**{0}**:  **None of the above**", i);
                }
                else
                {
                    sb.AppendFormat("\n\n{0}:  None of the above", i);
                }
            }

            return sb.ToString();
        }

        public static async Task<string> DescribeReservationAsync(
            this UserState userState,
            JToken reservation,
            int index,
            bool showOwner,
            bool showDate,
            bool showIndex,
            bool useMarkdown)
        {
            var startDate = userState.ConvertToLocalTime(reservation.StartDate());
            var duration = reservation.Value<string>("duration");

            var boatName = await BookedSchedulerCache
                .Instance[userState.ClubId]
                .GetResourceNameFromIdAsync(reservation.ResourceId());

            string owner = string.Empty;

            if (showOwner)
            {
                owner = $" {reservation.FirstName()} {reservation.LastName()}";
            }

            string partnerName = string.Empty;

            //
            // Getting the participant list is messy. When it's empty, it looks like an empty
            // array. When there are participants, it looks like an object with each key/value
            // pair consisting of the user id and the full user name. This is probably a bug so
            // we also try to handle the case that we expected (array of integer user id's).
            //
            if (reservation["participants"] is JArray)
            {
                var participants = (JArray)reservation["participants"];

                if (participants.Count > 0)
                {
                    var partnerRef = participants[0];
                    var partnerUser = await BookedSchedulerCache.Instance[userState.ClubId].GetUserAsync(partnerRef.UserId());
                    partnerName = $" w/ {partnerUser.FullName()}";
                }
            }
            else if (reservation["participants"] is JObject)
            {
                var participants = (JObject)reservation["participants"];

                foreach (var kv in participants)
                {
                    var partnerId = long.Parse(kv.Key);
                    var partnerUser = await BookedSchedulerCache.Instance[userState.ClubId].GetUserAsync(partnerId);
                    partnerName = $" w/ {partnerUser.FullName()}";
                    break;
                }
            }

            if (useMarkdown)
            {
                return string.Format(
                    "{0}**{1} {2}** {3}{4} *({5})*{6}",
                    showIndex ? $"**{index}**:  " : string.Empty,
                    showDate ? startDate.ToLocalTime().ToString("d") : string.Empty,
                    startDate.ToLocalTime().ToString("t"),
                    boatName,
                    partnerName,
                    duration,
                    owner);
            }
            else
            {
                return string.Format(
                    "{0}{1} {2} {3}{4} ({5}) {6}",
                    showIndex ? $"{index}:  " : string.Empty,
                    showDate ? startDate.ToLocalTime().ToString("d") : string.Empty,
                    startDate.ToLocalTime().ToString("t"),
                    boatName,
                    partnerName,
                    duration,
                    owner);
            }
        }

        public static async Task<string> SummarizeReservationAsync(this UserState userState, JToken reservation)
        {
            var startDate = userState.ConvertToLocalTime(reservation.StartDate());

            var boatName = await BookedSchedulerCache
                .Instance[userState.ClubId]
                .GetResourceNameFromIdAsync(reservation.ResourceId());

            return string.Format(
                "{0} {1} {2}",
                startDate.ToLocalTime().ToString("d"),
                startDate.ToLocalTime().ToString("t"),
                boatName
                );
        }

        #endregion

        #region Resources

        /// <summary>
        /// Check to see whether a user has permission to use a boat. By default, we check the
        /// user in the given UserState. But if a partnerUserId is provided, we check their
        /// permission instead.
        /// </summary>
        /// <param name="userState">The state for the calling user</param>
        /// <param name="resource">The resource that they want to access</param>
        /// <param name="partnerUserId">If given, check permission for this user.</param>
        /// <returns>Task that completes with true if the user has permission for the resource.</returns>
        public static async Task<bool> HasPermissionForResourceAsync(this UserState userState, JToken resource, long? partnerUserId = null)
        {
            var cache = BookedSchedulerCache.Instance[userState.ClubId];
            var user = await cache.GetUserAsync(partnerUserId ?? userState.UserId);
            var resourceId = resource.ResourceId();

            // See if the user is granted permission to the resource directly.
            var okByUser = user
                .Value<JArray>("permissions")
                .Any(r => r.Id() == resourceId);

            if (okByUser)
            {
                return true;
            }

            // See if any of the user's group memberships grant permission to the resource
            foreach (var group in user.Value<JArray>("groups"))
            {
                var groupNode = await cache.GetGroupAsync(group.Id());

                var okByGroup = groupNode
                    .Value<JArray>("permissions")
                    .Any(r => r.Value<string>().EndsWith($"/{resourceId}"));

                if (okByGroup)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Look for a matching boat resource given a user-entered name string.
        /// </summary>
        /// <param name="userState">The user context</param>
        /// <param name="name">The boat name as typed by the user</param>
        /// <returns>The JToken for the matching boat resource, or null if no good match was found.</returns>
        public static Task<Tuple<JToken, string>> FindBestResourceMatchAsync(this UserState userState, string name)
        {
            return FindBestResourceMatchAsync(
                userState,
                new LuisResult
                {
                    Entities = new List<EntityRecommendation>()
                    {
                        new EntityRecommendation
                        {
                            Type = LuisResultExtensions.EntityBoatName,
                            Entity = name
                        }
                    }
                });
        }

        /// <summary>
        /// Some terms should never be allowed in boat names. If LUIS mis-identifies these as part
        /// of a boat name entity, we remove them to make sure they don't confuse our resource matching.
        /// One common case is: "Reserve boat-name now for 30 minutes", where "now" is sometimes
        /// identified as part of the boat name.
        /// </summary>
        private readonly static string[] excludedBoatNameTerms = new string[]
        {
            "now"
        };

        /// <summary>
        /// Look for an acceptable match between the 'boatName' entities found by LUIS and
        /// the known set of boat names for the user's club. Consider alternate names for
        /// each boat as configured by the administrator using a custom attribute.
        /// </summary>
        /// <param name="userState">The user context</param>
        /// <param name="entities">The entities discovered by LUIS</param>
        /// <returns>The JToken for the matching boat resource, or null if no good match was found.</returns>
        public static async Task<Tuple<JToken, string>> FindBestResourceMatchAsync(this UserState userState, LuisResult result)
        {
            var entities = result.Entities;
            var entityWords = entities
                .Where(e => e.Type == LuisResultExtensions.EntityBoatName)
                .SelectMany(e => e.Entity.ToLower().Split(' '))
                .Where(word => !excludedBoatNameTerms.Contains(word))
                .ToList();

            if (entityWords.Count == 0)
            {
                return new Tuple<JToken, string>(null, "I'm sorry, but I didn't see anything that looked like a boat name.");
            }

            var resources = await BookedSchedulerCache.Instance[userState.ClubId].GetResourcesAsync();

            // If the boats are named sensibly, there should be only one perfect match.
            var boat = resources.FirstOrDefault((b) => PerfectMatchBoat(entityWords, b));

            if (boat != null)
            {
                return new Tuple<JToken, string>(boat, null);
            }

            //
            // Next, check to see if a subset of the entities completely spans the boat name words.
            // This could happen if LUIS classifies "extra" words as being part of the boat name.
            //
            var overMatches = resources.Where((b) => OverMatchBoat(entityWords, b));

            switch (overMatches.Count())
            {
                case 0:
                    break;  // fall through and check for "under-matches"

                case 1:
                    return new Tuple<JToken, string>(overMatches.First(), null);

                default:
                    // Multiple matches - ask for clarification.
                    var boatNames = overMatches.Select(b => $"'{b.Name()}'");
                    return new Tuple<JToken, string>(null, $"I think you meant one of these boats ({string.Join(", ", boatNames)}). Can you be more specific?");
            }

            //
            // Next, check for cases where the entities all match a subset of the boat name words.
            // This could happen if LUIS failed to classify some words as being part of the boat
            // name OR if the user provides only a portion of a longer name.
            //
            var underMatches = resources.Where((b) => UnderMatchBoat(entityWords, b));

            // TODO: If one partial match is superior to all others, we should allow it.

            switch (underMatches.Count())
            {
                case 0:
                    return new Tuple<JToken, string>(null, $"I'm sorry, but I didn't find any good matches for '{result.BoatName()}' in your club's boat list.");

                case 1:
                    return new Tuple<JToken, string>(underMatches.First(), null);

                default:
                    // Multiple matches - ask for clarification.
                    var boatNames = underMatches.Select(b => $"'{b.Name()}'");
                    return new Tuple<JToken, string>(null, $"I think you meant one of these boats ({string.Join(", ", boatNames)}). Can you be more specific?");
            }
        }

        private static bool PerfectMatchBoat(IList<string> entityWords, JToken boat)
        {
            //
            // Check for a perfect match with any of the boat names
            //
            return GetBoatNames(boat).Any(name => PerfectMatchName(entityWords, name.ToLower().Split(' ')));
        }

        private static bool OverMatchBoat(IList<string> entityWords, JToken boat)
        {
            //
            // Check to see if a subset of the entities completely spans the boat name words.
            // This could happen if LUIS classifies "extra" words as being part of the boat name.
            //
            return GetBoatNames(boat).Any(name => OverMatchName(entityWords, name.ToLower().Split(' ')));
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

        private static IEnumerable<string> GetBoatNames(JToken boat)
        {
            yield return boat.Name();

            // Start with the set of alternate names (if any) for the boat
            var boatNames = (boat
                .Value<JArray>("customAttributes")
                .Where(x => x.Value<string>("label") == "Alternate names")
                .First()
                .Value<string>("value") ?? string.Empty)
                .Split(',')
                .Where(s => !string.IsNullOrEmpty(s));

            foreach (var boatname in boatNames)
            {
                yield return boatname;
            }
        }

#endregion

#region Users

        /// <summary>
        /// Look for a matching user given a user-entered name string.
        /// </summary>
        /// <param name="userState">The user context</param>
        /// <param name="name">The user name as typed by the user</param>
        /// <returns>The JToken for the matching user, or null if no good match was found.</returns>
        public static Task<Tuple<JToken, string>> FindBestUserMatchAsync(this UserState userState, string name)
        {
            return FindBestUserMatchAsync(
                userState,
                new LuisResult
                {
                    Entities = new List<EntityRecommendation>()
                    {
                        new EntityRecommendation
                        {
                            Type = LuisResultExtensions.EntityRowerName,
                            Entity = name
                        }
                    }
                });
        }

        /// <summary>
        /// Look for an acceptable match between the 'userName' entities found by LUIS and
        /// the known set of user names for the user's club. Consider the first name, last
        /// name, and username for each user.
        /// </summary>
        /// <param name="userState">The user context</param>
        /// <param name="entities">The entities discovered by LUIS</param>
        /// <returns>The JToken for the matching user, or null if no good match was found.</returns>
        public static async Task<Tuple<JToken, string>> FindBestUserMatchAsync(this UserState userState, LuisResult result)
        {
            var entities = result.Entities;
            var entityWords = entities
                .Where(e => e.Type == LuisResultExtensions.EntityRowerName)
                .SelectMany(e => e.Entity.ToLower().Split(' '))
                .ToList();

            if (entityWords.Count == 0)
            {
                return new Tuple<JToken, string>(null, "I'm sorry, but I didn't see anything that looked like a user name.");
            }

            var users = await BookedSchedulerCache.Instance[userState.ClubId].GetUsersAsync();

            // If the boats are named sensibly, there should be only one perfect match.
            var user = users.FirstOrDefault((b) => PerfectMatchUser(entityWords, b));

            if (user != null)
            {
                return new Tuple<JToken, string>(user, null);
            }

            //
            // Next, check to see if a subset of the entities completely spans the user name words.
            // This could happen if LUIS classifies "extra" words as being part of the user name.
            //
            var overMatches = users.Where((b) => OverMatchUser(entityWords, b));

            switch (overMatches.Count())
            {
                case 0:
                    break;  // fall through and check for "under-matches"

                case 1:
                    return new Tuple<JToken, string>(overMatches.First(), null);

                default:
                    // Multiple matches - ask for clarification.
                    var userNames = overMatches.Select(u => $"'{u.FullName()}'");
                    return new Tuple<JToken, string>(null, $"I think you meant one of these users ({string.Join(", ", userNames)}). Can you be more specific?");
            }

            //
            // Next, check for cases where the entities all match a subset of the user name words.
            // This could happen if LUIS failed to classify some words as being part of the user
            // name OR if the user provides only a portion of a longer name.
            //
            var underMatches = users.Where((b) => UnderMatchUser(entityWords, b));

            // TODO: If one partial match is superior to all others, we should allow it.

            switch (underMatches.Count())
            {
                case 0:
                    return new Tuple<JToken, string>(null, $"I'm sorry, but I didn't find any good matches for '{result.UserName()}' in your club's member list.");

                case 1:
                    return new Tuple<JToken, string>(underMatches.First(), null);

                default:
                    // Multiple matches - ask for clarification.
                    var userNames = underMatches.Select(u => $"'{u.FullName()}'");
                    return new Tuple<JToken, string>(null, $"I think you meant one of these users ({string.Join(", ", userNames)}). Can you be more specific?");
            }
        }

        private static bool PerfectMatchUser(IList<string> entityWords, JToken user)
        {
            //
            // Check for a perfect match with any of the user names
            //
            return GetUserNames(user).Any(name => PerfectMatchName(entityWords, name.ToLower().Split(' ')));
        }

        private static bool OverMatchUser(IList<string> entityWords, JToken user)
        {
            //
            // Check to see if a subset of the entities completely spans the user name words.
            // This could happen if LUIS classifies "extra" words as being part of the user name.
            //
            return GetUserNames(user).Any(name => OverMatchName(entityWords, name.ToLower().Split(' ')));
        }

        private static bool UnderMatchUser(IList<string> entityWords, JToken user)
        {
            //
            // Check for cases where the entities all match a subset of the user name words.
            // This could happen if LUIS failed to classify some words as being part of the user
            // name OR if the user provides only a portion of a longer name.
            //
            return GetUserNames(user).Any(name => UnderMatchName(entityWords, name.ToLower().Split(' ')));
        }

        private static IEnumerable<string> GetUserNames(JToken user)
        {
            // NOTE: The username should never be any user's first or last name!
            yield return $"{user.FirstName()} {user.LastName()}";
            yield return user.UserName();
        }

#endregion

#region Matching Helpers

        private static bool PerfectMatchName(IList<string> entityWords, string[] userNameWords)
        {
            return entityWords.Count == userNameWords.Count() && userNameWords.All(word => entityWords.Contains(word));
        }

        private static bool UnderMatchName(IList<string> entityWords, string[] userNameWords)
        {
            return entityWords.Count < userNameWords.Count() && entityWords.All(word => userNameWords.Contains(word));
        }

        private static bool OverMatchName(IList<string> entityWords, string[] userNameWords)
        {
            return entityWords.Count > userNameWords.Count() && userNameWords.All(word => entityWords.Contains(word));
        }

#endregion

#region Timezones

        public static DateTime LocalTime(this UserState userState)
        {
            return userState.ConvertToLocalTime(DateTime.UtcNow);
        }

        public static DateTime ConvertToLocalTime(this UserState userState, DateTime dateTime)
        {
            return dateTime + userState.LocalOffsetForDate(dateTime);
        }

        public static TimeSpan LocalOffsetForDate(this UserState userState, DateTime date)
        {
            var user = BookedSchedulerCache.Instance[userState.ClubId].GetUserAsync(userState.UserId).Result;

            var mappings = TzdbDateTimeZoneSource.Default.WindowsMapping.MapZones;
            var mappedTz = mappings.FirstOrDefault(x => x.TzdbIds.Any(z => z.Equals(user.Timezone(), StringComparison.OrdinalIgnoreCase)));

            if (mappedTz == null)
            {
                return TimeSpan.Zero;
            }

            var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(mappedTz.WindowsId);

            TimeSpan offset = tzInfo.GetUtcOffset(date);

            return offset;
        }

#endregion
    }
}