using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;

namespace BoatTracker.BookedScheduler
{
    public static class JTokenExtensions
    {
        #region Resource helpers

        public static long ResourceId(this JToken jtoken)
        {
            return jtoken.Value<long>("resourceId");
        }

        public static string Name(this JToken jtoken)
        {
            return jtoken.Value<string>("name");
        }

        public static IEnumerable<string> BoatTagIds(this JToken jtoken)
        {
            var boatTagIds = jtoken
                .Value<JArray>("customAttributes")
                .Where(x => x.Value<string>("label").StartsWith("RFID Tag"))
                .Select(t => t.Value<string>("value") ?? string.Empty);

            return boatTagIds;
        }

        #endregion

        #region Reservation helpers

        public static string StartDate(this JToken jtoken)
        {
            return jtoken.Value<string>("startDate");
        }

        public static string ReferenceNumber(this JToken jtoken)
        {
            return jtoken.Value<string>("referenceNumber");
        }

        #endregion

        #region User helpers

        public static string UserName(this JToken jtoken)
        {
            return jtoken.Value<string>("username");
        }

        public static string MakerChannelKey(this JToken jtoken)
        {
            var jTokenChannelKey = jtoken
                .Value<JArray>("customAttributes")
                .Where(x => x.Value<string>("label").StartsWith("IFTTT"))
                .FirstOrDefault();

            return jTokenChannelKey?.Value<string>("value") ?? string.Empty;
        }

        #endregion
    }
}