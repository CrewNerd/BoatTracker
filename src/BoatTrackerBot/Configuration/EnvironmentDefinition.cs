﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Azure;

using BoatTracker.BookedScheduler;

namespace BoatTracker.Bot.Configuration
{
    /// <summary>
    /// Represents the configuration necessary to operate in an environment.
    /// </summary>
    public abstract class EnvironmentDefinition
    {
        private const string LuisModelIdKey = "LuisModelId";
        private const string LuisSubscriptionKeyKey = "LuisSubscriptionKey";
        private const string ClubIdListKey = "ClubIdList";

        // For the club details, we build the property key from a base followed by the club's ID.
        private const string ClubNameBaseKey = "ClubName_";
        private const string ClubUrlBaseKey = "ClubUrl_";
        private const string ClubUserNameBaseKey = "ClubUserName_";
        private const string ClubPasswordBaseKey = "ClubPassword_";

        static EnvironmentDefinition()
        {
            Instance = EnvironmentDefinition.CreateFromEnvironment();
        }

        public static EnvironmentDefinition Instance { get; private set; }

        public virtual string LuisModelId
        {
            get
            {
                return CloudConfigurationManager.GetSetting("LuisModelId");
            }
        }

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

        public ChannelInfo GetChannelInfo(string channel)
        {
            ChannelInfo channelInfo;
            string devName = this.IsDevelopment ? " (dev)" : string.Empty;

            if (channel == "skype")
            {
                channelInfo = new ChannelInfo("Skype", "Skype account key" + devName, true, true);
            }
            else if (channel == "facebook")
            {
                channelInfo = new ChannelInfo("Facebook Messenger", "Facebook account key" + devName, false, false);
            }
            else
            {
                throw new ArgumentException("Invalid channel name: " + channel);
            }

            return channelInfo;
        }

        #region Clubs

        private IEnumerable<string> clubIds;

        /// <summary>
        /// Gets a list of club id's that we're configured to talk to.
        /// </summary>
        public virtual IEnumerable<string> ClubIds
        {
            get
            {
                if (clubIds == null)
                {
                    var ids = CloudConfigurationManager.GetSetting(ClubIdListKey);

                    if (string.IsNullOrEmpty(ids))
                    {
                        throw new ApplicationException("Missing club id list");
                    }

                    this.clubIds = ids.Split(',');
                }

                return this.clubIds;
            }
        }

        private Dictionary<string, ClubInfo> mapClubIdToClubInfo;

        /// <summary>
        /// Gets a mapping from club id to its ClubInfo object.
        /// </summary>
        public virtual IDictionary<string, ClubInfo> MapClubIdToClubInfo
        {
            get
            {
                if (this.mapClubIdToClubInfo == null)
                {
                    this.mapClubIdToClubInfo = new Dictionary<string, ClubInfo>(this.ClubIds.Count());

                    foreach (var id in this.ClubIds)
                    {
                        var name = CloudConfigurationManager.GetSetting(ClubNameBaseKey + id);
                        if (string.IsNullOrEmpty(name))
                        {
                            throw new ApplicationException($"Missing name for club id '{id}'");
                        }

                        var url = CloudConfigurationManager.GetSetting(ClubUrlBaseKey + id);
                        if (string.IsNullOrEmpty(url))
                        {
                            throw new ApplicationException($"Missing URL for club id '{id}'");
                        }

                        var username = CloudConfigurationManager.GetSetting(ClubUserNameBaseKey + id);
                        if (string.IsNullOrEmpty(username))
                        {
                            throw new ApplicationException($"Missing user name for club id '{id}'");
                        }

                        var password = CloudConfigurationManager.GetSetting(ClubPasswordBaseKey + id);
                        if (string.IsNullOrEmpty(password))
                        {
                            throw new ApplicationException($"Missing password for club id '{id}'");
                        }

                        this.mapClubIdToClubInfo.Add(
                            id,
                            new ClubInfo { Name = name, Url = url, UserName = username, Password = password });
                    }
                }

                return this.mapClubIdToClubInfo;
            }
        }

        #endregion

        public async Task<UserState> TryBuildStateForUser(string userBotId, string channel)
        {
            string botAccountKeyDisplayName = this.GetChannelInfo(channel).BotAccountKeyDisplayName;

            foreach (var clubId in this.ClubIds)
            {
                var clubInfo = this.MapClubIdToClubInfo[clubId];

                BookedSchedulerClient client = new BookedSchedulerClient(clubInfo.Url);

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