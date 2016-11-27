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

        /// <summary>
        /// Gets the ResourceId from a resource node.
        /// </summary>
        /// <param name="jtoken">The extension object.</param>
        /// <returns>The ResourceId of a resource.</returns>
        public static long ResourceId(this JToken jtoken)
        {
            return jtoken.Value<long>(ResourceIdKey);
        }

        /// <summary>
        /// Gets the name of a resource.
        /// </summary>
        /// <param name="jtoken">The extension object.</param>
        /// <returns>The name of the resource.</returns>
        public static string Name(this JToken jtoken)
        {
            return jtoken.Value<string>(NameKey);
        }

        /// <summary>
        /// Gets the RFID tags for a resource.
        /// </summary>
        /// <param name="jtoken">The extension object.</param>
        /// <returns>A list of RFID tags associated with the resource.</returns>
        public static IEnumerable<string> BoatTagIds(this JToken jtoken)
        {
            var boatTagIds = jtoken
                .Value<JArray>(CustomAttributesKey)
                .Where(x => x.Value<string>(LabelKey).StartsWith("RFID Tag"))
                .Select(t => t.Value<string>(ValueKey) ?? string.Empty);

            return boatTagIds;
        }

        /// <summary>
        /// Gets a value indicating whether the resource is privately owned.
        /// </summary>
        /// <param name="jtoken">The extension object.</param>
        /// <returns>True if the resource is privately owned.</returns>
        public static bool IsPrivate(this JToken jtoken)
        {
            var isPrivate = jtoken
                .Value<JArray>(CustomAttributesKey)
                .Where(x => x.Value<string>(LabelKey).StartsWith("Private"))
                .Select(t => t.Value<string>(ValueKey))
                .FirstOrDefault();

            return isPrivate != null && isPrivate != "0";
        }

        /// <summary>
        /// Gets the maximum number of participants for the resource.
        /// </summary>
        /// <param name="jtoken">The extension object.</param>
        /// <returns>The maximum number of participants for the resource.</returns>
        public static int MaxParticipants(this JToken jtoken)
        {
            // Default to 1 if the administrator fails to set this property
            return jtoken.Value<int?>(MaxParticipantsKey) ?? 1;
        }

        #endregion

        #region Reservation helpers

        /// <summary>
        /// Gets the start date for a reservation.
        /// </summary>
        /// <param name="jtoken">The extension object.</param>
        /// <returns>The start date of the reservation.</returns>
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

        /// <summary>
        /// Gets the end date for a reservation.
        /// </summary>
        /// <param name="jtoken">The extension object.</param>
        /// <returns>The end date of the reservation.</returns>
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

        /// <summary>
        /// Gets the checkin date for a reservation (if checked in).
        /// </summary>
        /// <param name="jtoken">The extension object.</param>
        /// <returns>The checkin date or null if not checked in.</returns>
        public static DateTime? CheckInDate(this JToken jtoken)
        {
            DateTime? checkInDate = null;

            if (!string.IsNullOrEmpty(jtoken.Value<string>(CheckInDateKey)))
            {
                checkInDate = jtoken.Value<DateTime>(CheckInDateKey);
            }

            return checkInDate;
        }

        /// <summary>
        /// Gets the checkout date for a reservation (if checked out).
        /// </summary>
        /// <param name="jtoken">The extension object.</param>
        /// <returns>The checkout date or null if not checked out.</returns>
        public static DateTime? CheckOutDate(this JToken jtoken)
        {
            DateTime? checkOutDate = null;

            if (!string.IsNullOrEmpty(jtoken.Value<string>(CheckOutDateKey)))
            {
                checkOutDate = jtoken.Value<DateTime>(CheckOutDateKey);
            }

            return checkOutDate;
        }

        /// <summary>
        /// Gets the reference number for a reservation.
        /// </summary>
        /// <param name="jtoken">The extension object.</param>
        /// <returns>The reference number for the reservation.</returns>
        public static string ReferenceNumber(this JToken jtoken)
        {
            return jtoken.Value<string>(ReferenceNumberKey);
        }

        /// <summary>
        /// Gets a full list of participant names for a reservation. This includes the reservation
        /// owner (if includeOwner is true), any confirmed participants, and any invited or
        /// participating guests.
        /// </summary>
        /// <param name="jtoken">The extension object.</param>
        /// <param name="includeOwner">If true, include the reservation owner in the returned list.</param>
        /// <returns>A comma-separated list of participants (full names or email addresses).</returns>
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

        /// <summary>
        /// Gets the name of the resource associated with a reservation.
        /// </summary>
        /// <param name="jtoken">The extension object.</param>
        /// <returns>The resource name for the reservation.</returns>
        public static string ResourceName(this JToken jtoken)
        {
            return jtoken.Value<string>(ResourceNameKey);
        }

        /// <summary>
        /// Gets the id of the reservation owner.
        /// </summary>
        /// <param name="jtoken">The extension object.</param>
        /// <returns>The user id associated with the reservation.</returns>
        public static long UserId(this JToken jtoken)
        {
            return jtoken.Value<long>(UserIdKey);
        }

        #endregion

        #region User helpers

        /// <summary>
        /// Gets the id of a user.
        /// </summary>
        /// <param name="jtoken">The extension object.</param>
        /// <returns>The user id.</returns>
        public static long Id(this JToken jtoken)
        {
            return jtoken.Value<long>(IdKey);
        }

        /// <summary>
        /// Gets the username for a user object.
        /// </summary>
        /// <param name="jtoken">The extension object.</param>
        /// <returns>The username of the user.</returns>
        public static string UserName(this JToken jtoken)
        {
            return jtoken.Value<string>(UserNameKey);
        }

        /// <summary>
        /// Gets the first name of the user.
        /// </summary>
        /// <param name="jtoken">The extension object.</param>
        /// <returns>The user's first name.</returns>
        public static string FirstName(this JToken jtoken)
        {
            return jtoken.Value<string>(FirstNameKey);
        }

        /// <summary>
        /// Gets the last name of the user.
        /// </summary>
        /// <param name="jtoken">The extension object.</param>
        /// <returns>The user's last name.</returns>
        public static string LastName(this JToken jtoken)
        {
            return jtoken.Value<string>(LastNameKey);
        }

        /// <summary>
        /// Gets the full name of the user.
        /// </summary>
        /// <param name="jtoken">The extension object.</param>
        /// <returns>The full name of the user.</returns>
        public static string FullName(this JToken jtoken)
        {
            return $"{jtoken.Value<string>(FirstNameKey)} {jtoken.Value<string>(LastNameKey)}";
        }

        /// <summary>
        /// Gets the email address of the user.
        /// </summary>
        /// <param name="jtoken">The extension object.</param>
        /// <returns>The user's email address.</returns>
        public static string EmailAddress(this JToken jtoken)
        {
            return jtoken.Value<string>(EmailAddressKey);
        }

        /// <summary>
        /// Gets the time zone string of the user.
        /// </summary>
        /// <param name="jtoken">The extension object.</param>
        /// <returns>The user's time zone.</returns>
        public static string Timezone(this JToken jtoken)
        {
            return jtoken.Value<string>(TimezoneKey);
        }

        /// <summary>
        /// Gets the IFTTT maker channel key for the user.
        /// </summary>
        /// <param name="jtoken">The extension object.</param>
        /// <returns>The user's IFTTT maker channel key or an empty string if not key is set.</returns>
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