using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

using BoatTracker.Bot.Configuration;
using BoatTracker.Bot.Utils;

namespace BoatTracker.Bot.Controllers
{
    [Route("api/control/{operation}")]
    public class ControlApiController : ApiController
    {
        [ResponseType((typeof(void)))]
        public async Task<HttpResponseMessage> Post(
            [FromUri]string operation,
            [FromUri]string clubId = null,
            [FromUri]string securityKey = null)
        {
            if (string.IsNullOrEmpty(securityKey) || securityKey != EnvironmentDefinition.Instance.SecurityKey)
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            if (operation.ToLower() == "refreshcache")
            {
                if (!string.IsNullOrEmpty(clubId) && !EnvironmentDefinition.Instance.MapClubIdToClubInfo.ContainsKey(clubId))
                {
                    // Unknown club id
                    Trace.TraceError($"Webjob attempted cache refresh for unknown club '{clubId}'");
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
                }

                try
                {
                    Trace.TraceInformation($"Webjob starting cache refresh for club '{clubId}'");
                    await BookedSchedulerCache.Instance.RefreshCacheAsync(clubId);
                    Trace.TraceInformation($"Webjob finished cache refresh for club '{clubId}'");
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Webjob cache refresh for club '{clubId}' failed: {ex.Message}");
                }
            }
            else
            {
                // Unknown operation
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
