using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Azure;

using BoatTracker.BookedScheduler;
using BoatTracker.Bot.Utils;
using Newtonsoft.Json;

namespace BoatTracker.Bot.Configuration
{
    /// <summary>
    /// Represents the configuration necessary to operate in an environment. Some configuration is
    /// read from Azure service properties, but we bake as much into the code as possible. Child
    /// classes represent each deployment environment: local, development (cloud), and production.
    /// </summary>
    public abstract class EnvironmentDefinition
    {
        private const string LuisModelIdKey = "LuisModelId";
        private const string LuisSubscriptionKeyKey = "LuisSubscriptionKey";

        /// <summary>
        /// The club ID list provides the internal id's of each BookedScheduler instance that we
        /// are configured to work with. The list is comma-separated with no spaces. When a new
        /// club is added, there are four additional properties that must also be set to provide
        /// the friendly name, the URL, the admin account for the bot to use, and the account
        /// password.
        /// </summary>
        private const string ClubIdListKey = "ClubIdList";

        // For the club details, we build the property key from a base followed by the club's ID.
        private const string ClubNameBaseKey = "ClubName_";
        private const string ClubUrlBaseKey = "ClubUrl_";
        private const string ClubUserNameBaseKey = "ClubUserName_";
        private const string ClubPasswordBaseKey = "ClubPassword_";

        static EnvironmentDefinition()
        {
            Instance = CreateFromEnvironment();
        }

        public static EnvironmentDefinition Instance { get; private set; }

        /// <summary>
        /// Gets the model ID for our LUIS service.
        /// </summary>
        public virtual string LuisModelId
        {
            get
            {
                return CloudConfigurationManager.GetSetting("LuisModelId");
            }
        }

        /// <summary>
        /// Gets the subscription key for our LUIS service
        /// </summary>
        public virtual string LuisSubscriptionKey
        {
            get
            {
                return CloudConfigurationManager.GetSetting("LuisSubscriptionKey");
            }
        }

        public virtual bool IsLocal { get { return false; } }

        public virtual bool IsDevelopment { get { return false; } }

        public virtual bool IsProduction { get { return false; } }

        /// <summary>
        /// Returns channel information for the given channel name.
        /// </summary>
        /// <param name="channel">The internal name of the channel</param>
        /// <returns>A ChannelInfo object with configuration information for the channel.</returns>
        public ChannelInfo GetChannelInfo(string channel)
        {
            ChannelInfo channelInfo;
            string devName = this.IsDevelopment ? " (dev)" : string.Empty;

            if (channel == "skype")
            {
                channelInfo = new ChannelInfo(
                    "Skype",
                    "Skype account key" + devName,
                    supportsButtons:true,
                    supportsMarkdown:true);
            }
            else if (channel == "facebook")
            {
                channelInfo = new ChannelInfo(
                    "Facebook Messenger",
                    "Facebook account key" + devName,
                    supportsButtons:false,
                    supportsMarkdown:false);
            }
            else if (channel == "sms")
            {
                channelInfo = new ChannelInfo(
                    "Text Message",
                    "Text message account key" + devName,
                    supportsButtons:false,
                    supportsMarkdown:false);
            }
            else
            {
                throw new ArgumentException("Invalid channel name: " + channel);
            }

            return channelInfo;
        }

        #region Clubs

        private Dictionary<string, ClubInfo> mapClubIdToClubInfo;

        /// <summary>
        /// Gets a mapping from club id to its ClubInfo object.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public virtual IReadOnlyDictionary<string, ClubInfo> MapClubIdToClubInfo
        {
            get
            {
                if (this.mapClubIdToClubInfo == null)
                {
                    // TODO: lock needed here.
                    var body = File.ReadAllText(@"d:\home\site\wwwroot\ClubConfiguration.json");
                    var config = JsonConvert.DeserializeObject<ClubConfiguration>(body);

                    this.mapClubIdToClubInfo = config.Clubs.ToDictionary(c => c.Id);
                }

                return this.mapClubIdToClubInfo;
            }
        }

        #endregion

        public async Task<UserState> TryBuildStateForUser(string userBotId, string channel)
        {
            string botAccountKeyDisplayName = this.GetChannelInfo(channel).BotAccountKeyDisplayName;

            foreach (var clubId in this.MapClubIdToClubInfo.Keys)
            {
                var clubInfo = this.MapClubIdToClubInfo[clubId];

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
                                UserId = user.Value<long>("id"),
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

        /// <summary>
        /// Create an environment definition from the environment environment variable.
        /// </summary>
        /// <returns>The created environment definition.</returns>
        private static EnvironmentDefinition CreateFromEnvironment()
        {
            const string HostnameEnvVar = "WEBSITE_HOSTNAME";

            var hostName = Environment.GetEnvironmentVariable(HostnameEnvVar);

            if (hostName == null)
            {
                return new LocalEnvironmentDefinition();
            }

            hostName = hostName.Split('.').FirstOrDefault();

            switch (hostName.ToUpperInvariant())
            {
                case "BOATTRACKERBOT":
                    return new DevelopmentEnvironmentDefinition();

                case "BOATTRACKERBOT-PROD":
                    return new ProductionEnvironmentDefinition();

                default:
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Environment {0} does not have a corresponding Environment definition defined",
                            hostName));
            }
        }
    }
}