using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace BoatTracker.Bot.DataObjects
{
    public class RfidEvents
    {
        [JsonProperty("events")]
        public IReadOnlyList<RfidEvent> Events { get; set; }
    }
}