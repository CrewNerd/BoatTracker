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

            string doorName = "Unknown";

            if (ev.Antenna < this.currentClub.DoorNames.Count)
            {
                doorName = this.currentClub.DoorNames[ev.Antenna];
            }

            this.LogBoatEvent(boat, doorName, ev);

            var makerChannelKey = (await this.bsCache.GetBotUserAsync()).GetMakerChannelKey();
            if (!string.IsNullOrEmpty(makerChannelKey))
            {
                await this.SendBoatEventTrigger(boat, doorName, makerChannelKey, ev);
            }
        }

        private async Task SendBoatEventTrigger(JToken boat, string doorName, string channelKey, RfidEvent ev)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    await client.GetAsync(
                        string.Format(
                            "https://maker.ifttt.com/trigger/{0}/with/key/{1}?value1={2}&value2={3}&value3={4}",
                            ev.EventType,
                            channelKey,
                            Uri.EscapeUriString(ev.Timestamp.Value.ToString()),
                            Uri.EscapeUriString(boat.Name()),
                            Uri.EscapeUriString(doorName)));
                }
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex, new Dictionary<string, string> { ["clubId"] = this.currentClub.Id });
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
            telemetryClient.TrackEvent(ev.EventType, new Dictionary<string, string>
            {
                ["ClubId"] = this.currentClub.Id,
                ["Timestamp"] = ev.Timestamp.Value.ToString(),
                ["TagId"] = ev.Id,
                ["BoatName"] = boat.Name(),
                ["DoorName"] = doorName
            });
        }
    }
}
