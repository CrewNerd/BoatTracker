using System;

namespace BoatTracker.Bot.Configuration
{
    /// <summary>
    /// Represents per-environment configuration data specific to the production environment in Azure.
    /// </summary>
    public class ProductionEnvironmentDefinition : EnvironmentDefinition
    {
        public override string BotAccountKeyDisplayName
        {
            get
            {
                return "Bot account key";
            }
        }

        public override bool IsProduction
        {
            get
            {
                return true;
            }
        }
    }
}