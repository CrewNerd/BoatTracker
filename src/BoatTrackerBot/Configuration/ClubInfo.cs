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
        /// Gets or sets the list of names associated with the reader antennas.
        /// </summary>
        [JsonProperty("doorNames")]
        public IReadOnlyList<string> DoorNames { get; set; }

        [JsonProperty("rfidPassword")]
        public string RfidPassword { get; set; }
    }
}