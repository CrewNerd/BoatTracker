using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using BoatTracker.BookedScheduler;
using BoatTracker.Bot.Configuration;
using BoatTracker.Bot.DataObjects;
using BoatTracker.Bot.Utils;

namespace BoatTracker.Bot.Controllers
{
    [Route("api/rfid/events")]
    public class RfidEventsController : ApiController
    {
        private TelemetryClient telemetryClient;

        [ResponseType((typeof(void)))]
        public async Task<HttpResponseMessage> Post()
        {
            try
            {
                this.telemetryClient = new TelemetryClient();
                var env = EnvironmentDefinition.Instance;

                var body = await this.Request.Content.ReadAsStringAsync();
                var rfidEvents = JsonConvert.DeserializeObject<List<RfidEvent>>(body);

                foreach (var ev in rfidEvents)
                {
                    var clubId = ev.Location.ToLower();

                    if (string.IsNullOrEmpty(clubId) || !env.MapClubIdToClubInfo.ContainsKey(clubId))
                    {
                        this.telemetryClient.TrackTrace($"Invalid RFID location: {clubId}", SeverityLevel.Error);
                    }

                    var cache = BookedSchedulerCache.Instance[clubId];

                    if (!await cache.IsEventRedundantAsync(ev))
                    {
                        await this.ProcessEvent(cache, ev);
                    }
                }

                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex);
                throw;
            }
        }

        private async Task ProcessEvent(BookedSchedulerCache.BookedSchedulerCacheEntry cache, RfidEvent ev)
        {
            var boat = await cache.GetResourceFromRfidTagAsync(ev.EPC);
            var makerChannelKey = (await cache.GetBotUserAsync()).MakerChannelKey();

            string doorName = ev.ReadZone;

            this.LogBoatEvent(boat, doorName, ev);

            if (boat != null)
            {
                var iftttEvent = ev.Direction == "OUT" ? "boat_out" : "boat_in";

                await this.SendIftttTrigger(ev.Location, makerChannelKey, iftttEvent, ev.ReadTime.Value.ToString(), boat.Name(), doorName);

                if (boat.IsPrivate())
                {
                    await this.NotifyBoatOwners(cache, boat, ev);
                }

                //
                // TODO: Looks for a reservation to see if we can just annotate it with in/out times.
                // Otherwise, we need to create a reservation with an "unknown" rower to log the usage.
                //
            }
            else
            {
                // Tag isn't associated with a boat yet.
                await this.SendIftttTrigger(ev.Location, makerChannelKey, "new_tag", ev.ReadTime.Value.ToString(), ev.EPC, doorName);
            }
        }

        private async Task NotifyBoatOwners(BookedSchedulerCache.BookedSchedulerCacheEntry cache, JToken boat, RfidEvent ev)
        {
            var iftttEvent = ev.Direction == "OUT" ? "boat_out" : "boat_in";

            var botUserState = await cache.GetBotUserStateAsync();

            // We consider anyone who has permission to use a private boat to be an owner.
            var owners = (await cache.GetUsersAsync())
                .Where(u => botUserState.HasPermissionForResourceAsync(boat, u.Id()).Result);

            foreach (var owner in owners)
            {
                if (owner.Id() == botUserState.UserId)
                {
                    // Skip the botUser since they will appear to be an owner of everything, and we handle
                    // them separately anyway.
                    continue;
                }

                if (!string.IsNullOrEmpty(owner.MakerChannelKey()))
                {
                    await this.SendIftttTrigger(ev.Location, owner.MakerChannelKey(), iftttEvent, ev.ReadTime.Value.ToString(), boat.Name(), ev.ReadZone);
                }
            }

            //
            // TODO: Send a proactive message to each user as well.
            // https://docs.microsoft.com/en-us/bot-framework/dotnet/bot-builder-dotnet-proactive-messages
            //
        }

        private async Task SendIftttTrigger(
            string clubId,
            string channelKey,
            string eventName,
            string data1 = null,
            string data2 = null,
            string data3 = null)
        {
            if (!string.IsNullOrEmpty(channelKey))
            {
                string triggerUrl = $"https://maker.ifttt.com/trigger/{eventName}/with/key/{channelKey}";

                if (data1 != null)
                {
                    triggerUrl += $"?value1={Uri.EscapeUriString(data1)}";
                }

                if (data2 != null)
                {
                    triggerUrl += $"&value2={Uri.EscapeUriString(data2)}";
                }

                if (data3 != null)
                {
                    triggerUrl += $"&value3={Uri.EscapeUriString(data3)}";
                }

                try
                {
                    using (var client = new HttpClient())
                    {
                        await client.GetAsync(triggerUrl);
                    }
                }
                catch (Exception ex)
                {
                    this.telemetryClient.TrackException(
                        ex,
                        new Dictionary<string, string>
                        {
                            ["clubId"] = clubId,
                            ["eventName"] = eventName
                        });
                }
            }
        }

#if false
        private ClubInfo ValidateRequest()
        {
            if (this.Request.Headers.Authorization.Scheme.ToLower() != "basic")
            {
                throw new HttpResponseException(HttpStatusCode.Unauthorized);
            }

            var authParam = Encoding.UTF8.GetString(Convert.FromBase64String(this.Request.Headers.Authorization.Parameter));

            if (!authParam.Contains(":"))
            {
                throw new HttpResponseException(HttpStatusCode.Unauthorized);
            }

            var authParams = authParam.Split(':');

            var clubId = authParams[0];
            var password = authParams[1];

            var env = EnvironmentDefinition.Instance;

            if (!env.MapClubIdToClubInfo.ContainsKey(clubId))
            {
                throw new HttpResponseException(HttpStatusCode.Unauthorized);
            }

            var clubInfo = env.MapClubIdToClubInfo[clubId];

            if (clubInfo == null || string.Compare(password, clubInfo.RfidPassword) != 0)
            {
                throw new HttpResponseException(HttpStatusCode.Unauthorized);
            }

            return clubInfo;
        }
#endif

        private void LogBoatEvent(JToken boat, string doorName, RfidEvent ev)
        {
            if (boat != null)
            {
                this.telemetryClient.TrackEvent(
                    ev.Direction,
                    new Dictionary<string, string>
                    {
                        ["ClubId"] = ev.Location,
                        ["Timestamp"] = ev.ReadTime.Value.ToString(),
                        ["TagId"] = ev.EPC,
                        ["BoatName"] = boat.Name(),
                        ["DoorName"] = doorName
                    });
            }
            else
            {
                this.telemetryClient.TrackEvent(
                    "new_tag",
                    new Dictionary<string, string>
                    {
                        ["ClubId"] = ev.Location,
                        ["Timestamp"] = ev.ReadTime.Value.ToString(),
                        ["DoorName"] = doorName,
                        ["TagId"] = ev.EPC,
                    });
            }
        }
    }
}
