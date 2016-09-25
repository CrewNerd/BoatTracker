using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BoatTracker.Bot.DataObjects
{
    public class RfidEvent
    {
        public const string EventTypeIn = "boat_in";
        public const string EventTypeOut = "boat_out";

        [JsonProperty("timestamp", ItemConverterType = typeof(JavaScriptDateTimeConverter))]
        public DateTime? Timestamp { get; set; }

        [JsonProperty("antenna")]
        public int Antenna { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("eventType")]
        public string EventType { get; set; }
    }
}