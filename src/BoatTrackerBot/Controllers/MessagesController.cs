using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

using BoatTracker.Bot.Configuration;

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Connector;

namespace BoatTracker.Bot
{
    [BotAuthentication]
    [Route("api/messages")]
    public class MessagesController : ApiController
    {
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

        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        /// <param name="activity">The activity received from the Bot Framework</param>
        /// <returns>We must return an Accepted status code on success.</returns>
        [ResponseType((typeof(void)))]
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity != null)
            {
                switch (activity.GetActivityType())
                {
                    case ActivityTypes.Message:
                        if (!string.IsNullOrEmpty(activity.Text))
                        {
                            await Conversation.SendAsync(activity, () => new BoatTrackerDialog(this.LuisService));
                        }
                        else
                        {
                            ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                            string replyText;

                            if (activity.Attachments.Any() && activity.Attachments.First().ContentType.Contains("mp4"))
                            {
                                replyText = "I'm sorry, but I don't know how to handle voice messages yet. I hope to be able to do that soon.";
                            }
                            else
                            {
                                replyText = "I'm sorry, but I don't understand this kind of message.";
                            }

                            Activity reply = activity.CreateReply(replyText);
                            await connector.Conversations.ReplyToActivityAsync(reply);
                        }

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
    }
}