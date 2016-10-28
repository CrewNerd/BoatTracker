using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public static bool IsPrivate(this JToken jtoken)
        {
            var isPrivate = jtoken
                .Value<JArray>("customAttributes")
                .Where(x => x.Value<string>("label").StartsWith("Private"))
                .Select(t => t.Value<string>("value"))
                .FirstOrDefault();

            return isPrivate != null && isPrivate != "0";
        }

        public static int MaxParticipants(this JToken jtoken)
        {
            return jtoken.Value<int>("maxParticipants");
        }

        #endregion

        #region Reservation helpers

        public static DateTime StartDate(this JToken jtoken)
        {
            // NOTE: Look for either startDateTime or startDate to work around a BookedScheduler problem
            DateTime? dateTime = jtoken.Value<DateTime?>("startDateTime");

            if (!dateTime.HasValue)
            {
                dateTime = jtoken.Value<DateTime?>("startDate");
            }

            if (!dateTime.HasValue)
            {
                throw new ArgumentNullException("Missing date/time");
            }

            return dateTime.Value;
        }

        public static DateTime EndDate(this JToken jtoken)
        {
            // NOTE: Look for either startDateTime or startDate to work around a BookedScheduler problem
            DateTime? dateTime = jtoken.Value<DateTime?>("endDateTime");

            if (!dateTime.HasValue)
            {
                dateTime = jtoken.Value<DateTime?>("endDate");
            }

            if (!dateTime.HasValue)
            {
                throw new ArgumentNullException("Missing date/time");
            }

            return dateTime.Value;
        }

        public static DateTime? CheckInDate(this JToken jtoken)
        {
            DateTime? checkInDate = null;

            if (!string.IsNullOrEmpty(jtoken.Value<string>("checkInDate")))
            {
                checkInDate = jtoken.Value<DateTime>("checkInDate");
            }

            return checkInDate;
        }

        public static DateTime? CheckOutDate(this JToken jtoken)
        {
            DateTime? checkOutDate = null;

            if (!string.IsNullOrEmpty(jtoken.Value<string>("checkOutDate")))
            {
                checkOutDate = jtoken.Value<DateTime>("checkOutDate");
            }

            return checkOutDate;
        }

        public static string ReferenceNumber(this JToken jtoken)
        {
            return jtoken.Value<string>("referenceNumber");
        }

        public static string ParticipantNames(this JToken jtoken)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(jtoken.Value<string>("firstName"));
            sb.Append(" ");
            sb.Append(jtoken.Value<string>("lastName"));

            if (jtoken["participants"] is JObject)
            {
                var participants = (JObject)jtoken["participants"];

                foreach (var kv in participants)
                {
                    sb.Append(", ");
                    sb.Append(kv.Value);
                }
            }

            return sb.ToString();
        }

        public static string ResourceName(this JToken jtoken)
        {
            return jtoken.Value<string>("resourceName");
        }

        #endregion

        #region User helpers

        public static long Id(this JToken jtoken)
        {
            return jtoken.Value<long>("id");
        }

        public static string UserName(this JToken jtoken)
        {
            return jtoken.Value<string>("username");
        }

        public static string FullName(this JToken jtoken)
        {
            return $"{jtoken.Value<string>("firstName")} {jtoken.Value<string>("lastName")}";
        }

        public static string EmailAddress(this JToken jtoken)
        {
            return jtoken.Value<string>("emailaddress");
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