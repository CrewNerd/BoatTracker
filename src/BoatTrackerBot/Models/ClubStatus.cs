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
                TimeSpan refreshTime;

                if (EnvironmentDefinition.Instance.IsProduction)
                {
                    refreshTime = TimeSpan.FromMinutes(this.UseFasterPageRefresh ? 1 : 10);
                }
                else
                {
                    // Use a long refresh time to facilitate debugging
                    refreshTime = TimeSpan.FromHours(1);
                }

                return refreshTime.TotalSeconds.ToString();
            }
        }

        public IEnumerable<JToken> UpcomingReservations
        {
            get
            {
                var localTime = this.BotUserState.LocalTime();

                return this.Reservations
                    .Where(r =>
                    {
                        var startDateTime = this.BotUserState.ConvertToLocalTime(r.StartDate());
                        var endDateTime = this.BotUserState.ConvertToLocalTime(r.EndDate());

                        return
                            localTime.Date == startDateTime.Date &&                     // current day
                            r.CheckInDate() == null &&                                  // not checked in
                            localTime < startDateTime + TimeSpan.FromMinutes(15) &&     // not expired
                            startDateTime < localTime + TimeSpan.FromHours(4);          // < 4 hours in future
                    });
            }
        }

        public IEnumerable<JToken> OnTheWaterReservations
        {
            get
            {
                var localTime = this.BotUserState.LocalTime();

                return this.Reservations
                    .Where(r => r.CheckInDate().HasValue && !r.CheckOutDate().HasValue)
                    .Where(r =>
                    {
                        var startDateTime = this.BotUserState.ConvertToLocalTime(r.StartDate());
                        var endDateTime = this.BotUserState.ConvertToLocalTime(r.EndDate());

                        return
                            startDateTime.Date == localTime.Date &&     // current day
                            localTime < endDateTime;                    // not yet overdue
                    });
            }
        }

        public IEnumerable<JToken> OverdueReservations
        {
            get
            {
                var localTime = this.BotUserState.LocalTime();

                return this.Reservations
                    .Where(r => r.CheckInDate().HasValue && !r.CheckOutDate().HasValue)
                    .Where(r =>
                    {
                        var startDateTime = this.BotUserState.ConvertToLocalTime(r.StartDate());
                        var endDateTime = this.BotUserState.ConvertToLocalTime(r.EndDate());

                        return
                            startDateTime.Date == localTime.Date &&     // current day
                            localTime > endDateTime;                    // overdue
                    });
            }
        }

        /// <summary>
        /// Loads the reservations to be displayed, and optionally performs a checkin or checkout
        /// if the corresponding reservation reference id is provided. The checkin/checkout comes
        /// first so we retrieve the reservation list AFTER those changes are applied.
        /// </summary>
        /// <param name="checkin">Optional reference id of a reservation to check in</param>
        /// <param name="checkout">Optional reference id of a reservation to check out</param>
        /// <returns>Task returning an error message (or null) from the checkin or checkout</returns>
        public async Task<string> LoadDataAsync(string checkin, string checkout)
        {
            var clubInfo = EnvironmentDefinition.Instance.MapClubIdToClubInfo[this.ClubId];

            if (!BookedSchedulerCache.Instance[this.ClubId].IsInitialized)
            {
                this.Reservations = new JArray();
                return $"Please wait while the BoatTracker service initializes. This page will automatically refresh in one minute.";
            }

            // We only need the timezone for the user.
            this.BotUserState = await BookedSchedulerCache.Instance[this.ClubId].GetBotUserStateAsync();

            BookedSchedulerLoggingClient client = null;

            try
            {
                client = new BookedSchedulerLoggingClient(this.ClubId, true);
                await client.SignIn(clubInfo.UserName, clubInfo.Password);

                string message = null;

                if (!string.IsNullOrEmpty(checkin))
                {
                    try
                    {
                        await client.CheckInReservationAsync(checkin);
                    }
                    catch (Exception ex)
                    {
                        message = $"Checkin failed: {ex.Message}";
                    }
                }
                else if (!string.IsNullOrEmpty(checkout))
                {
                    try
                    {
                        await client.CheckOutReservationAsync(checkout);
                    }
                    catch (Exception ex)
                    {
                        message = $"Checkout failed: {ex.Message}";
                    }
                }

                // TODO: figure out how to narrow this down
                var reservations = await client.GetReservationsAsync(
                    start: DateTime.UtcNow - TimeSpan.FromDays(1),
                    end: DateTime.UtcNow + TimeSpan.FromDays(1));

                this.Reservations = reservations;

                return message;
            }
            catch (Exception ex)
            {
                this.Reservations = new JArray();
                return $"Unable to fetch reservations, currently. This page will automatically refresh in one minute. ({ex.Message})";
            }
            finally
            {
                if (client != null && client.IsSignedIn)
                {
                    try
                    {
                        await client.SignOut();
                    }
                    catch (Exception)
                    {
                        // best effort only
                    }
                }
            }
        }
    }
}