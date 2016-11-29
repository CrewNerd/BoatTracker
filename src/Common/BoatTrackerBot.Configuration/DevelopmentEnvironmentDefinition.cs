using System;

namespace BoatTracker.Bot.Configuration
{
    /// <summary>
    /// Represents per-environment configuration data specific to the development environment in Azure.
    /// </summary>
    public class DevelopmentEnvironmentDefinition : EnvironmentDefinition
    {
        /// <summary>
        /// Gets a value indicating whether this is the development environment.
        /// </summary>
        public override bool IsDevelopment
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the short name of the environment.
        /// </summary>
        public override string Name
        {
            get { return "DEV"; }
        }
    }
}