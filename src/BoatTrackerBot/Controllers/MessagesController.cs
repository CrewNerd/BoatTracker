using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Connector;

using BoatTracker.Bot.Configuration;
using System.Security.Claims;

namespace BoatTracker.Bot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        [ResponseType((typeof(void)))]
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity != null)
            {
                Trace.TraceInformation("Message from: {0} / {1}", activity.From.Id, activity.From.Name);

                var claimsPrincipal = this.RequestContext.Principal as ClaimsPrincipal;

                if (claimsPrincipal != null)
                {
                    var identityClaim = claimsPrincipal.Claims.Where(c => c.Type == "aud").FirstOrDefault();

                    if (identityClaim != null)
                    {
                        Trace.TraceInformation("Requester Id: {0}", identityClaim.Value);
                    }
                }

                switch (activity.GetActivityType())
                {
                    case ActivityTypes.Message:
                        await Conversation.SendAsync(activity, () => new BoatTrackerDialog(this.LuisService));
                        break;

                    case ActivityTypes.Ping:
                        break;

                    case ActivityTypes.ConversationUpdate:
                    case ActivityTypes.ContactRelationUpdate:
                    case ActivityTypes.Typing:
                    case ActivityTypes.DeleteUserData:
                    default:
                        Trace.TraceError($"Unknown activity type ignored: {activity.GetActivityType()}");
                        break;
                }
            }

            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }

        private ILuisService LuisService
        {
            get
            {
                EnvironmentDefinition env = EnvironmentDefinition.CreateFromEnvironment();

                return new LuisService(new LuisModelAttribute(env.LuisModelId, env.LuisSubscriptionKey));
            }
        }
    }
}