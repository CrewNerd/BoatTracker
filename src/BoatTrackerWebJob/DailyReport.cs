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

            log.WriteLine($"{env.Name}: Daily Report WebJob complete at {DateTime.UtcNow.ToString()}");
        }

        private static async Task RunDailyReport(string clubId, TextWriter log)
        {
            var clubInfo = EnvironmentDefinition.Instance.MapClubIdToClubInfo[clubId];

            var client = new BookedSchedulerLoggingClient(clubId, false);

            await client.SignInAsync(clubInfo.UserName, clubInfo.Password);

            // Get all reservations for the last day
            var reservations = await client.GetReservationsAsync(start: DateTime.UtcNow - TimeSpan.FromDays(1), end: DateTime.UtcNow);

            var compliant = new List<string>();
            var abandoned = new List<string>();
            var noCheckOut = new List<string>();
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
                        noCheckOut.Add($"{basicInfo} (actual: {localCheckInTime} - ??)");
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

            await client.SignOutAsync();

            var sbMessage = new StringBuilder();

            sbMessage.AppendLine($"<h2>BoatTracker Daily Report for: {clubInfo.Name}</h2>");
            sbMessage.AppendLine($"<p>Total reservations: {reservations.Count}</p>");

            AddReservationsToReport(sbMessage, compliant, "Compliant reservations");
            AddReservationsToReport(sbMessage, abandoned, "Unused reservations");
            AddReservationsToReport(sbMessage, noCheckOut, "Unclosed reservations");
            AddReservationsToReport(sbMessage, unknownParticipants, "Incomplete rosters");
            AddReservationsToReport(sbMessage, withGuest, "Guest rowers");
            AddReservationsToReport(sbMessage, upcomingWithGuest, "Upcoming guest rowers");

            log.Write(sbMessage.ToString());

            await SendDailyReportEmail(
                log,
                clubInfo,
                $"BoatTracker daily report for {clubInfo.Name}" + (EnvironmentDefinition.Instance.IsDevelopment ? " (DEV)" : ""),
                sbMessage.ToString());
        }

        private static void AddReservationsToReport(StringBuilder sb, List<string> reservationList, string listName)
        {
            if (reservationList.Count > 0)
            {
                sb.AppendLine($"<p>{listName} ({reservationList.Count}):<br/>");
                foreach (var s in reservationList)
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
