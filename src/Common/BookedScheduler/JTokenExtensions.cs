using Newtonsoft.Json.Linq;

namespace BoatTracker.BookedScheduler
{
    public static class JTokenExtensions
    {
        public static long ResourceId(this JToken jtoken)
        {
            return jtoken.Value<long>("resourceId");
        }

        public static string Name(this JToken jtoken)
        {
            return jtoken.Value<string>("name");
        }

        public static string StartDate(this JToken jtoken)
        {
            return jtoken.Value<string>("startDate");
        }

        public static string ReferenceNumber(this JToken jtoken)
        {
            return jtoken.Value<string>("referenceNumber");
        }
    }
}