using System;

namespace BoatTracker.Bot.Configuration
{
    /// <summary>
    /// Represents per-environment configuration data specific to the development environment in Azure.
    /// </summary>
    public class DevelopmentEnvironmentDefinition : EnvironmentDefinition
    {
        public override bool IsDevelopment
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
                return "DEV";
            }
        }
    }
}