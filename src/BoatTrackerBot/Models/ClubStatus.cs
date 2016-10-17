using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using BoatTracker.BookedScheduler;
using BoatTracker.Bot.Configuration;
using BoatTracker.Bot.DataObjects;
using BoatTracker.Bot.Utils;

using Newtonsoft.Json.Linq;

namespace BoatTracker.Bot.Models
{
    public class ClubStatus
    {
        public ClubStatus(string clubId)
        {
            this.ClubId = clubId;
        }

        public async Task LoadDataAsync()
        {
            var clubInfo = EnvironmentDefinition.Instance.MapClubIdToClubInfo[this.ClubId];

            // We only need the timezone for the user.
            this.BotUserState = await BookedSchedulerCache.Instance[this.ClubId].GetBotUserStateAsync();

            var client = new BookedSchedulerClient(clubInfo.Url);
            await client.SignIn(clubInfo.UserName, clubInfo.Password);
            var reservations = await client.GetReservationsAsync(
                start: this.BotUserState.ConvertToLocalTime(DateTime.UtcNow).Date,
                end: this.BotUserState.ConvertToLocalTime(DateTime.UtcNow).Date + TimeSpan.FromDays(1));

            this.Reservations = reservations;

        }

        public string ClubId { get; private set; }

        public JArray Reservations { get; private set; }

        public UserState BotUserState { get; private set; }

        public ClubInfo ClubInfo
        {
            get
            {
                return EnvironmentDefinition.Instance.MapClubIdToClubInfo[this.ClubId];
            }
        }

        private bool UseFasterPageRefresh
        {
            get
            {
                var clubInfo = EnvironmentDefinition.Instance.MapClubIdToClubInfo[this.ClubId];

                var localTime = this.BotUserState.LocalTime();

                // Refresh more frequently during business hours (or within 30 minutes of business hours)
                return
                    localTime.TimeOfDay.TotalHours + 0.5 >= (clubInfo.EarliestUseHour ?? 5) &&
                    localTime.TimeOfDay.TotalHours - 0.5< (clubInfo.LatestUseHour ?? 21);
            }
        }

        public string PageRefreshTime
        {
            get
            {
                return this.UseFasterPageRefresh ? "60" : "600";
            }
        }

        public IEnumerable<JToken> UpcomingReservations
        {
            get
            {
                return this.Reservations
                    .Where(r =>
                    {
                        var startDateTime = r.Value<DateTime>("startDate");
                        var endDateTime = r.Value<DateTime>("endDate");

                        return
                            r.CheckInDate() == null &&
                            endDateTime > DateTime.UtcNow &&
                            DateTime.UtcNow < startDateTime + TimeSpan.FromMinutes(15) &&
                            startDateTime < DateTime.UtcNow + TimeSpan.FromHours(18);
                    });
            }
        }

        public IEnumerable<JToken> OnTheWaterReservations
        {
            get
            {
                return this.Reservations
                    .Where(r => !string.IsNullOrEmpty(r.Value<string>("checkInDate")) && string.IsNullOrEmpty(r.Value<string>("checkOutDate")))
                    .Where(r =>
                    {
                        var endDateTime = r.Value<DateTime>("endDate");

                        return DateTime.UtcNow < endDateTime;
                    });
            }
        }

        public IEnumerable<JToken> OverdueReservations
        {
            get
            {
                return this.Reservations
                    .Where(r => !string.IsNullOrEmpty(r.Value<string>("checkInDate")) && string.IsNullOrEmpty(r.Value<string>("checkOutDate")))
                    .Where(r =>
                    {
                        var endDateTime = r.Value<DateTime>("endDate");

                        return DateTime.UtcNow > endDateTime;
                    });
            }
        }
    }
}