using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace BoatTracker.Bot.Configuration
{
    /// <summary>
    /// Represents the entire club configuration file (an array of ClubInfo objects).
    /// </summary>
    public class ClubConfiguration
    {
        /// <summary>
        /// Gets or sets the array of ClubInfo objects.
        /// </summary>
        [JsonProperty("clubs")]
        public IReadOnlyList<ClubInfo> Clubs { get; set; }
    }
}