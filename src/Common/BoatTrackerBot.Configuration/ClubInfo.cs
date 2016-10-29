using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace BoatTracker.Bot.Configuration
{
    /// <summary>
    /// Contains information about a club that we're configured to access
    /// </summary>
    public class ClubInfo
    {
        /// <summary>
        /// Gets or sets the id for the club.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the display name for the club.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the URL where we access the club's API.
        /// </summary>
        [JsonProperty("url")]
        public Uri Url { get; set; }

        /// <summary>
        /// Gets or sets the user name for accessing the club.
        /// </summary>
        [JsonProperty("userName")]
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the password we use to access our account.
        /// </summary>
        [JsonProperty("password")]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the hour (GMT) when the report should be generated.
        /// </summary>
        [JsonProperty("dailyReportGmtHour")]
        public int DailyReportGmtHour { get; set; }

        /// <summary>
        /// Gets or sets the email address from which the daily report is sent.
        /// </summary>
        [JsonProperty("dailyReportSender")]
        public string DailyReportSender { get; set; }

        /// <summary>
        /// Gets or sets a comma-separated list of email addresses to send the daily report to.
        /// </summary>
        [JsonProperty("dailyReportRecipients")]
        public string DailyReportRecipients { get; set; }

        /// <summary>
        /// Gets or sets the list of names associated with the reader antennas.
        /// </summary>
        [JsonProperty("doorNames")]
        public IReadOnlyList<string> DoorNames { get; set; }

        /// <summary>
        /// Gets or sets the password used by the RFID reader to push events to the cloud
        /// </summary>
        [JsonProperty("rfidPassword")]
        public string RfidPassword { get; set; }

        /// <summary>
        /// Gets or sets the earliest hour of the day when reservations may begin (can be fractional)
        /// </summary>
        [JsonProperty("earliestUseHour")]
        public float? EarliestUseHour { get; set; }

        /// <summary>
        /// Gets or sets the latest hour of operation (can be fractional)
        /// </summary>
        [JsonProperty("latestUseHour")]
        public float? LatestUseHour { get; set; }

        /// <summary>
        /// Gets or sets the minimum reservation duration in hours (can be fractional)
        /// </summary>
        [JsonProperty("minimumDurationHours")]
        public float? MinimumDurationHours { get; set; }

        /// <summary>
        /// Gets or sets the maximum reservation duration in hours (can be fractional)
        /// </summary>
        [JsonProperty("maximumDurationHours")]
        public float? MaximumDurationHours { get; set; }

        /// <summary>
        /// Gets or sets the early checkin window size, in minutes. Must match the configuration
        /// of the BookedScheduler site.
        /// </summary>
        [JsonProperty("earlyCheckinWindowInMinutes")]
        public int? EarlyCheckinWindowInMinutes { get; set; }

        #region Helpers

        [JsonIgnore]
        public TimeSpan EarliestUseTime
        {
            get
            {
                return TimeSpan.FromHours(this.EarliestUseHour ?? 5);
            }
        }

        [JsonIgnore]
        public TimeSpan LatestUseTime
        {
            get
            {
                return TimeSpan.FromHours(this.LatestUseHour ?? 21);
            }
        }

        [JsonIgnore]
        public TimeSpan MinimumDuration
        {
            get
            {
                return TimeSpan.FromHours(this.MinimumDurationHours ?? 0.5);
            }
        }

        [JsonIgnore]
        public TimeSpan MaximumDuration
        {
            get
            {
                return TimeSpan.FromHours(this.MaximumDurationHours ?? 3);
            }
        }

        #endregion
    }
}