using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

using Microsoft.ApplicationInsights;
using Newtonsoft.Json.Linq;

using BoatTracker.BookedScheduler;
using BoatTracker.Bot.DataObjects;
using BoatTracker.Bot.Configuration;
using BoatTracker.Bot.Utils;
using System.Web;

namespace BoatTracker.Bot.Controllers
{
    [Route("api/rfid/events")]
    public class RfidEventsController : ApiController
    {
        private ClubInfo currentClub;
        private BookedSchedulerCache.BookedSchedulerCacheEntry bsCache;
        private TelemetryClient telemetryClient;

        [ResponseType((typeof(void)))]
        public async Task<HttpResponseMessage> Post([FromBody]RfidEvents rfidEvents)
        {
            this.currentClub = this.ValidateRequest();
            this.bsCache = BookedSchedulerCache.Instance[this.currentClub.Id];
            this.telemetryClient = new TelemetryClient();

            if (rfidEvents == null)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            foreach (var ev in rfidEvents.Events)
            {
                if (!await this.bsCache.IsEventRedundantAsync(ev))
                {
                    await this.ProcessEvent(ev);
                }
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        private async Task ProcessEvent(RfidEvent ev)
        {
            var boat = await this.bsCache.GetResourceFromRfidTagAsync(ev.Id);
            var makerChannelKey = (await this.bsCache.GetBotUserAsync()).MakerChannelKey();

            string doorName = "Unknown";

            if (ev.Antenna < this.currentClub.DoorNames.Count)
            {
                doorName = this.currentClub.DoorNames[ev.Antenna];
            }

            this.LogBoatEvent(boat, doorName, ev);

            if (boat == null)
            {
                await this.SendIftttTrigger(makerChannelKey, "new_tag", ev.Timestamp.Value.ToString(), ev.Id, doorName);
                return;
            }

            await this.SendIftttTrigger(makerChannelKey, ev.EventType, ev.Timestamp.Value.ToString(), boat.Name(), doorName);

            //
            // TODO: Looks for a reservation to see if we can just annotate it with in/out times.
            // Otherwise, we need to create a reservation with an "unknown" rower to log the usage.
            //
        }

        private async Task SendIftttTrigger(string channelKey, string eventName, string data1 = null, string data2 = null, string data3 = null)
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
                    this.telemetryClient.TrackException(ex, new Dictionary<string, string>
                    {
                        ["clubId"] = this.currentClub.Id,
                        ["eventName"] = eventName
                    });
                }
            }
        }

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

        private void LogBoatEvent(JToken boat, string doorName, RfidEvent ev)
        {
            if (boat != null)
            {
                telemetryClient.TrackEvent(ev.EventType, new Dictionary<string, string>
                {
                    ["ClubId"] = this.currentClub.Id,
                    ["Timestamp"] = ev.Timestamp.Value.ToString(),
                    ["TagId"] = ev.Id,
                    ["BoatName"] = boat.Name(),
                    ["DoorName"] = doorName
                });
            }
            else
            {
                telemetryClient.TrackEvent("new_tag", new Dictionary<string, string>
                {
                    ["ClubId"] = this.currentClub.Id,
                    ["Timestamp"] = ev.Timestamp.Value.ToString(),
                    ["DoorName"] = doorName,
                    ["TagId"] = ev.Id,
                });
            }
        }
    }
}
