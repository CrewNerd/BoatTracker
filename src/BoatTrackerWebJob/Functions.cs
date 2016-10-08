using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs;
using Newtonsoft.Json.Linq;
using NodaTime.TimeZones;

using BoatTracker.BookedScheduler;
using BoatTracker.Bot.Configuration;
using System.Text;

namespace BoatTrackerWebJob
{
    public class Functions
    {
        [NoAutomaticTrigger]
        public static void RunPolicyChecks([Blob("container/policychecklog.txt")] TextWriter log)
        {
            log.WriteLine($"Policy check WebJob starting at {DateTime.UtcNow.ToString()}");

            foreach (var clubId in EnvironmentDefinition.Instance.MapClubIdToClubInfo.Keys)
            {
                log.WriteLine($"Starting policy checks for club: {clubId}");

                try
                {
                    Task t = Task.Run(() => CheckAllPolicies(clubId, log));
                    t.Wait();
                }
                catch (Exception ex)
                {
                    log.WriteLine($"CheckAllPolicies failed: {ex.Message}");
                }

                log.WriteLine($"Finished policy checks for club: {clubId}");
            }

            log.WriteLine($"Policy checks complete at {DateTime.UtcNow.ToString()}");
        }

        private static async Task CheckAllPolicies(string clubId, TextWriter log)
        {
            var clubInfo = EnvironmentDefinition.Instance.MapClubIdToClubInfo[clubId];

            BookedSchedulerClient client = new BookedSchedulerClient(clubInfo.Url);

            await client.SignIn(clubInfo.UserName, clubInfo.Password);

            // Get all reservations for the last day
            var reservations = await client.GetReservationsAsync(start: DateTime.UtcNow - TimeSpan.FromDays(1), end: DateTime.UtcNow);

            int numAbandoned = 0;
            int numNoCheckOut = 0;
            int numUnknownParticipants = 0;

            var sbAbandoned = new StringBuilder();
            var sbNoCheckOut = new StringBuilder();
            var sbUnknownParticipants = new StringBuilder();

            foreach (var reservation in reservations)
            {
                DateTime? checkInDate = reservation.CheckInDate();
                DateTime? checkOutDate = reservation.CheckOutDate();

                var user = await client.GetUserAsync(reservation.Value<string>("userId"));
                var boatName = reservation.Value<string>("resourceName");

                var boat = await client.GetResourceAsync(reservation.Value<string>("resourceId"));
                var localStartTime = ConvertToLocalTime(user, reservation.StartDate());

                if (checkInDate.HasValue)
                {
                    if (!checkOutDate.HasValue)
                    {
                        numNoCheckOut++;

                        sbNoCheckOut.AppendFormat(
                            "{0} {1} ({2}) - '{3}' @ {4}\n",
                            user.Value<string>("firstName"),
                            user.Value<string>("lastName"),
                            user.Value<string>("emailAddress"),
                            boatName,
                            localStartTime.ToShortTimeString()
                            );
                    }
                }
                else
                {
                    numAbandoned++;

                    sbAbandoned.AppendFormat(
                        "{0} {1} ({2}) - '{3}' @ {4}\n",
                        user.Value<string>("firstName"),
                        user.Value<string>("lastName"),
                        user.Value<string>("emailAddress"),
                        boatName,
                        localStartTime.ToShortTimeString()
                        );
                }

                var participants = (JArray)reservation["participants"];

                //
                // Check that all of the participants were recorded
                //
                if (participants.Count + 1 < boat.MaxParticipants())
                {
                    numUnknownParticipants++;

                    sbUnknownParticipants.AppendFormat(
                        "{0} {1} ({2}) - '{3}' @ {4}\n",
                        user.Value<string>("firstName"),
                        user.Value<string>("lastName"),
                        user.Value<string>("emailAddress"),
                        boatName,
                        localStartTime.ToShortTimeString()
                        );
                }

                // TODO: Check for guest-related violations.
            }

            var sbMessage = new StringBuilder();

            sbMessage.AppendLine();
            sbMessage.AppendLine($"Daily report for: {clubInfo.Name}");
            sbMessage.AppendLine();
            sbMessage.AppendLine($"Total reservations: {reservations.Count}");
            sbMessage.AppendLine();
            sbMessage.AppendLine($"Unused reservations ({numAbandoned}):");
            sbMessage.Append(sbAbandoned.ToString());
            sbMessage.AppendLine();
            sbMessage.AppendLine($"Unclosed reservations ({numNoCheckOut}):");
            sbMessage.Append(sbNoCheckOut.ToString());
            sbMessage.AppendLine();
            sbMessage.AppendLine($"Incomplete roster ({numUnknownParticipants}):");
            sbMessage.Append(sbUnknownParticipants.ToString());
            sbMessage.AppendLine();

            log.Write(sbMessage.ToString());

            // TODO: send the message by email too
        }

        #region Timezone helpers

        private static DateTime ConvertToLocalTime(JToken user, DateTime dateTime)
        {
            return dateTime + LocalOffsetForDate(user, dateTime);
        }

        private static TimeSpan LocalOffsetForDate(JToken user, DateTime date)
        {
            var mappings = TzdbDateTimeZoneSource.Default.WindowsMapping.MapZones;
            var mappedTz = mappings.FirstOrDefault(x => x.TzdbIds.Any(z => z.Equals(user.Value<string>("timezone"), StringComparison.OrdinalIgnoreCase)));

            if (mappedTz == null)
            {
                return TimeSpan.Zero;
            }

            var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(mappedTz.WindowsId);

            TimeSpan offset = tzInfo.GetUtcOffset(date.Date);

            return offset;
        }

        #endregion
    }
}
