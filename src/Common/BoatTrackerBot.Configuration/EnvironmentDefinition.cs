﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Microsoft.Azure;

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
        private const string SendGridApiKeyKey = "SendGridApiKey";
        private const string SecurityKeyKey = "SecurityKey";
        private const string ServiceHostKey = "ServiceHost";

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
                return CloudConfigurationManager.GetSetting(LuisModelIdKey);
            }
        }

        /// <summary>
        /// Gets the subscription key for our LUIS service
        /// </summary>
        public virtual string LuisSubscriptionKey
        {
            get
            {
                return CloudConfigurationManager.GetSetting(LuisSubscriptionKeyKey);
            }
        }

        /// <summary>
        /// Gets the API key for SendGrid
        /// </summary>
        public virtual string SendGridApiKey
        {
            get
            {
                return CloudConfigurationManager.GetSetting(SendGridApiKeyKey);
            }
        }

        public virtual string SecurityKey
        {
            get
            {
                return CloudConfigurationManager.GetSetting(SecurityKeyKey);
            }
        }

        public virtual string ServiceHost
        {
            get
            {
                return CloudConfigurationManager.GetSetting(ServiceHostKey);
            }
        }

        public virtual bool IsLocal { get { return false; } }

        public virtual bool IsDevelopment { get { return false; } }

        public virtual bool IsProduction { get { return false; } }

        public abstract string Name { get; }

        /// <summary>
        /// Gets a mapping from club id to its ClubInfo object.
        /// </summary>
        public IReadOnlyDictionary<string, ClubInfo> MapClubIdToClubInfo { get; protected set; }

        /// <summary>
        /// Returns channel information for the given channel name.
        /// </summary>
        /// <param name="channel">The internal name of the channel</param>
        /// <returns>A ChannelInfo object with configuration information for the channel.</returns>
        public ChannelInfo GetChannelInfo(string channel)
        {
            ChannelInfo channelInfo;

            if (channel == "skype")
            {
                channelInfo = new ChannelInfo(
                    "Skype",
                    supportsButtons:true,
                    supportsMarkdown:true);
            }
            else if (channel == "facebook")
            {
                channelInfo = new ChannelInfo(
                    "Facebook Messenger",
                    supportsButtons:false,
                    supportsMarkdown:false);
            }
            else if (channel == "sms")
            {
                channelInfo = new ChannelInfo(
                    "Text Message",
                    supportsButtons:false,
                    supportsMarkdown:false);
            }
            else
            {
                throw new ArgumentException("Invalid channel name: " + channel);
            }

            return channelInfo;
        }

        /// <summary>
        /// Create an environment definition from the environment environment variable.
        /// </summary>
        /// <returns>The created environment definition.</returns>
        private static EnvironmentDefinition CreateFromEnvironment()
        {
            const string HostnameEnvVar = "WEBSITE_HOSTNAME";
            const string ConfigFilePath = @"d:\home\site\wwwroot\ClubConfiguration.json";

            EnvironmentDefinition environmentDefinition;

            var hostName = Environment.GetEnvironmentVariable(HostnameEnvVar);

            if (hostName == null)
            {
                environmentDefinition = new LocalEnvironmentDefinition();
            }
            else
            {
                hostName = hostName.Split('.').FirstOrDefault();

                switch (hostName.ToUpperInvariant())
                {
                    case "BOATTRACKERBOT-DEV":
                        environmentDefinition = new DevelopmentEnvironmentDefinition();
                        break;

                    case "BOATTRACKERBOT-STG":
                    case "BOATTRACKERBOT":
                        environmentDefinition = new ProductionEnvironmentDefinition();
                        break;

                    default:
                        throw new ArgumentException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Environment {0} does not have a corresponding Environment definition defined",
                                hostName));
                }

                try
                {
                    // For cloud environments, we read club configuration from a local file
                    var configText = File.ReadAllText(ConfigFilePath);
                    var config = JsonConvert.DeserializeObject<ClubConfiguration>(configText);

                    environmentDefinition.MapClubIdToClubInfo = config.Clubs.ToDictionary(c => c.Id);
                }
                catch (Exception ex)
                {
                    throw new ApplicationException("No configuration file found", ex);
                }
            }

            return environmentDefinition;
        }
    }
}