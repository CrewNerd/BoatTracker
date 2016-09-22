using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace BoatTracker.Bot.Configuration
{
    public class ClubConfiguration
    {
        [JsonProperty("clubs")]
        public IReadOnlyList<ClubInfo> Clubs { get; set; }
    }
}