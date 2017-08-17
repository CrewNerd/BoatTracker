using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BoatTracker.BookedScheduler;
using BoatTracker.Bot.Configuration;
using Microsoft.Azure;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json.Linq;
using NodaTime.TimeZones;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace BoatTrackerWebJob
{
    /// <summary>
    /// Entry point and helpers for generating the daily report of usage and policy violations.
    /// </summary>
    public class DailyReport
    {
        /// <summary>
        /// Iterates over all of the clubs, calling a helper to generate the daily report.
        /// </summary>
        /// <param name="logName">The name of the log file blob.</param>
        /// <param name="log">The writer for logging.</param>
        [NoAutomaticTrigger]
        public static void SendDailyReport(
            string logName,
            [Blob("container/{logName}.txt")] TextWriter log)
        {
            var env = EnvironmentDefinition.Instance;

            log.WriteLine($"{env.Name}: Daily Report WebJob starting at {DateTime.UtcNow.ToString()}");

            DateTime utcNow = DateTime.UtcNow;

            foreach (var clubId in env.MapClubIdToClubInfo.Keys)
            {
                var clubInfo = env.MapClubIdToClubInfo[clubId];

                if (clubInfo.DailyReportGmtHour == utcNow.Hour || env.IsLocal)
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

            log.WriteLine($"{env.Name}: Daily Report WebJob complete at {DateTime.UtcNow.ToString()}");
        }

        /// <summary>
        /// Generates a daily report of usage and policy violations and emails it to the configured recipients.
        /// </summary>
        /// <param name="clubId">The club of interest</param>
        /// <param name="log">The writer for logging.</param>
        /// <returns>A task that completes when the daily report has been generated and sent.</returns>
        private static async Task RunDailyReport(string clubId, TextWriter log)
        {
            var clubInfo = EnvironmentDefinition.Instance.MapClubIdToClubInfo[clubId];

            var client = new BookedSchedulerRetryClient(clubId, false);

            await client.SignInAsync(clubInfo.UserName, clubInfo.Password);

            var body = new StringBuilder();

            body.AppendLine($"<h2>BoatTracker Daily Report for: {clubInfo.Name}</h2>");

            await BuildReservationsReport(body, client, clubId, log);
            await BuildResourcesReport(body, client, clubInfo, log);

            log.Write(body.ToString());

            await SendDailyReportEmail(
                log,
                clubInfo,
                $"BoatTracker daily report for {clubInfo.Name}" + (EnvironmentDefinition.Instance.IsDevelopment ? " (DEV)" : string.Empty),
                body.ToString());
        }

        private static async Task BuildReservationsReport(StringBuilder body, BookedSchedulerRetryClient client, string clubId, TextWriter log)
        {
            // Get all reservations for the last day
            var reservations = await client.GetReservationsAsync(start: DateTime.UtcNow - TimeSpan.FromDays(1), end: DateTime.UtcNow);

            var compliant = new List<string>();
            var abandoned = new List<string>();
            var failedToCheckOut = new List<string>();
            var unknownParticipants = new List<string>();
            var withGuest = new List<string>();

            foreach (var reservation in reservations)
            {
                DateTime? checkInDate = reservation.CheckInDate();
                DateTime? checkOutDate = reservation.CheckOutDate();

                var user = await client.GetUserAsync(reservation.UserId());
                var boatName = reservation.ResourceName();

                var localStartTime = ConvertToLocalTime(user, reservation.StartDate()).ToShortTimeString();
                var localEndTime = ConvertToLocalTime(user, reservation.EndDate()).ToShortTimeString();

                string basicInfo = $"{user.FullName()} ({user.EmailAddress()}) - '{boatName}' @ {localStartTime} - {localEndTime}";

                if (checkInDate.HasValue)
                {
                    var localCheckInTime = ConvertToLocalTime(user, checkInDate.Value).ToShortTimeString();

                    if (checkOutDate.HasValue)
                    {
                        var localCheckOutTime = ConvertToLocalTime(user, checkOutDate.Value).ToShortTimeString();

                        compliant.Add($"{basicInfo} (actual: {localCheckInTime} - {localCheckOutTime})");
                    }
                    else
                    {
                        failedToCheckOut.Add($"{basicInfo} (actual: {localCheckInTime} - ??)");
                    }
                }
                else
                {
                    abandoned.Add(basicInfo);
                }

                var invitedGuests = reservation["invitedGuests"] as JArray ?? new JArray();
                var participatingGuests = reservation["participatingGuests"] as JArray ?? new JArray();

                // Get the "full" reservation to make sure the participants list is given as an array
                var fullReservation = await client.GetReservationAsync(reservation.ReferenceNumber());
                var participants = (JArray)fullReservation["participants"];

                var boat = await client.GetResourceAsync(reservation.ResourceId());

                //
                // See if the number of recorded participants is less than the boat capacity. We always
                // have to add one for the reservation owner.
                //
                if (participants.Count + invitedGuests.Count + participatingGuests.Count + 1 < boat.MaxParticipants())
                {
                    unknownParticipants.Add(basicInfo);
                }

                // Check for reservations involving a guest rower
                if (invitedGuests.Count > 0 || participatingGuests.Count > 0)
                {
                    var guestEmail = invitedGuests.Count > 0 ? invitedGuests[0].Value<string>() : participatingGuests[0].Value<string>();
                    withGuest.Add($"{basicInfo} with {guestEmail}");
                }
            }

            // Get all reservations for the next two days
            var upcomingReservations = await client.GetReservationsAsync(start: DateTime.UtcNow, end: DateTime.UtcNow + TimeSpan.FromDays(2));
            var upcomingWithGuest = new List<string>();

            foreach (var reservation in upcomingReservations)
            {
                var invitedGuests = reservation["invitedGuests"] as JArray ?? new JArray();
                var participatingGuests = reservation["participatingGuests"] as JArray ?? new JArray();

                if (invitedGuests.Count > 0 || participatingGuests.Count > 0)
                {
                    DateTime? checkInDate = reservation.CheckInDate();
                    DateTime? checkOutDate = reservation.CheckOutDate();

                    var user = await client.GetUserAsync(reservation.UserId());
                    var boatName = reservation.ResourceName();

                    var localStartDate = ConvertToLocalTime(user, reservation.StartDate()).ToShortDateString();
                    var localStartTime = ConvertToLocalTime(user, reservation.StartDate()).ToShortTimeString();
                    var localEndTime = ConvertToLocalTime(user, reservation.EndDate()).ToShortTimeString();

                    string basicInfo = $"{user.FullName()} ({user.EmailAddress()}) - '{boatName}' @ {localStartDate} {localStartTime} - {localEndTime}";

                    var guestEmail = invitedGuests.Count > 0 ? invitedGuests[0].Value<string>() : participatingGuests[0].Value<string>();
                    upcomingWithGuest.Add($"{basicInfo} with {guestEmail}");
                }
            }

            body.AppendLine($"<p>Total reservations: {reservations.Count}</p>");

            AddSectionToReport(body, compliant, "Compliant reservations");
            AddSectionToReport(body, abandoned, "Unused reservations");
            AddSectionToReport(body, failedToCheckOut, "Unclosed reservations");
            AddSectionToReport(body, unknownParticipants, "Incomplete rosters");
            AddSectionToReport(body, withGuest, "Guest rowers");
            AddSectionToReport(body, upcomingWithGuest, "Upcoming guest rowers");
        }

        private static async Task BuildResourcesReport(StringBuilder body, BookedSchedulerRetryClient client, ClubInfo clubInfo, TextWriter log)
        {
            var resources = await client.GetResourcesAsync();

            var noCapacity = new List<string>();
            var requiresCheckInNotSet = new List<string>();
#if false
            var invalidMinimumReservation = new List<string>();
#endif

            foreach (var resource in resources)
            {
                var name = resource.Name();

                var capacity = resource["maxParticipants"];
                if (capacity == null || string.IsNullOrEmpty(capacity.Value<string>()))
                {
                    noCapacity.Add(name);
                }

                var requiresCheckIn = resource["requiresCheckIn"];
                if (requiresCheckIn == null || requiresCheckIn.Value<int>() == 0)
                {
                    requiresCheckInNotSet.Add(name);
                }

#if false
                var minLength = resource["minLength"];
                if (minLength == null || !minLength.Value<string>().StartsWith(clubInfo.MinimumDuration.Minutes.ToString()))
                {
                    invalidMinimumReservation.Add(name);
                }
#endif
            }

            if (noCapacity.Count() > 0 || /* invalidMinimumReservation.Count() > 0 || */ requiresCheckInNotSet.Count() > 0)
            {
                body.AppendLine("<br/><p>Boat configuration warnings:</p>");

                AddSectionToReport(body, noCapacity, "No configured capacity");
                AddSectionToReport(body, requiresCheckInNotSet, "Requires checkin/checkout not set");
#if false
                AddSectionToReport(body, invalidMinimumReservation, "Minimum reservation time doesn't match club setting:");
#endif
            }
        }

        /// <summary>
        /// Adds a set of reservations to the email body we're building.
        /// </summary>
        /// <param name="sb">Buffer containing the generated email body.</param>
        /// <param name="itemList">The set of reservation descriptions to be added.</param>
        /// <param name="listName">The name of the list.</param>
        private static void AddSectionToReport(StringBuilder sb, List<string> itemList, string listName)
        {
            if (itemList.Count > 0)
            {
                sb.AppendLine($"<p>{listName} ({itemList.Count}):<br/>");
                foreach (var s in itemList)
                {
                    sb.AppendLine(s);
                    sb.AppendLine("<br/>");
                }

                sb.AppendLine("</p>");
            }
            else
            {
                sb.AppendLine($"<p>No {listName.ToLower()}.</p>");
            }
        }

        /// <summary>
        /// Sends the generated report.
        /// </summary>
        /// <param name="log">The writer for logging.</param>
        /// <param name="clubInfo">Info about the club of interest</param>
        /// <param name="subject">The email subject</param>
        /// <param name="body">The email body</param>
        /// <returns>A task that completes when the email has been sent.</returns>
        private static async Task SendDailyReportEmail(TextWriter log, ClubInfo clubInfo, string subject, string body)
        {
            var client = new SendGridClient(EnvironmentDefinition.Instance.SendGridApiKey);

            EmailAddress from = new EmailAddress(clubInfo.DailyReportSender);

            string[] recipients = new string[0];

            if (EnvironmentDefinition.Instance.IsDevelopment)
            {
                recipients = new string[] { CloudConfigurationManager.GetSetting("DeveloperEmail", false) };
            }
            else if (EnvironmentDefinition.Instance.IsProduction)
            {
                recipients = clubInfo.DailyReportRecipients.Split(',');
            }

            foreach (var recipient in recipients)
            {
                try
                {
                    EmailAddress to = new EmailAddress(recipient);
                    SendGridMessage mail = new SendGridMessage
                    {
                        From = from,
                        Subject = subject,
                        HtmlContent = body
                    };
                    mail.AddTo(to);

                    var response = await client.SendEmailAsync(mail);

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
            var mappedTz = mappings.FirstOrDefault(x => x.TzdbIds.Any(z => z.Equals(user.Timezone(), StringComparison.OrdinalIgnoreCase)));

            if (mappedTz == null)
            {
                return TimeSpan.Zero;
            }

            var timezoneInfo = TimeZoneInfo.FindSystemTimeZoneById(mappedTz.WindowsId);

            TimeSpan offset = timezoneInfo.GetUtcOffset(date);

            return offset;
        }

#endregion
    }
}
