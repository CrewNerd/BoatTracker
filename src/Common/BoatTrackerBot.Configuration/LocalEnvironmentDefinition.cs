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

        public override string LuisModelId
        {
            get
            {
                // Leaving this empty for security reasons... fill it in by hand when debugging locally
                return "";
            }
        }

        public override string LuisSubscriptionKey
        {
            get
            {
                // Leaving this empty for security reasons... fill it in by hand when debugging locally
                return "";
            }
        }

        public override string SendGridApiKey
        {
            get
            {
                // Leaving this empty for security reasons... fill it in by hand when debugging locally
                return "";
            }
        }

        public override string ServiceHost
        {
            get
            {
                // Leaving this empty for security reasons... fill it in by hand when debugging locally
                return "";
            }
        }

        public override bool IsLocal
        {
            get
            {
                return true;
            }
        }

        public override string Name
        {
            get
            {
                return "LOCAL";
            }
        }
    }
}