using System;

namespace BoatTracker.Bot.Configuration
{
    /// <summary>
    /// Represents per-environment configuration data specific to the development environment in Azure.
    /// </summary>
    public class DevelopmentEnvironmentDefinition : EnvironmentDefinition
    {
        public override string BotAccountKeyDisplayName
        {
            get
            {
                return "Bot account key (dev)";
            }
        }

        public override bool IsDevelopment
        {
            get
            {
                return true;
            }
        }
    }
}