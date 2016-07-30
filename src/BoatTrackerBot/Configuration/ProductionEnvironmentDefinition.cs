using System;

namespace BoatTracker.Bot.Configuration
{
    /// <summary>
    /// Represents per-environment configuration data specific to the production environment in Azure.
    /// </summary>
    public class ProductionEnvironmentDefinition : EnvironmentDefinition
    {
        /// <summary>
        /// Gets the expiration time for pending trigger items
        /// </summary>
        public override TimeSpan TriggerExpirationTime
        {
            get
            {
                return TimeSpan.FromHours(2);
            }
        }
    }
}