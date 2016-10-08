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
                ["pnw"] = new ClubInfo
                {
                    Id = "pnw",
                    Name = "PNW Test Site",
                    Url = new Uri("http://pnw.bookedscheduler.com/Web/Services/index.php/"),
                    UserName = "boattrackerbot",
                    Password = "xyzzy",
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

        public override bool IsLocal
        {
            get
            {
                return true;
            }
        }
    }
}