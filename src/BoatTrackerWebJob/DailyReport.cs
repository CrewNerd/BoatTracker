using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Azure;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json.Linq;
using NodaTime.TimeZones;
using SendGrid;
using SendGrid.Helpers.Mail;

using BoatTracker.BookedScheduler;
using BoatTracker.Bot.Configuration;

namespace BoatTrackerWebJob
{
    public class DailyReport
    {
        [NoAutomaticTrigger]
        public static void SendDailyReport([Blob("container/dailyreport.txt")] TextWriter log)
        {
            var env = EnvironmentDefinition.Instance;

            log.WriteLine($"{env.Name}: Daily Report WebJob starting at {DateTime.UtcNow.ToString()}");

            DateTime utcNow = DateTime.UtcNow;

            foreach (var clubId in env.MapClubIdToClubInfo.Keys)
            {
                var clubInfo = env.MapClubIdToClubInfo[clubId];

                if (clubInfo.DailyReportGmtHour == utcNow.Hour)
                {
                    log.WriteLine($"Starting daily report for club: {clubId}");

                    try
                    {
                        Task t = Task.Run(() => RunDailyReport(clubId, log));
                        t.Wait();
                    }
                    catch (Exception ex)
                    {
                        log.WriteLine($"RunDailyReport failed: {ex.Message}");
                        log.WriteLine($"RunDailyReport failed: {ex.StackTrace}");
                        if (ex.InnerException != null)
                        {
                            log.WriteLine($"RunDailyReport failed: inner exception = {ex.InnerException.Message}");
                            log.WriteLine($"RunDailyReport failed: inner exception = {ex.InnerException.StackTrace}");
                        }
                    }

                    log.WriteLine($"Finished daily report for club: {clubId}");
                }
            }

            log.WriteLine($"Daily Report WebJob complete at {DateTime.UtcNow.ToString()}");
        }

        private static async Task RunDailyReport(string clubId, TextWriter log)
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
                // Get the "full" reservation to make sure the participants list is given as an array
                var fullReservation = await client.GetReservationAsync(reservation.ReferenceNumber());

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

                var participants = (JArray)fullReservation["participants"];

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

            sbMessage.AppendLine($"<h2>BoatTracker Daily Report for: {clubInfo.Name}</h2>");
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

                sbMessage.AppendLine("</p>");
            }
            else
            {
                sbMessage.AppendLine("<p>No incomplete rosters.</p>");
            }

            log.Write(sbMessage.ToString());

            await SendDailyReportEmail(
                log,
                clubInfo,
                $"BoatTracker daily report for {clubInfo.Name}" + (EnvironmentDefinition.Instance.IsDevelopment ? " (DEV)" : ""),
                sbMessage.ToString());
        }

        private static async Task SendDailyReportEmail(TextWriter log, ClubInfo clubInfo, string subject, string body)
        {
            dynamic sg = new SendGridAPIClient(EnvironmentDefinition.Instance.SendGridApiKey);

            Email from = new Email(clubInfo.DailyReportSender);
            Content content = new Content("text/html", body);

            string[] recipients = new string[0];

            if (EnvironmentDefinition.Instance.IsDevelopment)
            {
                recipients = new string[] { CloudConfigurationManager.GetSetting("DeveloperEmail") };
            }
            else if (EnvironmentDefinition.Instance.IsProduction)
            {
                recipients = clubInfo.DailyReportRecipients.Split(',');
            }

            foreach (var recipient in recipients)
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
