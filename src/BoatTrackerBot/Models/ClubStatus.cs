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
    /// <summary>
    /// Model object for the Club Status page view.
    /// </summary>
    public class ClubStatus
    {
        /// <summary>
        /// Initializes a new instance of the ClubStatus class.
        /// </summary>
        /// <param name="clubId">The ID of the club.</param>
        public ClubStatus(string clubId)
        {
            this.ClubId = clubId;
        }

        /// <summary>
        /// Gets the club id.
        /// </summary>
        public string ClubId { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether we received a valid clubStatusSecret,
        /// meaning the call came from a kiosk. Otherwise, we don't display the checkin
        /// and checkout buttons.
        /// </summary>
        public bool IsKiosk { get; set; }

        /// <summary>
        /// Gets the set of reservations to be filtered down for the status view.
        /// </summary>
        public JArray Reservations { get; private set; }

        /// <summary>
        /// Gets the UserState object for the bot user (for time zone calculations).
        /// </summary>
        public UserState BotUserState { get; private set; }

        /// <summary>
        /// Gets the configuration data for the club.
        /// </summary>
        public ClubInfo ClubInfo
        {
            get
            {
                return EnvironmentDefinition.Instance.MapClubIdToClubInfo[this.ClubId];
            }
        }

        /// <summary>
        /// Gets a value indicating whether we should use the fast page refresh interval.
        /// </summary>
        private bool UseFasterPageRefresh
        {
            get
            {
                var clubInfo = EnvironmentDefinition.Instance.MapClubIdToClubInfo[this.ClubId];

                var localTime = this.BotUserState.LocalTime();

                // Refresh more frequently during business hours (or within 30 minutes of business hours)
                return
                    localTime.TimeOfDay.TotalHours + 0.5 >= (clubInfo.EarliestUseHour ?? 5) &&
                    localTime.TimeOfDay.TotalHours - 0.5 < (clubInfo.LatestUseHour ?? 21);
            }
        }

        /// <summary>
        /// Gets the page refresh time in seconds.
        /// </summary>
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

        /// <summary>
        /// Gets the set of upcoming reservations
        /// </summary>
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

                        // TODO: This assumes that everyone uses the 15-minute expiration time, but this can
                        // be configured separately for each reservation. We should pull the expiration from
                        // the resource and use that here. Check to see if the value gets put in the reservation
                        // since that would make life a lot simpler here. Maybe make this a per-club setting.

                        return
                            localTime.Date == startDateTime.Date &&                     // current day
                            r.CheckInDate() == null &&                                  // not checked in
                            localTime < startDateTime + TimeSpan.FromMinutes(15) &&     // not expired
                            startDateTime < localTime + TimeSpan.FromHours(4);          // < 4 hours in future
                    });
            }
        }

        /// <summary>
        /// Gets the set of reservations that are checked in and on the water now.
        /// </summary>
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

        /// <summary>
        /// Gets the set of reservations that should have checked out by now.
        /// </summary>
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
                this.BotUserState = new UserState { ClubId = this.ClubId, UserId = 1 };
                return $"Please wait while the BoatTracker service initializes. This page will automatically refresh in one minute.";
            }

            // We only need the timezone for the user.
            this.BotUserState = await BookedSchedulerCache.Instance[this.ClubId].GetBotUserStateAsync();

            BookedSchedulerRetryClient client = null;

            try
            {
                client = new BookedSchedulerRetryClient(this.ClubId, true);
                await client.SignInAsync(clubInfo.UserName, clubInfo.Password);

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
                        await client.SignOutAsync();
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