using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

using BoatTracker.Bot.Configuration;

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Connector;

using Activity = Microsoft.Bot.Connector.Activity;

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
            try
            {
                if (activity != null)
                {
                    switch (activity.GetActivityType())
                    {
                        case ActivityTypes.Message:
                            if (!string.IsNullOrEmpty(activity.Text))
                            {
                                // HACK!!
                                // LUIS thinks "#" characters are special, so tokens like "#1" get screwed up
                                // as they pass through to the BoatTracker dialog. To work around that, we
                                // transform them into a unique string that will be ignored, and considered to
                                // be a boatName entity. Later, we restore it to its original form.
                                var pattern = @"#(?<suffix>[0-9]*)\s";
                                activity.Text = Regex.Replace(activity.Text, pattern, @"xYZzy${suffix} ");

                                try
                                {
                                    await Conversation.SendAsync(activity, () => new BoatTrackerDialog(this.LuisService));
                                }
                                catch (Microsoft.Rest.ValidationException)
                                {
                                    // Try to work around an intermittent ValidationException problem by retrying...
                                    Trace.TraceWarning($"Caught Microsoft.Rest.ValidationException - retrying...");

                                    await Task.Delay(500);
                                    await Conversation.SendAsync(activity, () => new BoatTrackerDialog(this.LuisService));
                                }
                                catch (Exception)
                                {
                                    throw;
                                }
                            }
                            else
                            {
                                using (var connector = new ConnectorClient(new Uri(activity.ServiceUrl)))
                                {
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
                            }

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
            catch (Exception ex)
            {
                // In the dev environment, we return exception for debugging purposes and
                // to diagnose test case failures more easily.
                if (EnvironmentDefinition.Instance.IsDevelopment)
                {
                    using (var connector = new ConnectorClient(new Uri(activity.ServiceUrl)))
                    {
                        await connector.Conversations.ReplyToActivityAsync(activity.CreateReply($"{ex.GetType().Name}: {ex.Message}"));
                        await connector.Conversations.ReplyToActivityAsync(activity.CreateReply($"Exception: {ex.StackTrace}"));
                    }
                }

                throw;
            }
        }
    }
}