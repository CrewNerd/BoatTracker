using System;
using System.Collections.Generic;

namespace BoatTracker.Bot.Configuration
{
    /// <summary>
    /// Represents per-environment configuration data specific to the production environment in Azure.
    /// </summary>
    public class LocalEnvironmentDefinition : EnvironmentDefinition
    {
        public LocalEnvironmentDefinition()
        {
            this.MapClubIdToClubInfo = new Dictionary<string, ClubInfo>
            {
                ["foo"] = new ClubInfo
                {
                    Id = "foo",
                    Name = "Site Name",
                    Url = new Uri("http://yoursite.bookedscheduler.com/Web/Services/index.php/"),
                    UserName = "boattrackerbot",
                    Password = "password",
                    DailyReportGmtHour = 7,
                    DailyReportRecipients = "",
                    DoorNames = new [] { "Main door", "Side door" },
                    EarliestUseHour = 5f,
                    LatestUseHour = 23.5f,
                    MinimumDurationHours = 0.5f,
                    MaximumDurationHours = 4f,
                    RfidPassword = "abcdefgh"
                }
            };
        }

        /// <summary>
        /// Gets the model ID for our LUIS service.
        /// </summary>
        public override string LuisModelId
        {
            get
            {
                // Leaving this empty for security reasons... fill it in by hand when debugging locally
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the subscription key for our LUIS service
        /// </summary>
        public override string LuisSubscriptionKey
        {
            get
            {
                // Leaving this empty for security reasons... fill it in by hand when debugging locally
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the API key for SendGrid
        /// </summary>
        public override string SendGridApiKey
        {
            get
            {
                // Leaving this empty for security reasons... fill it in by hand when debugging locally
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the name of the boattracker service for this deployment slot. Used by WebJobs
        /// to call the service.
        /// </summary>
        public override string ServiceHost
        {
            get
            {
                // Leaving this empty for security reasons... fill it in by hand when debugging locally
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is the local environment.
        /// </summary>
        public override bool IsLocal
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the short name of the environment.
        /// </summary>
        public override string Name
        {
            get { return "LOCAL"; }
        }
    }
}