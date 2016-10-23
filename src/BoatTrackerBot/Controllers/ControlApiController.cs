using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

using BoatTracker.Bot.Utils;
using BoatTracker.Bot.Configuration;

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
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
                }

                await BookedSchedulerCache.Instance.RefreshCacheAsync(clubId);
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
