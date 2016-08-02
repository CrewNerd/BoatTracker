using System;
using System.Collections.Generic;

namespace BoatTracker.Bot.Configuration
{
    /// <summary>
    /// Represents per-environment configuration data specific to the production environment in Azure.
    /// </summary>
    public class LocalEnvironmentDefinition : EnvironmentDefinition
    {
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

        public override IEnumerable<string> ClubIds
        {
            get
            {
                // Using placeholder value for security reasons... fill it in by hand when debugging locally
                return new List<string>() { "foo" };
            }
        }

        public override IDictionary<string, ClubInfo> MapClubIdToClubInfo
        {
            get
            {
                // Using placeholder values for security reasons... replace these when debugging locally
                return new Dictionary<string, ClubInfo>
                {
                    ["foo"] = new ClubInfo
                    {
                        Name = "Foo",
                        Url = "https://foo.bookedscheduler.com/Web/Services/index.php/",
                        UserName = "boattrackerbot",
                        Password = "foo"
                    }
                };
            }
        }
    }
}