using System;
using System.Linq;
using System.Threading.Tasks;

using BoatTracker.BookedScheduler;
using BoatTracker.Bot.DataObjects;
using BoatTracker.Bot.Utils;

namespace BoatTracker.Bot.Configuration
{
    /// <summary>
    /// Represents the configuration necessary to operate in an environment. Some configuration is
    /// read from Azure service properties, but we bake as much into the code as possible. Child
    /// classes represent each deployment environment: local, development (cloud), and production.
    /// </summary>
    public static class EnvironmentDefinitionExtensions
    {
        public static async Task<UserState> TryBuildStateForUser(this EnvironmentDefinition config, string userBotId, string channel)
        {
            string botAccountKeyDisplayName = config.GetChannelInfo(channel).BotAccountKeyDisplayName;

            foreach (var clubId in config.MapClubIdToClubInfo.Keys)
            {
                var clubInfo = config.MapClubIdToClubInfo[clubId];

                BookedSchedulerClient client = new BookedSchedulerLoggingClient(clubId);

                await client.SignIn(clubInfo.UserName, clubInfo.Password);

                if (client.IsSignedIn)
                {
                    var users = await client.GetUsersAsync();

                    try
                    {
                        var user = users
                            .Where(u => u["customAttributes"]
                                .Where(attr => attr.Value<string>("label") == botAccountKeyDisplayName)
                                .First()
                                .Value<string>("value") == userBotId)
                            .FirstOrDefault();

                        if (user != null)
                        {
                            return new UserState
                            {
                                ClubId = clubId,
                                UserId = user.Id(),
                                Timezone = user.Value<string>("timezone")
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }

            return null;
        }
    }
}