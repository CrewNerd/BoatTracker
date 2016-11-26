using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;

namespace BoatTracker.BookedScheduler
{
    /// <summary>
    /// Extension methods for JToken to access properties for reservations, users, resources, and
    /// so on. We did this in lieu of creating a full object model for BookedScheduler but we may
    /// want to revisit that decision at some point.
    /// </summary>
    public static class JTokenExtensions
    {
        private const string CheckInDateKey = "checkInDate";
        private const string CheckOutDateKey = "checkOutDate";
        private const string CustomAttributesKey = "customAttributes";
        private const string EmailAddressKey = "emailAddress";
        private const string EndDateKey = "endDate";
        private const string EndDateTimeKey = "endDateTime";
        private const string FirstNameKey = "firstName";
        private const string IdKey = "id";
        private const string InvitedGuestsKey = "invitedGuests";
        private const string LabelKey = "label";
        private const string LastNameKey = "lastName";
        private const string MaxParticipantsKey = "maxParticipants";
        private const string NameKey = "name";
        private const string ParticipatingGuestsKey = "participatingGuests";
        private const string ParticipantsKey = "participants";
        private const string ReferenceNumberKey = "referenceNumber";
        private const string ResourceIdKey = "resourceId";
        private const string ResourceNameKey = "resourceName";
        private const string StartDateKey = "startDate";
        private const string StartDateTimeKey = "startDateTime";
        private const string TimezoneKey = "timezone";
        private const string UserIdKey = "userId";
        private const string UserNameKey = "userName";
        private const string ValueKey = "value";

        #region Resource helpers

        public static long ResourceId(this JToken jtoken)
        {
            return jtoken.Value<long>(ResourceIdKey);
        }

        public static string Name(this JToken jtoken)
        {
            return jtoken.Value<string>(NameKey);
        }

        public static IEnumerable<string> BoatTagIds(this JToken jtoken)
        {
            var boatTagIds = jtoken
                .Value<JArray>(CustomAttributesKey)
                .Where(x => x.Value<string>(LabelKey).StartsWith("RFID Tag"))
                .Select(t => t.Value<string>(ValueKey) ?? string.Empty);

            return boatTagIds;
        }

        public static bool IsPrivate(this JToken jtoken)
        {
            var isPrivate = jtoken
                .Value<JArray>(CustomAttributesKey)
                .Where(x => x.Value<string>(LabelKey).StartsWith("Private"))
                .Select(t => t.Value<string>(ValueKey))
                .FirstOrDefault();

            return isPrivate != null && isPrivate != "0";
        }

        public static int MaxParticipants(this JToken jtoken)
        {
            // Default to 1 if the administrator fails to set this property
            return jtoken.Value<int?>(MaxParticipantsKey) ?? 1;
        }

        #endregion

        #region Reservation helpers

        public static DateTime StartDate(this JToken jtoken)
        {
            // NOTE: Look for either startDateTime or startDate to work around a BookedScheduler problem
            DateTime? dateTime = jtoken.Value<DateTime?>(StartDateTimeKey);

            if (!dateTime.HasValue)
            {
                dateTime = jtoken.Value<DateTime?>(StartDateKey);
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
            DateTime? dateTime = jtoken.Value<DateTime?>(EndDateTimeKey);

            if (!dateTime.HasValue)
            {
                dateTime = jtoken.Value<DateTime?>(EndDateKey);
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

            if (!string.IsNullOrEmpty(jtoken.Value<string>(CheckInDateKey)))
            {
                checkInDate = jtoken.Value<DateTime>(CheckInDateKey);
            }

            return checkInDate;
        }

        public static DateTime? CheckOutDate(this JToken jtoken)
        {
            DateTime? checkOutDate = null;

            if (!string.IsNullOrEmpty(jtoken.Value<string>(CheckOutDateKey)))
            {
                checkOutDate = jtoken.Value<DateTime>(CheckOutDateKey);
            }

            return checkOutDate;
        }

        public static string ReferenceNumber(this JToken jtoken)
        {
            return jtoken.Value<string>(ReferenceNumberKey);
        }

        public static string ParticipantNames(this JToken jtoken, bool includeOwner = true)
        {
            var participantNames = new List<string>();

            if (includeOwner)
            {
                participantNames.Add($"{jtoken.Value<string>(FirstNameKey)} {jtoken.Value<string>(LastNameKey)}");
            }

            if (jtoken[ParticipantsKey] is JObject)
            {
                var participants = (JObject)jtoken[ParticipantsKey];

                foreach (var kv in participants)
                {
                    participantNames.Add(kv.Value.ToString());
                }
            }

            var invitedGuests = jtoken[InvitedGuestsKey] as JArray;
            if (invitedGuests != null)
            {
                participantNames.AddRange(invitedGuests.Select(t => t.Value<string>()).ToList());
            }

            var participatingGuests = jtoken[ParticipatingGuestsKey] as JArray;
            if (participatingGuests != null)
            {
                participantNames.AddRange(participatingGuests.Select(t => t.Value<string>()).ToList());
            }

            return string.Join(", ", participantNames);
        }

        public static string ResourceName(this JToken jtoken)
        {
            return jtoken.Value<string>(ResourceNameKey);
        }

        public static long UserId(this JToken jtoken)
        {
            return jtoken.Value<long>(UserIdKey);
        }

        #endregion

        #region User helpers

        public static long Id(this JToken jtoken)
        {
            return jtoken.Value<long>(IdKey);
        }

        public static string UserName(this JToken jtoken)
        {
            return jtoken.Value<string>(UserNameKey);
        }

        public static string FirstName(this JToken jtoken)
        {
            return jtoken.Value<string>(FirstNameKey);
        }

        public static string LastName(this JToken jtoken)
        {
            return jtoken.Value<string>(LastNameKey);
        }

        public static string FullName(this JToken jtoken)
        {
            return $"{jtoken.Value<string>(FirstNameKey)} {jtoken.Value<string>(LastNameKey)}";
        }

        public static string EmailAddress(this JToken jtoken)
        {
            return jtoken.Value<string>(EmailAddressKey);
        }

        public static string Timezone(this JToken jtoken)
        {
            return jtoken.Value<string>(TimezoneKey);
        }

        public static string MakerChannelKey(this JToken jtoken)
        {
            var jtokenChannelKey = jtoken
                .Value<JArray>(CustomAttributesKey)
                .Where(x => x.Value<string>(LabelKey).StartsWith("IFTTT"))
                .FirstOrDefault();

            return jtokenChannelKey?.Value<string>(ValueKey) ?? string.Empty;
        }

        #endregion
    }
}