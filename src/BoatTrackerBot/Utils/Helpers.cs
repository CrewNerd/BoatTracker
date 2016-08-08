using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;
using NodaTime.TimeZones;

using BoatTracker.Bot.Configuration;

namespace BoatTracker.Bot.Utils
{
    public static class Helpers
    {
        public static async Task<string> DescribeReservationsAsync(UserState userState, JArray reservations)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var reservation in reservations)
            {
                DateTime startDate = DateTime.Parse(reservation.Value<string>("startDate"));
                startDate = ConvertToLocalTime(userState, startDate);
                var duration = reservation.Value<string>("duration");

                var boatName = await BookedSchedulerCache
                    .Instance[userState.ClubId]
                    .GetResourceNameFromIdAsync(reservation.Value<long>("resourceId"));

                sb.AppendFormat("\r\n\r\n**{0} {1}** {2} *({3})*", startDate.ToLocalTime().ToString("d"), startDate.ToLocalTime().ToString("t"), boatName, duration);
            }

            return sb.ToString();
        }

        public static DateTime ConvertToLocalTime(UserState userState, DateTime dateTime)
        {
            var mappings = TzdbDateTimeZoneSource.Default.WindowsMapping.MapZones;
            var mappedTz = mappings.FirstOrDefault(x => x.TzdbIds.Any(z => z.Equals(userState.Timezone, StringComparison.OrdinalIgnoreCase)));

            if (mappedTz == null)
            {
                return dateTime;
            }

            var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(mappedTz.WindowsId);
            return dateTime + tzInfo.GetUtcOffset(dateTime);
        }

    }
}