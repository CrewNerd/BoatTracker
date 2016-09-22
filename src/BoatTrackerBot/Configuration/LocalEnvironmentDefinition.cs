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
                    Name = "Foo Bar",
                    Url = new Uri("https://foo.bookedscheduler.com/Web/Services/index.php/"),
                    UserName = "boattrackerbot",
                    Password = "foo",
                    DoorNames = new [] { "Main door", "Side door" }
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