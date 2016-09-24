using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Connector;

using BoatTracker.Bot.Configuration;

namespace BoatTracker.Bot
{
    [BotAuthentication]
    [Route("api/messages")]
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
                return new LuisService(
                    new LuisModelAttribute(
                        EnvironmentDefinition.Instance.LuisModelId,
                        EnvironmentDefinition.Instance.LuisSubscriptionKey));
            }
        }
    }
}