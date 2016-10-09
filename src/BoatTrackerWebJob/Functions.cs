using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs;
using Newtonsoft.Json.Linq;
using NodaTime.TimeZones;
using SendGrid;
using SendGrid.Helpers.Mail;

using BoatTracker.BookedScheduler;
using BoatTracker.Bot.Configuration;

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
                    if (ex.InnerException != null)
                    {
                        log.WriteLine($"CheckAllPolicies failed: inner exception = {ex.InnerException.Message}");
                    }
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

            var abandoned = new List<string>();
            var noCheckOut = new List<string>();
            var unknownParticipants = new List<string>();

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
                        noCheckOut.Add(string.Format(
                            "{0} {1} ({2}) - '{3}' @ {4}",
                            user.Value<string>("firstName"),
                            user.Value<string>("lastName"),
                            user.Value<string>("emailAddress"),
                            boatName,
                            localStartTime.ToShortTimeString()
                            ));
                    }
                }
                else
                {
                    abandoned.Add(string.Format(
                        "{0} {1} ({2}) - '{3}' @ {4}",
                        user.Value<string>("firstName"),
                        user.Value<string>("lastName"),
                        user.Value<string>("emailAddress"),
                        boatName,
                        localStartTime.ToShortTimeString()
                        ));
                }

                var participants = (JArray)reservation["participants"];

                //
                // Check that all of the participants were recorded
                //
                if (participants.Count + 1 < boat.MaxParticipants())
                {
                    unknownParticipants.Add(string.Format(
                        "{0} {1} ({2}) - '{3}' @ {4}",
                        user.Value<string>("firstName"),
                        user.Value<string>("lastName"),
                        user.Value<string>("emailAddress"),
                        boatName,
                        localStartTime.ToShortTimeString()
                        ));
                }

                // TODO: Check for guest-related violations.
            }

            var sbMessage = new StringBuilder();

            sbMessage.AppendLine($"<h1>BoatTracker Daily Report for: {clubInfo.Name}</h1>");
            sbMessage.AppendLine($"<p>Total reservations: {reservations.Count}</p>");

            if (abandoned.Count > 0)
            {
                sbMessage.AppendLine($"<p>Unused reservations ({abandoned.Count}):<br/>");
                foreach (var s in abandoned)
                {
                    sbMessage.AppendLine(s);
                    sbMessage.AppendLine("<br/>");
                }

                sbMessage.AppendLine("</p>");
            }
            else
            {
                sbMessage.AppendLine("<p>No unused reservations.</p>");
            }

            if (noCheckOut.Count > 0)
            {
                sbMessage.AppendLine($"<p>Unclosed reservations ({noCheckOut.Count}):<br/>");
                foreach (var s in noCheckOut)
                {
                    sbMessage.AppendLine(s);
                    sbMessage.AppendLine("<br/>");

                }

                sbMessage.AppendLine("</p>");
            }
            else
            {
                sbMessage.AppendLine("<p>No unclosed reservations.</p>");
            }

            if (unknownParticipants.Count > 0)
            {
                sbMessage.AppendLine($"<p>Incomplete roster ({unknownParticipants.Count}):<br/>");
                foreach (var s in unknownParticipants)
                {
                    sbMessage.AppendLine(s);
                    sbMessage.AppendLine("<br/>");
                }

                sbMessage.AppendLine("</p");
            }
            else
            {
                sbMessage.AppendLine("<p>No incomplete rosters.</p>");
            }

            log.Write(sbMessage.ToString());

            await SendDailyReportEmail(
                log,
                clubInfo,
                $"BoatTracker daily report for {clubInfo.Name}",
                sbMessage.ToString());
        }

        private static async Task SendDailyReportEmail(TextWriter log, ClubInfo clubInfo, string subject, string body)
        {
            dynamic sg = new SendGridAPIClient(EnvironmentDefinition.Instance.SendGridApiKey);

            Email from = new Email(clubInfo.DailyReportSender);
            Content content = new Content("text/html", body);

            foreach (var recipient in clubInfo.DailyReportRecipients.Split(','))
            {
                try
                {
                    Email to = new Email(recipient);
                    Mail mail = new Mail(from, subject, to, content);

                    dynamic response = await sg.client.mail.send.post(requestBody: mail.Get());

                    log.WriteLine($"Sent report email to {recipient}");
                }
                catch (Exception ex)
                {
                    log.WriteLine($"Failed to send report email to {recipient}: {ex.Message}");
                }
            }
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
