using System;

namespace BoatTracker.Bot.Configuration
{
    /// <summary>
    /// Represents per-environment configuration data specific to the production environment in Azure.
    /// </summary>
    public class ProductionEnvironmentDefinition : EnvironmentDefinition
    {
        /// <summary>
        /// Gets a value indicating whether this is the production (or staging) environment.
        /// </summary>
        public override bool IsProduction
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the short name of the environment.
        /// </summary>
        public override string Name
        {
            get
            {
                return "PROD";
            }
        }
    }
}